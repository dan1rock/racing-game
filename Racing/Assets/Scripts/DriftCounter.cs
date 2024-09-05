using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DriftCounter : MonoBehaviour
{
    [SerializeField] private float minAngle = 10;
    [SerializeField] private float minSpeed = 10;
    [SerializeField] private float scoreApplyTime = 3f;
    [SerializeField] private AnimationCurve speedMultiplier;
    [SerializeField] private float speedMultiplierRatio = 50f;
    [SerializeField] private List<float> multiplierRaiseTime;

    [SerializeField] private TMP_Text singleDriftScore;
    [SerializeField] private TMP_Text overallDriftScore;
    [SerializeField] private TMP_Text scoreMultiplier;

    private GameObject _singleScore;
    private Animator _scoreAnimator;
    private Animator _multiplierAnimator;
    
    private float _minAngleRads;
    private float _score = 0f;
    private float _overallScore = 0f;
    private float _lastDriftDetected;
    private float _lastDriftFail = -100f;
    private float _driftStart;
    private float _driftDistance = 0f;
    private bool _isDrifting = false;
    
    private int _multiplier = 1;

    private void Awake()
    {
        _singleScore = singleDriftScore.transform.parent.gameObject;
        _scoreAnimator = overallDriftScore.transform.GetComponent<Animator>();
        _multiplierAnimator = scoreMultiplier.transform.parent.GetComponent<Animator>();
        
        _singleScore.SetActive(false);
        
        _minAngleRads = minAngle * Mathf.Deg2Rad;
        singleDriftScore.text = "0";
        overallDriftScore.text = "0";
        scoreMultiplier.text = "1";
    }

    private void Update()
    {
        if (!_isDrifting) return;
        
        if (_multiplier < multiplierRaiseTime.Count)
        {
            if (_driftDistance > multiplierRaiseTime[_multiplier - 1])
            {
                _multiplier += 1;
                scoreMultiplier.text = _multiplier.ToString();
                _multiplierAnimator.Play("UIPop");
            }
        }
        
        if (Time.time - _lastDriftDetected > scoreApplyTime)
        {
            _overallScore += _score * _multiplier;
            _score = 0f;
            _multiplier = 1;
            _driftDistance = 0f;
            
            _scoreAnimator.Play("UIPop");
            _singleScore.SetActive(false);
            singleDriftScore.text = ((int)_score).ToString();
            overallDriftScore.text = ((int)_overallScore).ToString();
            scoreMultiplier.text = _multiplier.ToString();
            _isDrifting = false;
        }
    }

    public void ProcessDrift(float speed, float angle)
    {
        angle = Mathf.Abs(angle);
        if (angle < _minAngleRads) return;
        if (speed < minSpeed) return;
        if (Time.time - _lastDriftFail < 1f) return;
        
        _score += speed * angle * speedMultiplier.Evaluate(Mathf.Clamp01(speed / speedMultiplierRatio));

        singleDriftScore.text = ((int)_score).ToString();

        _driftDistance += speed * 0.001f;
        _lastDriftDetected = Time.time;
        
        if (!_isDrifting)
        {
            _singleScore.SetActive(true);
            _driftStart = Time.time;
            _isDrifting = true;
        }
    }

    public void DriftFailed()
    {
        _score = 0f;
        _multiplier = 1;
        _driftDistance = 0f;
        
        _singleScore.SetActive(false);
        singleDriftScore.text = ((int)_score).ToString();
        overallDriftScore.text = ((int)_overallScore).ToString();
        scoreMultiplier.text = _multiplier.ToString();
        _isDrifting = false;

        _lastDriftFail = Time.time;
    }
}
