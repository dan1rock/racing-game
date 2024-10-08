using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,
    Stage
}

public enum RaceMode
{
    Drift,
    Race
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> driftCars;
    [SerializeField] private List<GameObject> raceCars;
    [SerializeField] private AudioMixer audioMixer;
    
    public GameState gameState;

    public DayTime dayTime;
    public Weather weather;
    public int stageId;
    public GameObject car;
    public int carId;
    public RaceMode raceMode;

    private static GameManager _instance;
    
    private void Awake()
    {
        if (_instance)
        {
            DestroyImmediate(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        LoadSave();

        Application.targetFrameRate = 60;
        
        _instance = this;

        gameState = GameState.Menu;
    }

    public static GameManager Get()
    {
        return _instance;
    }

    public void LoadStage(MenuManager menuManager)
    {
        dayTime = menuManager.selectedDayTime;
        weather = menuManager.selectedWeather;
        stageId = menuManager.selectedStage;
        carId = menuManager.selectedCarId;
        raceMode = menuManager.selectedRaceMode;

        car = raceMode switch
        {
            RaceMode.Drift => driftCars[carId],
            RaceMode.Race => raceCars[carId],
            _ => car
        };

        gameState = GameState.Stage;

        SaveSystem.SavePlayer(this);
        SceneManager.LoadScene(stageId + 1);
    }

    public void LoadMenu()
    {
        gameState = GameState.Menu;
        SceneManager.LoadScene(0);
    }

    private void LoadSave()
    {
        try
        {
            PlayerData playerData = SaveSystem.LoadPlayer();
            
            if (playerData == null) return;

            dayTime = playerData.menuSelectedDayTime;
            weather = playerData.menuSelectedWeather;
            stageId = playerData.menuSelectedStageId;
            carId = playerData.menuSelectedCarId;
            raceMode = playerData.menuSelectedRaceMode;
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
    }
    
    public void SetCarVolume(float volume)
    {
        audioMixer.SetFloat("CarVolume", Mathf.Lerp(-80.0f, 0.0f, Mathf.Clamp01(volume)));
    }
}
