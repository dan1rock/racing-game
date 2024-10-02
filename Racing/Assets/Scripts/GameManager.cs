using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,
    Stage
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    
    public GameState gameState;

    public DayTime dayTime;
    public Weather weather;
    public int stageId;
    public GameObject car;

    private static GameManager _instance;
    
    private void Awake()
    {
        if (_instance)
        {
            DestroyImmediate(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);

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
        car = menuManager.selectedCar;
        
        gameState = GameState.Stage;

        SceneManager.LoadScene(stageId + 1);
    }

    public void LoadMenu()
    {
        gameState = GameState.Menu;
        SceneManager.LoadScene(0);
    }
    
    public void SetCarVolume(float volume)
    {
        audioMixer.SetFloat("CarVolume", Mathf.Lerp(-80.0f, 0.0f, Mathf.Clamp01(volume)));
    }
}
