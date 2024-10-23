using System.Collections.Generic;
using UnityEngine;

public class WorldSpaceCanvasScaler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private List<Transform> objectsToAdjust;
    [SerializeField] private float borderlineAspect = 5f / 3f;
    [SerializeField] private bool lockMaxAspect;

    private List<Vector3> _adjustableObjectsDefaultScales = new();
    
    private Vector2 _defaultResolution;
    private float _defaultAspect;

    private float _recordedAspect;

    private RectTransform _canvasRectTransform;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        _canvasRectTransform = GetComponent<RectTransform>();

        _defaultResolution = new Vector2(_canvasRectTransform.rect.width, _canvasRectTransform.rect.height);
        _defaultAspect = _defaultResolution.x / _defaultResolution.y;

        foreach (Transform t in objectsToAdjust)
        {
            _adjustableObjectsDefaultScales.Add(t.localScale);
        }
        
        AdjustCanvasToScreen();
    }

    private void Update()
    {
        AdjustCanvasToScreen();
    }

    private void AdjustCanvasToScreen()
    {
        float screenAspect = (float)Screen.width / (float)Screen.height;
        
        if (screenAspect == _recordedAspect) return;
        
        float scaleFactor = screenAspect / _defaultAspect;

        if (lockMaxAspect && scaleFactor > 1f) scaleFactor = 1f;
        
        float newWidth = _defaultResolution.x * scaleFactor;
        
        _canvasRectTransform.sizeDelta = new Vector2(newWidth, _defaultResolution.y);

        bool adjust = screenAspect < borderlineAspect;
        float adjustment = adjust ? screenAspect / borderlineAspect : 1f;
        for (int i = 0; i < objectsToAdjust.Count; i++)
        {
            objectsToAdjust[i].localScale = _adjustableObjectsDefaultScales[i] * adjustment;
        }

        _recordedAspect = screenAspect;
    }
}