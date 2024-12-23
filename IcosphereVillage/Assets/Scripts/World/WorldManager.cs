using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldManager : MonoSingleton<WorldManager>
{
    [SerializeField] private List<Planet> allPlanets;
    [SerializeField] private List<ExplorerBehaviour> allExplorers;
    [SerializeField] private Planet planetPrefab;
    [SerializeField] private Transform origin;
    [SerializeField] private uint globalSeed;
    [SerializeField] private int enteredSeed = -1;
    [SerializeField] private int explorationRadiusStep;
    private int explorationRadius;
    [SerializeField] private float rocketSpeed;

    private async void Start()
    {
        RandomGenerator.seedGeneratorSeed = (uint)Random.Range(0, int.MaxValue);

        await CreateNewPlanet(GetNewPlanetPositionAndAngle(), (int)RandomGenerator.GetRandomSeed());
        PlayerController.instance.Initialize(allPlanets[0]);
        allPlanets[0].rotating = true;
    }

    private void Update()
    {
        RotatePlanetsAroundOrigin();
        PlayerController.instance.UpdatePlayer();
    }

    public Planet GetPlanet(int index)
    {
        return allPlanets[index];
    }

    public int AddExplorer(ExplorerBehaviour explorer)
    {
        allExplorers.Add(explorer);
        return allExplorers.Count - 1;
    }

    public ExplorerBehaviour GetExplorer(int index)
    {
        return allExplorers[index];
    }

    public async void LaunchAircraft(Planet planet, int index)
    {
        Transform rocket = planet.rocketByIndex[index];
        PlayerController.instance.SetZoomOnRocket(rocket);

        await Task.Delay(1000);

        rocket.GetComponentInChildren<TrailRenderer>().enabled = true;
        rocket.SetParent(null);

        #region CreatePlanet

        Planet newPlanet = await CreateNewPlanet(GetNewPlanetPositionAndAngle(), (int)RandomGenerator.GetRandomSeed());

        #endregion

        #region Start flight

        Triangle start = planet.triangles[index];
        float timer = 0;
        Vector3 old, dir;
        Triangle end = newPlanet.triangles[newPlanet.hangarIndex];
        dir = planet.transform.TransformDirection(start.elevationNormal);
        while (timer < 2)
        {
            timer += Time.deltaTime;
            await Task.Yield();
            rocket.position += dir * 5 * timer * Time.deltaTime;
        }

        timer = 0;

        #endregion

        #region Fly to planet

        Vector3 p1, p2, p3, p4;

        p1 = rocket.position;
        p2 = rocket.position + planet.transform.TransformDirection(start.elevationNormal) * 30;
        p3 = newPlanet.transform.TransformPoint(end.centralPoint)
             + newPlanet.transform.TransformDirection(end.elevationNormal * 30);
        p4 = newPlanet.transform.TransformPoint(end.centralPoint);

        float duration = Vector3.Distance(planet.transform.position, newPlanet.transform.position) / rocketSpeed;

        while (timer < duration)
        {
            old = rocket.position;
            rocket.position = ExBeziers.CubicBeziersCurve(p1, p2, p3, p4, timer / duration);
            dir = (rocket.position - old).normalized;
            rocket.rotation = Quaternion.Slerp(rocket.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 5);

            timer += Time.deltaTime;
            await Task.Yield();
        }

        #endregion

        planet.rocketByIndex.Remove(index);
        start.building = Building.None;
        rocket.gameObject.SetActive(false);
        PlayerController.instance.ExitRocketZoom();
        PlayerController.instance.SetCurrentPlanet(newPlanet);
        newPlanet.rotating = true;
    }

    public async Task<Planet> CreateNewPlanet((Vector3, float) data, int seed)
    {
        uint realSeed = seed == -1 ? RandomGenerator.GetRandomSeed() : (uint)seed;
        var p = Instantiate(planetPrefab, data.Item1, Quaternion.identity, origin);

        p.currentAngle = data.Item2;
        p.distanceFromOrigin = explorationRadius;
        p.orbitSpeed = RandomGenerator.GetRandomValueInRange(10, 25) / p.distanceFromOrigin;
        p.amplitude = RandomGenerator.GetRandomValueInRange(0, 50);
        p.offset = RandomGenerator.GetRandomValueInRange(0, 2 * math.PI);

        await p.Initialize(realSeed);
        allPlanets.Add(p);
        UIManager.instance.AddPlanetGui(allPlanets.Count - 1);

        return p;
    }

    private (Vector3, float) GetNewPlanetPositionAndAngle()
    {
        explorationRadius += explorationRadiusStep;
        float randomAngle = math.radians(RandomGenerator.GetRandomValueInRange(0, 360));

        Vector3 pos = new Vector3(explorationRadius * math.cos(randomAngle),
            0,
            explorationRadius * math.sin(randomAngle));

        return ((pos + origin.position), randomAngle);
    }

    private void RotatePlanetsAroundOrigin()
    {
        foreach (var planet in allPlanets)
        {
            if (planet == PlayerController.instance.GetCurrentPlanet || !planet.rotating) continue;
            planet.currentAngle += planet.orbitSpeed * Time.deltaTime;
            planet.transform.position = origin.position +
                                        Vector3.right * (planet.distanceFromOrigin * math.cos(planet.currentAngle)) +
                                        Vector3.forward * (planet.distanceFromOrigin * math.sin(planet.currentAngle))
                                        + Vector3.up *
                                        (math.sin(planet.currentAngle + planet.offset) * planet.amplitude);
        }
    }

    public async void DEBUG_CreatePlanet()
    {
        var rng = GetNewPlanetPositionAndAngle();
        var p = await CreateNewPlanet(rng, enteredSeed);
        p.rotating = true;
    }

    public int DEBUG_RocketPos;

    public void DEBUG_CreateRocket()
    {
        allPlanets[^1].CreateBuilding(DEBUG_RocketPos, Building.Rocket);
    }

    public void DEBUG_SetSeed(int s)
    {
        enteredSeed = s;
    }
}