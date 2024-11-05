using System;
using UnityEngine;

public class CarNameCanvas : MonoBehaviour
{
    private void Update()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

        transform.position = transform.parent.position + transform.parent.up + Vector3.up;
    }
}
