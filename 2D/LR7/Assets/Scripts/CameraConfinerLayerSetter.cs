using System;
using UnityEngine;

public class CameraConfinerLayerSetter : MonoBehaviour
{
    private void OnEnable()
    {
        Helpers.RecursiveLayerSet(transform, Helpers.ConfinerLayer);
    }
}
