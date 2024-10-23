using UnityEngine;

public class WorldSpaceCanvasScaler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private bool lockMaxAspect;

    private Vector2 _defaultResolution;
    private float _defaultAspect;

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

        AdjustCanvasToScreen();
    }

    private void Update()
    {
        AdjustCanvasToScreen();
    }

    private void AdjustCanvasToScreen()
    {
        float screenAspect = (float)Screen.width / (float)Screen.height;
        
        float scaleFactor = screenAspect / _defaultAspect;

        if (lockMaxAspect && scaleFactor > 1f) scaleFactor = 1f;
        
        float newWidth = _defaultResolution.x * scaleFactor;
        
        _canvasRectTransform.sizeDelta = new Vector2(newWidth, _defaultResolution.y);
    }
}