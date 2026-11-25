using System;
using UnityEngine;

public class NonPlayerCharacter : MonoBehaviour
{
    public GameObject dialogueBubble;

    private void Awake()
    {
        //DEMO purpose only. In the tutorial, this is set in the inspector on the prefab, but to keep the layer
        //settings empty, we have to do it dynamically in the demo
        Helpers.RecursiveLayerSet(transform, Helpers.NPCLayer);
    }

    void Start()
    {
        dialogueBubble.SetActive(false);
    }
}
