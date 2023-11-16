using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldManager : MonoSingleton<WorldManager>
{
    [SerializeField] private List<Planet> allPlanets;
    [SerializeField] private List<ExplorerBehaviour> allExplorers;
    [SerializeField] private Planet planetPrefab;
    [SerializeField] private uint globalSeed;

    private async void Start()
    {
        RandomGenerator.seedGeneratorSeed = globalSeed;
        
        await CreateNewPlanet(Vector3.zero);
        PlayerController.instance.Initialize(allPlanets[0]);
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
        Debug.Log("Launched aicraft of " + planet +  "on tile " + index);
        
        Transform rocket = planet.rocketByIndex[index];
        PlayerController.instance.SetZoomOnRocket(rocket);
        
        rocket.SetParent(null);
        
        Vector3 nextPlanetPosition = new Vector3(50, 50, 50);

        Planet newPlanet = await CreateNewPlanet(nextPlanetPosition);
        
        Triangle start = planet.triangles[index];
        Triangle end = newPlanet.triangles[newPlanet.hangarIndex];

        Vector3 p1, p2, p3, p4;

        p1 = rocket.position;
        p2 = rocket.position + start.elevationNormal * 30;
        p3 = newPlanet.transform.TransformPoint(end.centralPoint)
             + newPlanet.transform.TransformDirection(end.elevationNormal * 30);
        p4 = newPlanet.transform.TransformPoint(end.centralPoint);

        float timer = 0;
        while (timer < 5)
        {
            timer += Time.deltaTime;
            await Task.Yield();
            rocket.position = ExBeziers.CubicBeziersCurve(p1, p2, p3, p4, timer / 5);
        }
        
        planet.rocketByIndex.Remove(index);
        start.building = Building.None;
        rocket.gameObject.SetActive(false);
        
        PlayerController.instance.ExitRocketZoom();
        PlayerController.instance.SetCurrentPlanet(newPlanet);
    }

    public async Task<Planet> CreateNewPlanet(Vector3 position)
    {
        var p = Instantiate(planetPrefab, position, Quaternion.identity);
        await p.Initialize();
        allPlanets.Add(p);
        UIManager.instance.AddPlanetGui(allPlanets.Count - 1);

        return p;
    }

    public void DEBUG_CreatePlanet()
    {
        Vector3 rng = new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50));
        CreateNewPlanet(rng);
    }

    public int DEBUG_RocketPos;
    public void DEBUG_CreateRocket()
    {
        allPlanets[0].CreateBuilding(DEBUG_RocketPos, Building.Rocket);
    }
}
