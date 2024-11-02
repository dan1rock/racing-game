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
    TimeAttack,
    Race
}

public class GameManager : MonoBehaviour
{
    [SerializeField] public List<GameObject> driftCars;
    [SerializeField] public List<GameObject> raceCars;
    [SerializeField] public List<Material> carColors;
    [SerializeField] public List<Sprite> mapPreviews;
    [SerializeField] public List<Sprite> mapLayouts;
    [SerializeField] public List<string> mapNames;
    [SerializeField] private AudioMixer audioMixer;
    
    public GameState gameState;

    public QualityLevel graphicsQuality;
    public DayTime dayTime;
    public Weather weather;
    public int stageId;
    public int laps;
    public bool stageReverse;
    public GameObject car;
    public int carId;
    public int carColorId;
    public RaceMode raceMode;
    public Difficulty difficulty;

    public Settings settings;
    private AdMobManager _adMobManager;
    
    private static GameManager _instance;
    
    private void Awake()
    {
        if (_instance)
        {
            DestroyImmediate(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        
        _instance = this;
        _adMobManager = FindObjectOfType<AdMobManager>();
        _adMobManager?.InitializeAds();
        
        LoadSave();

        Application.targetFrameRate = 60;

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
        laps = menuManager.selectedLaps;
        stageReverse = menuManager.reverseToggled;
        carId = menuManager.selectedCarId;
        carColorId = menuManager.selectedCarColorId;
        raceMode = menuManager.selectedRaceMode;
        difficulty = menuManager.selectedDifficulty;

        car = raceMode switch
        {
            RaceMode.Drift => driftCars[carId],
            RaceMode.TimeAttack => raceCars[carId],
            RaceMode.Race => raceCars[carId],
            _ => raceCars[carId]
        };

        gameState = GameState.Stage;

        SaveSystem.SavePlayer(this);
        SceneManager.LoadScene(stageId + 1);
    }

    public void LoadMenu()
    {
        gameState = GameState.Menu;
        SaveSystem.SavePlayer(this);
        
        _adMobManager.ShowInterstitialAd(() => SceneManager.LoadScene(0));
    }

    public void ReloadStage()
    {
        SceneManager.LoadScene(stageId + 1);
    }

    private void LoadSave()
    {
        try
        {
            PlayerData playerData = SaveSystem.LoadPlayer();
            
            if (playerData == null) return;

            graphicsQuality = playerData.menuSelectedQuality;
            dayTime = playerData.menuSelectedDayTime;
            weather = playerData.menuSelectedWeather;
            stageId = playerData.menuSelectedStageId;
            laps = playerData.menuSelectedLaps;
            stageReverse = playerData.menuSelectedStageReverse;
            carId = playerData.menuSelectedCarId;
            carColorId = playerData.menuSelectedCarColorId;
            raceMode = playerData.menuSelectedRaceMode;
            difficulty = playerData.menuSelectedDifficulty;
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

    public void SetGraphicsQuality(QualityLevel qualityLevel)
    {
        graphicsQuality = qualityLevel;
        settings.graphicsPreset = qualityLevel;
        settings.ApplySettings();
        SaveSystem.SavePlayer(this);
    }
}
