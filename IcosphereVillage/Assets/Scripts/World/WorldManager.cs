using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldManager : MonoSingleton<WorldManager>
{
    [SerializeField] private List<Planet> allPlanets;
    [SerializeField] private Planet planetPrefab;
    [SerializeField] private uint globalSeed;

    private void Start()
    {
        RandomGenerator.seedGeneratorSeed = globalSeed;
        
        CreateNewPlanet(Vector3.zero);
        PlayerController.instance.Initialize(allPlanets[0]);
    }

    public Planet GetPlanet(int index)
    {
        return allPlanets[index];
    }

    public void CreateNewPlanet(Vector3 position)
    {
        var p = Instantiate(planetPrefab, position, Quaternion.identity);
        p.Initialize();
        UIManager.instance.AddPlanetGui(allPlanets.Count);
        allPlanets.Add(p);
    }

    public void DEBUG_CreatePlanet()
    {
        Vector3 rng = new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50));
        CreateNewPlanet(rng);
    }
}
