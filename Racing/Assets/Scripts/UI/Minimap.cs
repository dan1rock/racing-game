using UnityEngine;

public class Minimap : MonoBehaviour
{
    public Transform reference1World;
    public Transform reference2World;

    public RectTransform reference1Minimap;
    public RectTransform reference2Minimap;

    public RectTransform playerMarker;
    public Transform playerTransform;

    public float minimapRotation;

    private void Update()
    {
        Vector2 worldPos1 = new(reference1World.position.z, reference1World.position.x);
        Vector2 worldPos2 = new(reference2World.position.z, reference2World.position.x);
        
        Vector2 minimapPos1 = reference1Minimap.anchoredPosition;
        Vector2 minimapPos2 = reference2Minimap.anchoredPosition;
        
        Vector2 worldDelta = worldPos2 - worldPos1;
        Vector2 minimapDelta = minimapPos2 - minimapPos1;

        Vector2 scale = new(minimapDelta.x / worldDelta.x, minimapDelta.y / worldDelta.y);
        
        Vector2 playerWorldPos = new(playerTransform.position.z, playerTransform.position.x);
        
        Vector2 offsetWorld = playerWorldPos - worldPos1;
        
        float rotationRadians = minimapRotation * Mathf.Deg2Rad;
        Vector2 rotatedOffsetWorld = new(
            offsetWorld.x * Mathf.Cos(rotationRadians) - offsetWorld.y * Mathf.Sin(rotationRadians),
            offsetWorld.x * Mathf.Sin(rotationRadians) + offsetWorld.y * Mathf.Cos(rotationRadians)
        );
        
        Vector2 offsetMinimap = new(rotatedOffsetWorld.x * scale.x, rotatedOffsetWorld.y * scale.y);
        
        playerMarker.anchoredPosition = minimapPos1 + offsetMinimap;
    }
}
