using System;
using System.Collections;
using UnityEngine;

public class KnockDownObject : MonoBehaviour
{
    private bool _isActive = false;
    private bool _isKnocked = false;
    private Vector3 _basePosition;
    
    private KnockManager _knockManager;

    private void Awake()
    {
        _knockManager = FindFirstObjectByType<KnockManager>();
        _knockManager.RegisterObject(this);

        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return new WaitForSeconds(5f);

        _basePosition = transform.position;
        _isActive = true;

        StartCoroutine(CheckKnockState());
    }

    private IEnumerator CheckKnockState()
    {
        while (!_isKnocked)
        {
            float diff = (transform.position - _basePosition).magnitude;
            if (diff > 0.1f)
            {
                _isKnocked = true;
                
                _knockManager.ObjectKnocked(this);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }
}
