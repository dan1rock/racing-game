using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> cars;    
    [SerializeField] private List<GameObject> menuCars;
    [SerializeField] private Transform carSpawn;
    
    public int selectedStage;
    public GameObject selectedCar;
    public DayTime selectedDayTime;
    public Weather selectedWeather;

    private void Awake()
    {
        selectedCar = cars[0];

        GameObject car = Instantiate(menuCars[Random.Range(0, menuCars.Count - 1)], carSpawn.position, carSpawn.rotation);
        car.GetComponent<Car>().SetMenuMode();
    }

    public void SetWeather(int id)
    {
        selectedWeather = (Weather)id;
    }

    public void SetDaytime(int id)
    {
        selectedDayTime = (DayTime)id;
    }

    public void SetStage(int id)
    {
        selectedStage = id;
    }

    public void SetCar(int id)
    {
        selectedCar = cars[id];
    }

    public void LoadStage()
    {
        GameManager.Get().LoadStage(this);
    }
}
