using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldManager : MonoSingleton<WorldManager>
{
    [SerializeField] private List<Transform> allPlanets;
    [SerializeField] private Planet planetPrefab;

    private void Start()
    {
        CreateNewPlanet(Vector3.zero);
    }

    public Transform GetPlanet(int index)
    {
        return allPlanets[index];
    }

    public void CreateNewPlanet(Vector3 position)
    {
        var p = Instantiate(planetPrefab, position, Quaternion.identity);
        p.Initialize();
        UIManager.instance.AddPlanetGui(allPlanets.Count);
        allPlanets.Add(p.transform);
    }

    public void DEBUG_CreatePlanet()
    {
        Vector3 rng = new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50));
        CreateNewPlanet(rng);
    }
}
