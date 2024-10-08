using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> menuCars;
    [SerializeField] private Transform carSpawn;

    [SerializeField] private TMP_Dropdown stageDropdown;
    [SerializeField] private TMP_Dropdown carDropdown;
    [SerializeField] private TMP_Dropdown timeDropdown;
    [SerializeField] private TMP_Dropdown weatherDropdown;
    [SerializeField] private TMP_Dropdown raceModeDropdown;
    
    public int selectedStage;
    public int selectedCarId;
    public DayTime selectedDayTime;
    public Weather selectedWeather;
    public RaceMode selectedRaceMode;

    private void Awake()
    {
        GameObject car = Instantiate(menuCars[Random.Range(0, menuCars.Count - 1)], carSpawn.position, carSpawn.rotation);
        car.GetComponent<Car>().SetMenuMode();

        selectedCarId = GameManager.Get().carId;
        selectedStage = GameManager.Get().stageId;
        selectedDayTime = GameManager.Get().dayTime;
        selectedWeather = GameManager.Get().weather;
        selectedRaceMode = GameManager.Get().raceMode;

        stageDropdown.value = selectedStage;
        carDropdown.value = selectedCarId;
        timeDropdown.value = (int)selectedDayTime;
        weatherDropdown.value = (int)selectedWeather;
        raceModeDropdown.value = (int)selectedRaceMode;
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

    public void SetCar(int id)
    {
        selectedCarId = id;
    }

    public void LoadStage()
    {
        GameManager.Get().LoadStage(this);
    }
}
