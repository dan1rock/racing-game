using UnityEngine;

public class PropFix : MonoBehaviour
{
    private void Awake()
    {
        Ray ray = new()
        {
            origin = transform.position + Vector3.up,
            direction = -Vector3.up
        };

        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, 5f);
        if (hit)
        {
            transform.position = hitInfo.point;
            
            const float maxAngle = 10f * Mathf.Deg2Rad;

            Vector3 limitedUp = Vector3.RotateTowards(Vector3.up, hitInfo.normal, maxAngle, 0f);
            
            transform.up = limitedUp;
        }
    }
}
