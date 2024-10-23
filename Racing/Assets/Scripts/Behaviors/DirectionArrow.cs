using UnityEngine;

public class DirectionArrow : MonoBehaviour
{
    private Transform _target;

    private void Update()
    {
        if (!_target) return;

        Vector3 flatTarget = _target.position;
        flatTarget.y = 0f;

        Vector3 flatPosition = transform.position;
        flatPosition.y = 0f;

        transform.rotation = Quaternion.LookRotation(flatTarget - flatPosition);
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }
}
