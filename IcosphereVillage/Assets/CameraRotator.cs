using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotator : MonoBehaviour
{
    public float iconSize;
    void Update()
    {
        transform.rotation = Quaternion.Euler(0,0,0);
        transform.localScale = Vector3.one *
                                    ((1 * Vector3.Distance(transform.position,
                                        PlayerController.instance.cam.transform.position)) * iconSize);
    }
}
