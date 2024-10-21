using System;
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> menuCars;
    [SerializeField] private Transform carSpawn;
    [SerializeField] private Transform selectedCarSpawn;

    [SerializeField] private GameObject mainMenu;
    
    [Header("Stage Settings")] 
    [SerializeField] private GameObject stageSettings;
    [SerializeField] private Toggle reverseToggle;
    [SerializeField] private Image mapPreview;
    [SerializeField] private Image mapLayout;
    [SerializeField] private TMP_Text mapName;
    [SerializeField] private TMP_Text modeName;
    [SerializeField] private TMP_Text weatherName;
    [SerializeField] private TMP_Text dayTimeName;

    [Header("Car Selection")] 
    [SerializeField] private GameObject carSelection;
    [SerializeField] private TMP_Text selectedCarName;
    [SerializeField] private UICarStats uiCarStats;

    [SerializeField] private CinemachineVirtualCamera mainView;
    [SerializeField] private CinemachineVirtualCamera carSelectView;
    [SerializeField] private CinemachineVirtualCamera stageSelectView;
    
    public int selectedStage;
    public bool reverseToggled;
    public int selectedCarId;
    public int selectedCarColorId;
    public DayTime selectedDayTime;
    public Weather selectedWeather;
    public RaceMode selectedRaceMode;

    private CinemachineVirtualCamera[] _cameras;
    private GameObject _selectedCar;

    private void Awake()
    {
        GameObject car = Instantiate(menuCars[Random.Range(0, menuCars.Count - 1)], carSpawn.position, carSpawn.rotation);
        car.GetComponent<Car>().SetMenuMode();

        selectedCarId = GameManager.Get().carId;
        selectedCarColorId = GameManager.Get().carColorId;
        selectedStage = GameManager.Get().stageId;
        reverseToggled = GameManager.Get().stageReverse;
        selectedDayTime = GameManager.Get().dayTime;
        selectedWeather = GameManager.Get().weather;
        selectedRaceMode = GameManager.Get().raceMode;
        
        reverseToggle.isOn = reverseToggled;

        _cameras = new[] { mainView, carSelectView, stageSelectView };
        
        SetView(mainView);
        
        SpawnSelectedCar();
        
        UpdateSelectedMap();
        UpdateSelectedMode();
        UpdateSelectedTime();
        UpdateSelectedWeather();
    }

    public void SetWeather(int id)
    {
        selectedWeather = (Weather)id;
    }

    public void SetDaytime(int id)
    {
        selectedDayTime = (DayTime)id;
    }
    
    public void SetMode(int id)
    {
        selectedRaceMode = (RaceMode)id;
    }

    public void SetStage(int id)
    {
        selectedStage = id;
    }

    public void SetReverse(bool state)
    {
        reverseToggled = state;
    }

    public void SetCarColor(int id)
    {
        selectedCarColorId = id;
        
        _selectedCar.GetComponent<Car>().SetColor(GameManager.Get().carColors[selectedCarColorId]);
    }

    public void LoadStage()
    {
        GameManager.Get().LoadStage(this);
    }

    public void SetMenuState(int state)
    {
        mainMenu.SetActive(state == 0);
        carSelection.SetActive(state == 1);
        stageSettings.SetActive(state == 2);

        CinemachineVirtualCamera targetView = state switch
        {
            0 => mainView,
            1 => carSelectView,
            2 => stageSelectView,
            _ => mainView
        };

        SetView(targetView);
    }

    public void ScrollCars(bool right)
    {
        selectedCarId += right ? 1 : -1;
        
        if (selectedCarId >= menuCars.Count) selectedCarId = 0;
        if (selectedCarId < 0) selectedCarId = menuCars.Count - 1;
        
        SpawnSelectedCar();
    }
    
    public void ScrollMaps(bool right)
    {
        selectedStage += right ? 1 : -1;
        
        if (selectedStage >= GameManager.Get().mapNames.Count) selectedStage = 0;
        if (selectedStage < 0) selectedStage = GameManager.Get().mapNames.Count - 1;
        
        UpdateSelectedMap();
    }

    public void ScrollMode(bool right)
    {
        selectedRaceMode = ClampEnum(selectedRaceMode, right);
        
        UpdateSelectedMode();
    }

    public void ScrollDayTime(bool right)
    {
        selectedDayTime = ClampEnum(selectedDayTime, right);

        UpdateSelectedTime();
    }
    
    public void ScrollWeather(bool right)
    {
        selectedWeather = ClampEnum(selectedWeather, right);

        UpdateSelectedWeather();
    }

    private void SpawnSelectedCar()
    {
        if (_selectedCar) Destroy(_selectedCar);
        
        _selectedCar = Instantiate(menuCars[selectedCarId], selectedCarSpawn.position, selectedCarSpawn.rotation);
        Car car = _selectedCar.GetComponent<Car>();
        
        car.SetMenuMode(true);
        car.SetColor(GameManager.Get().carColors[selectedCarColorId]);

        selectedCarName.text = car.carName;
        uiCarStats.SetCar(car);
    }

    private void UpdateSelectedMap()
    {
        mapPreview.sprite = GameManager.Get().mapPreviews[selectedStage];
        mapLayout.sprite = GameManager.Get().mapLayouts[selectedStage];
        mapName.text = GameManager.Get().mapNames[selectedStage];
    }

    private void UpdateSelectedTime()
    {
        dayTimeName.text = selectedDayTime.ToString();
    }

    private void UpdateSelectedWeather()
    {
        weatherName.text = selectedWeather.ToString();
    }

    private void UpdateSelectedMode()
    {
        modeName.text = selectedRaceMode.ToString();
    }

    private void SetView(ICinemachineCamera targetCamera)
    {
        foreach (CinemachineVirtualCamera vCam in _cameras)
        {
            vCam.Priority = 10;
        }

        targetCamera.Priority = 20;
    }
    
    public static T ClampEnum<T>(T enumValue, bool increment) where T : Enum
    {
        int enumLength = Enum.GetValues(typeof(T)).Length;
        int currentIndex = Convert.ToInt32(enumValue);
        
        currentIndex += increment ? 1 : -1;
        
        if (currentIndex < 0)
        {
            currentIndex = enumLength - 1;
        }
        else if (currentIndex >= enumLength)
        {
            currentIndex = 0;
        }

        return (T)Enum.ToObject(typeof(T), currentIndex);
    }
}
