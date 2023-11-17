using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlanetGUI : MonoBehaviour
{
    [SerializeField] private int index;
    [SerializeField] private Image water, earth;
    [SerializeField] private TMP_Text name, ressource;

    public void Initialize(int i, Color water, Color earth,string name)
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
        index = i;
        this.water.color = water;
        this.earth.color = earth;
        this.name.text = name;
    }

    public void UpdateRessourceCount(int ressources)
    {
        ressource.text = ressources.ToString();
    }
    
    public void OnClick()
    {
        PlayerController.instance.SetNewTarget(index);
    }
}
