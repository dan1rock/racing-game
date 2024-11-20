using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KnockManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    
    public int totalObjects = 0;
    public int knockedObjects = 0;

    private List<KnockDownObject> _knockObjects = new();

    private LevelManager _levelManager;

    private void Awake()
    {
        _levelManager = FindFirstObjectByType<LevelManager>();
    }

    public void RegisterObject(KnockDownObject knockDownObject)
    {
        _knockObjects.Add(knockDownObject);
        totalObjects += 1;
    }

    public void ObjectKnocked(KnockDownObject knockDownObject)
    {
        if (_knockObjects.Remove(knockDownObject))
        {
            knockedObjects += 1;

            scoreText.text = knockedObjects.ToString();
        }
    }

    public bool StageFailed()
    {
        return _levelManager.stageFailed;
    }
}
