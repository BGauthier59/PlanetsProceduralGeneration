using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldManager : MonoSingleton<WorldManager>
{
    [SerializeField] private List<Planet> allPlanets;
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

    public async Task CreateNewPlanet(Vector3 position)
    {
        var p = Instantiate(planetPrefab, position, Quaternion.identity);
        await p.Initialize();
        allPlanets.Add(p);
        UIManager.instance.AddPlanetGui(allPlanets.Count - 1);
    }

    public void DEBUG_CreatePlanet()
    {
        Vector3 rng = new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50));
        CreateNewPlanet(rng);
    }
}
