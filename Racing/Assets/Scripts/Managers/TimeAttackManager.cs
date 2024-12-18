using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeAttackManager : MonoBehaviour
{
    [SerializeField] private TMP_Text overallTimeText;
    [SerializeField] private TMP_Text lapTimeText;
    [SerializeField] private TMP_Text bestLapTimeText;
    [SerializeField] private TMP_Text lapsText;

    [Header("End Menu")] 
    [SerializeField] private GameObject timeAttackEndMenu;
    [SerializeField] private TMP_Text endOverallTimeText;
    [SerializeField] private TMP_Text endBestLapTimeText;
    [SerializeField] private TMP_Text endMainLabel;

    private bool _reverse = false;
    private bool _finished = false;
    public float overallTime = 0f;
    private float _lapTime = 0f;
    private float _bestLapTime = Mathf.Infinity;

    private LevelManager _levelManager;
    
    private void Awake()
    {
        _levelManager = GetComponent<LevelManager>();
        
        overallTimeText.text = FormatTime(0f);
        lapTimeText.text = FormatTime(0f);
        bestLapTimeText.text = "??:??:???";
        lapsText.text = $"Lap {_levelManager.currentLap} / {_levelManager.laps}";

        if (_levelManager.raceMode == RaceMode.TimeAttack)
        {
            _levelManager.OnLapFinish += OnLapFinish;
            _levelManager.OnStageFinish += OnStageFinish;

            if (GameManager.Get()?.challengeManager)
            {
                if (GameManager.Get().challengeManager.timeLimit > 0f)
                {
                    overallTime = GameManager.Get().challengeManager.timeLimit;
                    _reverse = true;
                }
            }
        }
    }

    private void FixedUpdate()
    {
        HandleRewind();
    }

    private readonly List<float> _rewindOverallTimes = new();
    private readonly List<float> _rewindLapTimes = new();
    private readonly List<float> _rewindBestLapTimes = new();
    
    private void HandleRewind()
    {
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
        _rewindOverallTimes.Insert(0, overallTime);
        _rewindLapTimes.Insert(0, _lapTime);
        _rewindBestLapTimes.Insert(0, _bestLapTime);
        
        const int maxRewindSteps = 30 * 60;

        if (_rewindOverallTimes.Count > maxRewindSteps)
        {
            _rewindOverallTimes.RemoveAt(_rewindOverallTimes.Count - 1);
            _rewindLapTimes.RemoveAt(_rewindLapTimes.Count - 1);
            _rewindBestLapTimes.RemoveAt(_rewindBestLapTimes.Count - 1);
        }
    }

    private void RewindState()
    {
        if (_rewindOverallTimes.Count <= 1) return;
        
        overallTime = _rewindOverallTimes[0];
        _rewindOverallTimes.RemoveAt(0);

        _lapTime = _rewindLapTimes[0];
        _rewindLapTimes.RemoveAt(0);

        _bestLapTime = _rewindBestLapTimes[0];
        _rewindBestLapTimes.RemoveAt(0);
        
        lapsText.text = $"Lap {_levelManager.currentLap} / {_levelManager.laps}";
        lapTimeText.text = FormatTime(_lapTime);
        overallTimeText.text = FormatTime(overallTime);

        bestLapTimeText.text = _bestLapTime < Mathf.Infinity ? FormatTime(_bestLapTime) : "??:??:???";
    }

    private void Update()
    {
        if (_levelManager.raceMode != RaceMode.TimeAttack) return;
        if (!_levelManager.carStarted) return;
        
        ProcessTime();
    }

    private void ProcessTime()
    {
        if (_finished) return;
        
        _lapTime += Time.deltaTime;
        overallTime += (_reverse ? -1f : 1f) * Time.deltaTime;
        
        if (overallTime < 0f)
        {
            overallTime = 0f;
            _levelManager.FailStage();
        }
        
        overallTimeText.text = FormatTime(overallTime);
        lapTimeText.text = FormatTime(_lapTime);
    }

    private void OnLapFinish()
    {
        if (_bestLapTime > _lapTime)
        {
            _bestLapTime = _lapTime;
            bestLapTimeText.text = FormatTime(_bestLapTime);
        }
        
        _lapTime = 0f;
        lapTimeText.text = FormatTime(_lapTime);
        
        if (_levelManager.currentLap <= _levelManager.laps)
        {
            lapsText.text = $"Lap {_levelManager.currentLap} / {_levelManager.laps}";
        }
    }

    private void OnStageFinish()
    {
        _finished = true;
        
        timeAttackEndMenu.SetActive(true);
        endOverallTimeText.text = FormatTime(overallTime);
        endBestLapTimeText.text = bestLapTimeText.text;

        if (_reverse)
        {
            endMainLabel.text = "Time left:";
        }
        
        if (GameManager.Get().challengeManager)
        {
            endBestLapTimeText.transform.parent.gameObject.SetActive(false);
        }
    }

    private string FormatTime(float secs)
    {
        int minutes = Mathf.FloorToInt(secs / 60);
        int seconds = Mathf.FloorToInt(secs % 60);
        int milliseconds = Mathf.FloorToInt((secs * 1000) % 1000);
        
        return $"{minutes:00}:{seconds:00}:{milliseconds:000}";
    }
}
