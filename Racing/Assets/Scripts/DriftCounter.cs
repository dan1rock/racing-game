using System;
using TMPro;
using UnityEngine;

public class DriftCounter : MonoBehaviour
{
    [SerializeField] private float minAngle = 10;
    [SerializeField] private float minSpeed = 10;
    [SerializeField] private AnimationCurve speedMultiplier;
    [SerializeField] private float speedMultiplierRatio = 50f;

    [SerializeField] private TMP_Text singleDriftScore;

    private float _minAngleRads;
    private float _score = 0f;

    private void Awake()
    {
        _minAngleRads = minAngle * Mathf.Deg2Rad;
        singleDriftScore.text = "0";
    }

    public void ProcessDrift(float speed, float angle)
    {
        angle = Mathf.Abs(angle);
        if (angle < _minAngleRads) return;
        if (speed < minSpeed) return;
        
        _score += speed * angle * speedMultiplier.Evaluate(Mathf.Clamp01(speed / speedMultiplierRatio));

        singleDriftScore.text = ((int)_score).ToString();
    }
}
