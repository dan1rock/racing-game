using TMPro;
using UnityEngine;

public class TimeAttackManager : MonoBehaviour
{
    [SerializeField] private TMP_Text overallTimeText;
    [SerializeField] private TMP_Text lapTimeText;
    [SerializeField] private TMP_Text bestLapTimeText;
    [SerializeField] private TMP_Text lapsText;

    private int _currentLap = 1;
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
        lapsText.text = "Lap " + _currentLap;
    }

    private void Update()
    {
        if (_levelManager.raceMode != RaceMode.Race) return;
        if (!_levelManager.carStarted) return;
        
        ProcessTime();
    }

    private void ProcessTime()
    {
        _lapTime += Time.deltaTime;
        overallTime += Time.deltaTime;
        
        overallTimeText.text = FormatTime(overallTime);
        lapTimeText.text = FormatTime(_lapTime);
    }

    public void OnLapFinish()
    {
        _currentLap++;
        
        lapsText.text = "Lap " + _currentLap;

        if (_bestLapTime > _lapTime)
        {
            _bestLapTime = _lapTime;
            bestLapTimeText.text = FormatTime(_bestLapTime);
        }
        
        _lapTime = 0f;
        lapTimeText.text = FormatTime(_lapTime);
    }

    private string FormatTime(float secs)
    {
        int minutes = Mathf.FloorToInt(secs / 60);
        int seconds = Mathf.FloorToInt(secs % 60);
        int milliseconds = Mathf.FloorToInt((secs * 1000) % 1000);
        
        return $"{minutes:00}:{seconds:00}:{milliseconds:000}";
    }
}
