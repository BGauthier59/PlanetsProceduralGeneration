using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlanetGUI : MonoBehaviour
{
    [SerializeField] private int index;
    [SerializeField] private Image water, earth;

    public void Initialize(int i, Color water, Color earth)
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
        index = i;
        this.water.color = water;
        this.earth.color = earth;
    }
    
    public void OnClick()
    {
        PlayerController.instance.SetNewTarget(index);
    }
}
