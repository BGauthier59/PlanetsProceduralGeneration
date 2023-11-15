using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoSingleton<UIManager>
{
    [SerializeField] private PlanetGUI[] allPlanetGuis;
    [SerializeField] private Transform planetGuiLayout;
    [SerializeField] private PlanetGUI planetGuiPrefab;

    public void AddPlanetGui(int newPlanetIndex)
    {
        var n = Instantiate(planetGuiPrefab, Vector3.zero, Quaternion.identity, planetGuiLayout);
        n.Initialize(newPlanetIndex);
    }
}
