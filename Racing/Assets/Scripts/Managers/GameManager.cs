using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

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
    public int menuState = 0;
    public Vector2 menuChallengesScrollState = Vector2.zero;

    public QualityLevel graphicsQuality;
    public GraphicsSmoke smokeQuality;
    
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

    public int bots = 4;

    public float masterVolume = 0.2f;

    public int[] challengeData;

    public event Action OnStageLoad;
    
    private AdMobManager _adMobManager;
    public ChallengeManager challengeManager;
    
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
        _adMobManager = FindFirstObjectByType<AdMobManager>();
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
        OnStageLoad?.Invoke();
        
        dayTime = menuManager.selectedDayTime;
        weather = menuManager.selectedWeather;
        stageId = menuManager.selectedStage;
        laps = menuManager.selectedLaps;
        stageReverse = menuManager.reverseToggled;
        carId = menuManager.selectedCarId;
        carColorId = menuManager.selectedCarColorId;
        raceMode = menuManager.selectedRaceMode;
        difficulty = menuManager.selectedDifficulty;
        bots = menuManager.selectedBotCount;

        car = raceMode switch
        {
            RaceMode.Drift => driftCars[carId],
            RaceMode.TimeAttack => raceCars[carId],
            RaceMode.Race => raceCars[carId],
            _ => raceCars[carId]
        };

        gameState = GameState.Stage;

        SavePlayer();
        SceneManager.LoadScene(stageId + 1);
    }

    public void LoadChallenge(ChallengeManager challenge)
    {
        OnStageLoad?.Invoke();
        
        dayTime = challenge.dayTime;
        weather = challenge.weather;
        stageId = challenge.stageId;
        laps = challenge.laps;
        stageReverse = challenge.stageReverse;
        car = challenge.car;

        carId = driftCars.IndexOf(car);
        if (carId == -1)
        {
            carId = raceCars.IndexOf(car);
        }

        carColorId = carColors.IndexOf(challenge.carColor);
        raceMode = challenge.raceMode;
        difficulty = challenge.difficulty;
        bots = challenge.bots;

        challengeManager = challenge;
        DontDestroyOnLoad(challenge.gameObject);
        
        SavePlayer();
        SceneManager.LoadScene(stageId + 1);
    }

    public void LoadMenu()
    {
        if (challengeManager) Destroy(challengeManager.gameObject);
        
        gameState = GameState.Menu;
        SavePlayer();

        SceneManager.LoadScene(0);
    }

    public void ReloadStage()
    {
        SceneManager.LoadScene(stageId + 1);
    }

    public void SavePlayer()
    {
        SaveSystem.SavePlayer(this);
    }

    private void LoadSave()
    {
        try
        {
            PlayerData playerData = SaveSystem.LoadPlayer();
            
            if (playerData == null) return;

            graphicsQuality = playerData.menuSelectedQuality;
            smokeQuality = playerData.graphicsSmokeLevel;
            
            dayTime = playerData.menuSelectedDayTime;
            weather = playerData.menuSelectedWeather;
            stageId = playerData.menuSelectedStageId;
            laps = playerData.menuSelectedLaps;
            stageReverse = playerData.menuSelectedStageReverse;
            carId = playerData.menuSelectedCarId;
            carColorId = playerData.menuSelectedCarColorId;
            raceMode = playerData.menuSelectedRaceMode;
            difficulty = playerData.menuSelectedDifficulty;
            bots = playerData.menuSelectedBotCount;

            masterVolume = playerData.masterVolume;

            challengeData = playerData.challengeData;
            challengeData ??= new int[1024];
            
            if (bots <= 0) bots = 4;
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

    public void SaveGraphicsSettings(Settings settings)
    {
        graphicsQuality = settings.graphicsPreset;
        smokeQuality = settings.smokeQuality;
        SavePlayer();
    }

    public int CountTotalStars()
    {
        int count = 0;
        
        foreach (int challenge in challengeData)
        {
            for (int j = 0; j < 32; j++)
            {
                if ((challenge & (1 << j)) != 0)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
