[System.Serializable]
public class PlayerData
{
    public QualityLevel menuSelectedQuality;
    public DayTime menuSelectedDayTime;
    public Weather menuSelectedWeather;
    public int menuSelectedStageId;
    public bool menuSelectedStageReverse;
    public int menuSelectedCarId;
    public int menuSelectedCarColorId;
    public RaceMode menuSelectedRaceMode;

    public PlayerData(GameManager player)
    {
        menuSelectedQuality = player.graphicsQuality;
        menuSelectedDayTime = player.dayTime;
        menuSelectedWeather = player.weather;
        menuSelectedStageId = player.stageId;
        menuSelectedStageReverse = player.stageReverse;
        menuSelectedCarId = player.carId;
        menuSelectedCarColorId = player.carColorId;
        menuSelectedRaceMode = player.raceMode;
    }
}
