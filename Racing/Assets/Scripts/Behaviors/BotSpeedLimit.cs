using System;
using UnityEngine;

public class BotSpeedLimit : MonoBehaviour
{
    [SerializeField] private bool reverse = false;
    [SerializeField] private bool dontBreak = false;
    [SerializeField] private float speedLimit = 30f;

    private LevelManager _levelManager;

    private void Awake()
    {
        _levelManager = FindObjectOfType<LevelManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_levelManager.reverse != reverse) return;
        
        CarBot carBot = other.transform.parent.GetComponent<CarBot>();

        if (carBot)
        {
            if (!dontBreak)
            {
                carBot.speedLimit = speedLimit;
            }

            carBot.dontBreak = dontBreak;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (_levelManager.reverse != reverse) return;
        
        CarBot carBot = other.transform.parent.GetComponent<CarBot>();

        if (carBot)
        {
            carBot.speedLimit = Mathf.Infinity;
            carBot.dontBreak = false;
        }
    }
}
