using UnityEngine;

public class PropFix : MonoBehaviour
{
    [ContextMenu("Fix This")]
    public void Fix()
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

            Quaternion currentRotation = transform.rotation;
            
            Vector3 limitedUp = Vector3.RotateTowards(Vector3.up, hitInfo.normal, maxAngle, 0f);
            
            Quaternion targetRotation = Quaternion.LookRotation(transform.forward, limitedUp);
            
            transform.rotation = Quaternion.Euler(currentRotation.eulerAngles.x, currentRotation.eulerAngles.y, targetRotation.eulerAngles.z);
        }
    }

    [ContextMenu("Fix All")]
    public void FixAll()
    {
        PropFix[] propFixes = FindObjectsOfType<PropFix>();

        foreach (PropFix propFix in propFixes)
        {
            propFix.Fix();
        }
    }
}
