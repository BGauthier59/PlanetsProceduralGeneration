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
    [SerializeField] private int explorationRadiusStep;
    private int explorationRadius;

    private async void Start()
    {
        RandomGenerator.seedGeneratorSeed = globalSeed;

        await CreateNewPlanet(GetNewPlanetPositionAndAngle());
        PlayerController.instance.Initialize(allPlanets[0]);
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

        #region Fly to planet

        Triangle start = planet.triangles[index];
        Vector3 p1, p2, p3, p4;
        Vector3 old, dir;

        float timer = 0;

        Planet newPlanet = await CreateNewPlanet(GetNewPlanetPositionAndAngle());
        Triangle end = newPlanet.triangles[newPlanet.hangarIndex];

        p1 = rocket.position;
        p2 = rocket.position + start.elevationNormal * 30;
        p3 = newPlanet.transform.TransformPoint(end.centralPoint)
             + newPlanet.transform.TransformDirection(end.elevationNormal * 30);
        p4 = newPlanet.transform.TransformPoint(end.centralPoint);
        
        while (timer < 5)
        {
            timer += Time.deltaTime;
            await Task.Yield();
            old = rocket.position;
            rocket.position = ExBeziers.CubicBeziersCurve(p1, p2, p3, p4, timer / 5);
            dir = (rocket.position - old).normalized;
            rocket.rotation = Quaternion.Slerp(rocket.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 5);
        }

        #endregion

        planet.rocketByIndex.Remove(index);
        start.building = Building.None;
        rocket.gameObject.SetActive(false);
        PlayerController.instance.ExitRocketZoom();
        PlayerController.instance.SetCurrentPlanet(newPlanet);
    }

    public async Task<Planet> CreateNewPlanet((Vector3, float) data)
    {
        var p = Instantiate(planetPrefab, data.Item1, Quaternion.identity, origin);
        
        p.currentAngle = data.Item2;
        p.distanceFromOrigin = explorationRadius;
        p.orbitSpeed = RandomGenerator.GetRandomValueInRange(0, 0.5f);
        p.orbitSpeed = 0.2f;
        
        await p.Initialize();
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
            //if (planet == PlayerController.instance.GetCurrentPlanet) continue;
            planet.currentAngle += planet.orbitSpeed * Time.deltaTime;
            planet.transform.position = origin.position +
                                        Vector3.right * (planet.distanceFromOrigin * math.cos(planet.currentAngle)) +
                                        Vector3.forward * (planet.distanceFromOrigin * math.sin(planet.currentAngle));
        }
    }

    public void DEBUG_CreatePlanet()
    {
        var rng = GetNewPlanetPositionAndAngle();
        CreateNewPlanet(rng);
    }

    public int DEBUG_RocketPos;

    public void DEBUG_CreateRocket()
    {
        allPlanets[^1].CreateBuilding(DEBUG_RocketPos, Building.Rocket);
    }
}