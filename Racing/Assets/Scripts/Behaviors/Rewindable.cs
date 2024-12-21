using System.Collections.Generic;
using UnityEngine;

public class Rewindable : MonoBehaviour
{
    private Rigidbody _rb;
    private LevelManager _levelManager;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _levelManager = FindFirstObjectByType<LevelManager>();
    }

    private void FixedUpdate()
    {
        HandleRewind();
    }

    private bool _wasRewinding = false;
    private readonly List<Vector3> _rewindPositions = new();
    private readonly List<Quaternion> _rewindRotations = new();
    private readonly List<Vector3> _rewindVelocities = new();
    private readonly List<Vector3> _rewindAngularVelocities = new();
    
    private void HandleRewind()
    {
        if (!_levelManager) return;
        
        if (_levelManager.rewind)
        {
            RewindState();
        }
        else
        {
            RecordState();
        }
    }

    private void RecordState()
    {
        if (_wasRewinding)
        {
            _rb.isKinematic = false;
            
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.ResetInertiaTensor();
            
            if (_rewindPositions.Count >= 1)
            {
                //transform.position = _rewindPositions[0];
                //transform.rotation = _rewindRotations[0];
                _rb.linearVelocity = _rewindVelocities[0];
                _rb.angularVelocity = _rewindAngularVelocities[0];
            }

            _wasRewinding = false;
        }

        const int maxRewindSteps = 30 * 60;
        
        _rewindPositions.Insert(0, transform.position);
        _rewindRotations.Insert(0, transform.rotation);
        _rewindVelocities.Insert(0, _rb.linearVelocity);
        _rewindAngularVelocities.Insert(0, _rb.angularVelocity);
        
        if (_rewindPositions.Count > maxRewindSteps)
        {
            _rewindPositions.RemoveAt(_rewindPositions.Count - 1);
            _rewindRotations.RemoveAt(_rewindRotations.Count - 1);
            _rewindVelocities.RemoveAt(_rewindVelocities.Count - 1);
            _rewindAngularVelocities.RemoveAt(_rewindAngularVelocities.Count - 1);
        }
    }

    private void RewindState()
    {
        _rb.isKinematic = true;

        if (_rewindPositions.Count <= 1) return;
        
        _rb.MovePosition(_rewindPositions[0]);
        _rewindPositions.RemoveAt(0);
        
        _rb.MoveRotation(_rewindRotations[0]);
        _rewindRotations.RemoveAt(0);
        
        _rewindVelocities.RemoveAt(0);
        _rewindAngularVelocities.RemoveAt(0);

        _wasRewinding = true;
    }
}
