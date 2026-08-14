using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiHandPhase : MonoBehaviour
{
    [Header("Script made by Danix, please give credits if used!!")]
    public Transform sphere;
    public Transform controller;

    void Update()
    {
        if (true)
        {
            sphere.rotation = controller.rotation;
        }
    }
}
   