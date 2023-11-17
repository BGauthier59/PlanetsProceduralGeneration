using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoSingleton<UIManager>
{
    [SerializeField] private List<PlanetGUI> allPlanetGuis;
    [SerializeField] private Transform planetGuiLayout;
    [SerializeField] private PlanetGUI planetGuiPrefab;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Transform choiceWheel;
    [SerializeField] private Image[] choices;
    [SerializeField] private int selection;
    [SerializeField] private Color selectionColor,unselectColor;
    [SerializeField] private Vector3 mousePosMemory;
    [SerializeField] private bool selecting;
    [SerializeField] private string[] syllables;
    
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


        string pname = "";

        int x = RandomGenerator.GetRandomValueInt(3, 5);
        
        for (int i = 0; i < x; i++)
        {
            pname += syllables[RandomGenerator.GetRandomValueInt(0, syllables.Length)];
        }

        p.planetName = pname;
        n.Initialize(newPlanetIndex, w, e,pname);
        allPlanetGuis.Add(n);
    }

    public void SelectExplorerGui(int index)
    {
        infoText.text = $"Explorer N°{index} is selected";
    }

    public void UnSelectExplorerGui()
    {
        infoText.text = $"No explorer selected";
    }

    public void SetChoiceWheel()
    {
        choiceWheel.gameObject.SetActive(true);
        choiceWheel.position = Input.mousePosition;
        mousePosMemory = Input.mousePosition;
        selecting = true;
    }
    
    public void HideChoiceWheel()
    {
        choiceWheel.gameObject.SetActive(false);
        selecting = false;
    }

    public void Update()
    {
        for (int i = 0; i < allPlanetGuis.Count; i++)
        {
            allPlanetGuis[i].UpdateRessourceCount(WorldManager.instance.GetPlanet(i).hangarRessourceAmount);
        }
        
        
        
        if (!selecting) return;
        
        if (Vector3.Distance(Input.mousePosition, mousePosMemory) < 300)
        {
            Vector3 dir = Input.mousePosition - mousePosMemory;
            float dot = Vector3.Dot(dir.normalized, Vector3.up);
            if (dot > 0.5f)
            {
                selection = 3;
                PlayerController.instance.selection = 3;
            } 
            else if (dot < -0.5f)
            {
                selection = 1;
                PlayerController.instance.selection = 1;
            }
            else if (dir.x > 0)
            {
                selection = 2;
                PlayerController.instance.selection = 2;
            }
            else
            {
                selection = 0;
                PlayerController.instance.selection = 0;
            }
        }
        else
        {
            selection = -1;
            PlayerController.instance.selection = -1;
        }
        
        for (int i = 0; i < 4; i++)
        {
            if (i == selection)
            {
                choices[i].transform.localScale = Vector3.Lerp(choices[i].transform.localScale,Vector3.one*1.5f, 5*Time.deltaTime);
                choices[i].color = Color.Lerp(choices[i].color,selectionColor,5*Time.deltaTime);
            }
            else
            {
                choices[i].transform.localScale = Vector3.Lerp(choices[i].transform.localScale,Vector3.one, 5*Time.deltaTime);
                choices[i].color = Color.Lerp(choices[i].color,unselectColor,5*Time.deltaTime);
            }
        }
    }
}
