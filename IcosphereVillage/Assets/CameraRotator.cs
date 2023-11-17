using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotator : MonoBehaviour
{
    public float iconSize;
    public int planet;
    public SpriteRenderer renderer;

    private void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (PlayerController.instance.GetCurrentPlanet.seed != planet)
        {
            renderer.enabled = false;
            transform.GetChild(0).gameObject.SetActive(false);
        }
        else
        {
            renderer.enabled = true;
            transform.GetChild(0).gameObject.SetActive(true);
        }
        
        transform.rotation = Quaternion.Euler(0,0,0);
        transform.localScale = Vector3.one *
                                    ((1 * Vector3.Distance(transform.position,
                                        PlayerController.instance.cam.transform.position)) * iconSize);
    }
}
