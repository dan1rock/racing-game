[System.Serializable]
public class PlayerData
{
    public DayTime menuSelectedDayTime;
    public Weather menuSelectedWeather;
    public int menuSelectedStageId;
    public int menuSelectedCarId;

    public PlayerData(GameManager player)
    {
        menuSelectedDayTime = player.dayTime;
        menuSelectedWeather = player.weather;
        menuSelectedStageId = player.stageId;
        menuSelectedCarId = player.carId;
    }
}
