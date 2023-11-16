using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome", menuName = "ScriptableObjects/Biome", order = 1)]
public class BiomeSO : ScriptableObject
{
    public Color topColor;
    public Color groundColor;
    public Color bottomColor;

    public Material[] waterMaterials;

    [SerializeField] public RessourceType[] ressources;
}

[Serializable]
public struct RessourceType
{
    public GameObject prefab;
    public Gradient colorGradient;
    public int materialIndex;
}