using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlanetGUI : MonoBehaviour
{
    [SerializeField] private int index;

    public void Initialize(int i)
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
        index = i;
    }
    
    public void OnClick()
    {
        PlayerController.instance.SetNewTarget(index);
    }
}
