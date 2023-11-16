using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoSingleton<UIManager>
{
    [SerializeField] private PlanetGUI[] allPlanetGuis;
    [SerializeField] private Transform planetGuiLayout;
    [SerializeField] private PlanetGUI planetGuiPrefab;
    [SerializeField] private TMP_Text infoText;
    
    private static readonly int WaterColor = Shader.PropertyToID("_WaterColor");

    private void Start()
    {
        UnSelectExplorerGui();
    }

    public void AddPlanetGui(int newPlanetIndex)
    {
        var p = WorldManager.instance.GetPlanet(newPlanetIndex);
        var n = Instantiate(planetGuiPrefab, Vector3.zero, Quaternion.identity, planetGuiLayout);
        Color w, e;
        if (p.waterLevel == 0)
        {
            w = p.biome.groundColor;
            e = p.biome.topColor;
        }
        else
        {
            w = p.waterRenderer.material.GetColor(WaterColor);
            e = p.biome.groundColor;
        }

        w.a = 1;
        e.a = 1;
        n.Initialize(newPlanetIndex, w, e);

    }

    public void SelectExplorerGui(int index)
    {
        infoText.text = $"Explorer N°{index} is selected";
    }

    public void UnSelectExplorerGui()
    {
        infoText.text = $"No explorer selected";
    }
}
