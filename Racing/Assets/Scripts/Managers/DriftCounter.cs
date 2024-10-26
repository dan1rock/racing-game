using System.Collections;
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

    [SerializeField] private TMP_Text lapsText;
    [SerializeField] private TMP_Text singleDriftScore;
    [SerializeField] private TMP_Text overallDriftScore;
    [SerializeField] private TMP_Text addedDriftScore;
    [SerializeField] private TMP_Text scoreMultiplier;
    
    [Header("End Menu")]
    [SerializeField] private GameObject driftEndMenu;
    [SerializeField] private TMP_Text endOverallScoreText;
    [SerializeField] private TMP_Text endBiggestSingleScoreText;

    private GameObject _singleScore;
    private GameObject _addedScore;
    private Animator _scoreAnimator;
    private Animator _multiplierAnimator;
    private LevelManager _levelManager;
    
    private float _minAngleRads;
    private float _score = 0f;
    private float _overallScore = 0f;
    private float _bestSingleScore = 0f;
    
    private float _lastDriftDetected;
    private float _lastDriftFail = -100f;
    private float _driftStart;
    private float _driftDistance = 0f;
    private float _nextMultiplierRaiseDistance = 0f;
    private bool _isDrifting = false;
    
    private int _multiplier = 1;
    
    private bool _finished = false;

    private void Awake()
    {
        _singleScore = singleDriftScore.transform.parent.gameObject;
        _addedScore = addedDriftScore.gameObject;
        _scoreAnimator = overallDriftScore.transform.GetComponent<Animator>();
        _multiplierAnimator = scoreMultiplier.transform.parent.GetComponent<Animator>();
        _levelManager = GetComponent<LevelManager>();
        
        _singleScore.SetActive(false);
        _addedScore.SetActive(false);
        
        _minAngleRads = minAngle * Mathf.Deg2Rad;
        singleDriftScore.text = "0";
        overallDriftScore.text = "0";
        scoreMultiplier.text = "x1";
        lapsText.text = $"Lap {_levelManager.currentLap} / {_levelManager.laps}";
        
        InitMultiplierSystem();
        
        _nextMultiplierRaiseDistance = multiplierRaiseTime[0];
        
        if (_levelManager.raceMode == RaceMode.Drift)
        {
            _levelManager.OnLapFinish += OnLapFinish;
            _levelManager.OnStageFinish += OnStageFinish;
        }
    }

    private void InitMultiplierSystem()
    {
        List<float> newList = new();

        const float lengthRatio = 0.5f;
        const int numPerElement = 2;

        foreach (float length in multiplierRaiseTime)
        {
            for (int i = 0; i < numPerElement; i++)
            {
                newList.Add(length * lengthRatio);
            }
        }

        multiplierRaiseTime = newList;
    }

    private void Update()
    {
        if (_levelManager.raceMode != RaceMode.Drift) return;
        
        if (!_isDrifting) return;
        
        HandleMultiplier();
        
        if (Time.time - _lastDriftDetected > scoreApplyTime)
        {
            StartCoroutine(ApplyScore());
        }
    }

    private void HandleMultiplier()
    {
        if (_multiplier >= 50) return;
        
        if (_driftDistance > _nextMultiplierRaiseDistance)
        {
            _multiplier += 1;
            scoreMultiplier.text = "x" + _multiplier;
            _multiplierAnimator.Play("UIPop");

            int nextMultiplier = _multiplier < multiplierRaiseTime.Count
                ? _multiplier - 1
                : multiplierRaiseTime.Count - 1;
            
            _nextMultiplierRaiseDistance += multiplierRaiseTime[nextMultiplier];
        }
    }

    private IEnumerator ApplyScore()
    {
        float addedScore = _score * _multiplier;
        _overallScore += addedScore;
        
        _scoreAnimator.Play("UIPop");
        _singleScore.SetActive(false);
        overallDriftScore.text = ((int)_overallScore).ToString();
        scoreMultiplier.text = "x1";
        addedDriftScore.text = "+" + (int)(_score * _multiplier);

        if (_bestSingleScore < addedScore) _bestSingleScore = addedScore;
        
        _score = 0f;
        _multiplier = 1;
        _driftDistance = 0f;
        _nextMultiplierRaiseDistance = multiplierRaiseTime[0];
        _isDrifting = false;
        
        _addedScore.SetActive(true);

        yield return new WaitForSeconds(1f);
        
        _addedScore.SetActive(false);
    }

    public void ProcessDrift(float speed, float angle)
    {
        if (_finished) return;
        if (_levelManager.wrongDirectionActive) return;
        
        angle = Mathf.Abs(angle);
        if (angle < _minAngleRads) return;
        if (speed < minSpeed) return;
        if (Time.time - _lastDriftFail < 1f) return;
        
        _score += speed * angle * speedMultiplier.Evaluate(Mathf.Clamp01(speed / speedMultiplierRatio)) * 0.1f;

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

    public void OnDriftFail()
    {
        if (!_isDrifting) return;
        
        _score = 0f;
        _multiplier = 1;
        _driftDistance = 0f;
        _nextMultiplierRaiseDistance = multiplierRaiseTime[0];
        
        _singleScore.SetActive(false);
        overallDriftScore.text = ((int)_overallScore).ToString();
        scoreMultiplier.text = "x1";
        _isDrifting = false;

        _lastDriftFail = Time.time;

        StartCoroutine(DriftFailAnimation());
    }
    
    public void OnLapFinish()
    {
        if (_levelManager.currentLap <= _levelManager.laps)
        {
            lapsText.text = $"Lap {_levelManager.currentLap} / {_levelManager.laps}";
        }
    }

    private void OnStageFinish()
    {
        _finished = true;
        
        StartCoroutine(ApplyScore());
        
        driftEndMenu.SetActive(true);
        endOverallScoreText.text = ((int)_overallScore).ToString();
        endBiggestSingleScoreText.text = ((int)_bestSingleScore).ToString();
    }

    private IEnumerator DriftFailAnimation()
    {
        _addedScore.SetActive(true);
        addedDriftScore.text = "Drift Failed";

        yield return new WaitForSeconds(1f);

        _addedScore.SetActive(false);
    }
}
