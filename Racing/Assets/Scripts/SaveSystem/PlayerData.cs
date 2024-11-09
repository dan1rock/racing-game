[System.Serializable]
public class PlayerData
{
    public QualityLevel menuSelectedQuality;
    public DayTime menuSelectedDayTime;
    public Weather menuSelectedWeather;
    public int menuSelectedStageId;
    public int menuSelectedLaps;
    public bool menuSelectedStageReverse;
    public int menuSelectedCarId;
    public int menuSelectedCarColorId;
    public RaceMode menuSelectedRaceMode;
    public Difficulty menuSelectedDifficulty;
    public int menuSelectedBotCount;

    public float masterVolume;

    public int[] challengeData;

    public PlayerData(GameManager player)
    {
        menuSelectedQuality = player.graphicsQuality;
        menuSelectedDayTime = player.dayTime;
        menuSelectedWeather = player.weather;
        menuSelectedStageId = player.stageId;
        menuSelectedLaps = player.laps;
        menuSelectedStageReverse = player.stageReverse;
        menuSelectedCarId = player.carId;
        menuSelectedCarColorId = player.carColorId;
        menuSelectedRaceMode = player.raceMode;
        menuSelectedDifficulty = player.difficulty;
        menuSelectedBotCount = player.bots;

        masterVolume = player.masterVolume;

        challengeData = player.challengeData;
    }
}
