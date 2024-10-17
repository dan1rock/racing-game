using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> menuCars;
    [SerializeField] private Transform carSpawn;
    [SerializeField] private Transform selectedCarSpawn;

    [SerializeField] private GameObject mainMenu;
    
    [Header("Stage Settings")] 
    [SerializeField] private GameObject stageSettings;
    [SerializeField] private TMP_Dropdown stageDropdown;
    [SerializeField] private Toggle reverseToggle;
    [SerializeField] private TMP_Dropdown timeDropdown;
    [SerializeField] private TMP_Dropdown weatherDropdown;
    [SerializeField] private TMP_Dropdown raceModeDropdown;

    [Header("Car Selection")] 
    [SerializeField] private GameObject carSelection;
    [SerializeField] private TMP_Text selectedCarName;

    [SerializeField] private CinemachineVirtualCamera mainView;
    [SerializeField] private CinemachineVirtualCamera carSelectView;
    [SerializeField] private CinemachineVirtualCamera stageSelectView;
    
    public int selectedStage;
    public bool reverseToggled;
    public int selectedCarId;
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
        selectedStage = GameManager.Get().stageId;
        reverseToggled = GameManager.Get().stageReverse;
        selectedDayTime = GameManager.Get().dayTime;
        selectedWeather = GameManager.Get().weather;
        selectedRaceMode = GameManager.Get().raceMode;

        stageDropdown.value = selectedStage;
        reverseToggle.isOn = reverseToggled;
        timeDropdown.value = (int)selectedDayTime;
        weatherDropdown.value = (int)selectedWeather;
        raceModeDropdown.value = (int)selectedRaceMode;

        _cameras = new[] { mainView, carSelectView, stageSelectView };
        
        SetView(mainView);
        SpawnSelectedCar();
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

    private void SpawnSelectedCar()
    {
        if (_selectedCar) Destroy(_selectedCar);
        
        _selectedCar = Instantiate(menuCars[selectedCarId], selectedCarSpawn.position, selectedCarSpawn.rotation);
        Car car = _selectedCar.GetComponent<Car>();
        
        car.SetMenuMode(true);

        selectedCarName.text = car.carName;
    }

    private void SetView(ICinemachineCamera targetCamera)
    {
        foreach (CinemachineVirtualCamera vCam in _cameras)
        {
            vCam.Priority = 10;
        }

        targetCamera.Priority = 20;
    }
}
