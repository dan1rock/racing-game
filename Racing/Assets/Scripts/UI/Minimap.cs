using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    public Transform reference1World;
    public Transform reference2World;

    public RectTransform reference1Minimap;
    public RectTransform reference2Minimap;

    public RectTransform playerMarker;
    public Transform playerTransform;

    private List<RectTransform> _botMarkers = new();
    private List<Transform> _botLocations = new();

    public float minimapRotation;

    private Vector2 _worldPos1;
    private Vector2 _worldPos2;
    
    private Vector2 _minimapPos1;
    private Vector2 _minimapPos2;

    private Vector2 _worldDelta;
    private Vector2 _minimapDelta;

    private Vector2 _scale;

    private void Awake()
    {
        Init();
    }

    private void Update()
    {
        ProcessMarker(playerTransform, playerMarker);

        for (int i = 0; i < _botLocations.Count; i++)
        {
            ProcessMarker(_botLocations[i], _botMarkers[i]);
        }
    }

    private void Init()
    {
        _worldPos1 = new Vector2(reference1World.position.z, reference1World.position.x);
        _worldPos2 = new Vector2(reference2World.position.z, reference2World.position.x);
        
        _minimapPos1 = reference1Minimap.anchoredPosition;
        _minimapPos2 = reference2Minimap.anchoredPosition;
        
        _worldDelta = _worldPos2 - _worldPos1;
        _minimapDelta = _minimapPos2 - _minimapPos1;
        
        _scale = new Vector2(_minimapDelta.x / _worldDelta.x, _minimapDelta.y / _worldDelta.y);
    }

    private void ProcessMarker(Transform worldPos, RectTransform marker)
    {
        Vector2 playerWorldPos = new(worldPos.position.z, worldPos.position.x);
        
        Vector2 offsetWorld = playerWorldPos - _worldPos1;
        
        float rotationRadians = minimapRotation * Mathf.Deg2Rad;
        Vector2 rotatedOffsetWorld = new(
            offsetWorld.x * Mathf.Cos(rotationRadians) - offsetWorld.y * Mathf.Sin(rotationRadians),
            offsetWorld.x * Mathf.Sin(rotationRadians) + offsetWorld.y * Mathf.Cos(rotationRadians)
        );
        
        Vector2 offsetMinimap = new(rotatedOffsetWorld.x * _scale.x, rotatedOffsetWorld.y * _scale.y);
        
        marker.anchoredPosition = _minimapPos1 + offsetMinimap;
    }

    public void AddBotMarker(Transform bot)
    {
        _botLocations.Add(bot);
        RectTransform botMarker = Instantiate(playerMarker, playerMarker.parent);
        botMarker.name = $"BotMarker{_botMarkers.Count}";
        botMarker.GetComponent<Image>().color = Color.grey;
        botMarker.localScale *= 0.7f;
        
        _botMarkers.Add(botMarker);
    }
}
