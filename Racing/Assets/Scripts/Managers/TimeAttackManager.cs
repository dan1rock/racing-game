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
        }
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
        overallTime += Time.deltaTime;
        
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
        endOverallTimeText.text = overallTimeText.text;
        endBestLapTimeText.text = bestLapTimeText.text;
    }

    private string FormatTime(float secs)
    {
        int minutes = Mathf.FloorToInt(secs / 60);
        int seconds = Mathf.FloorToInt(secs % 60);
        int milliseconds = Mathf.FloorToInt((secs * 1000) % 1000);
        
        return $"{minutes:00}:{seconds:00}:{milliseconds:000}";
    }
}
