using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class RaceManager : MonoBehaviour
{
    [SerializeField] private List<float> difficultyMaxThrottle;
    [SerializeField] private List<float> difficultyThrottleReduction;
    [SerializeField] private List<float> difficultyMaxReaction;
    [SerializeField] private List<float> difficultyReactionReduction;
    
    [SerializeField] private GameObject raceEndMenu;
    [SerializeField] private TMP_Text posText;
    [SerializeField] private TMP_Text lapsText;
    [SerializeField] private TMP_Text summaryPosition;

    [SerializeField] private GameObject leaderboardPos;
    [SerializeField] public GameObject leaderboardName;

    private bool _finished = false;
    
    public List<CarController> carControllers = new();
    public List<RectTransform> leaderboardPositions = new();
    
    private LevelManager _levelManager;
    private RacingLine _racingLine;
    private Coroutine _updatePositionsCoroutine;

    private void Awake()
    {
        _levelManager = FindFirstObjectByType<LevelManager>();
        
        lapsText.text = $"Lap {_levelManager.currentLap} / {_levelManager.laps}";

        if (_levelManager.raceMode == RaceMode.Race)
        {
            _levelManager.OnLapFinish += OnLapFinish;
            _levelManager.OnStageFinish += OnStageFinish;
            _levelManager.OnCheckpoint += RecalculatePlayerDistanceLimit;

            _racingLine = FindFirstObjectByType<RacingLine>();
        }
    }

    private void Start()
    {
        if (_levelManager.raceMode != RaceMode.Race) return;
        
        InitRace();

        carControllers = new List<CarController>(FindObjectsByType<CarController>(FindObjectsSortMode.None));

        _updatePositionsCoroutine = StartCoroutine(UpdatePositions());
    }

    private void FixedUpdate()
    {
        posText.text = $"P{_levelManager.player.currentPosition} / {_levelManager.bots + 1}";
    }

    private void InitRace()
    {
        CarPlayer player = FindFirstObjectByType<CarPlayer>();

        if (player)
        {
            player.leaderboardPos = leaderboardName.GetComponent<RectTransform>();
        }

        int difficulty = (int)_levelManager.difficulty;
        
        float baseSpeed = difficultyMaxThrottle[difficulty];
        float steeringReaction = difficultyMaxReaction[difficulty];

        Vector2 leaderboardOffset = Vector2.zero;
        
        leaderboardPositions.Add(leaderboardPos.GetComponent<RectTransform>());

        foreach (SpawnPos pos in _levelManager.botSpawns)
        {
            string botName = $"Bot{leaderboardPositions.Count}";
            CarBot bot = SpawnBot(pos.position, Quaternion.LookRotation(pos.forward, Vector3.up), baseSpeed,
                steeringReaction, botName);
            baseSpeed -= difficultyThrottleReduction[difficulty];
            steeringReaction *= difficultyReactionReduction[difficulty];
            steeringReaction = Mathf.Clamp(steeringReaction, 0.05f, 1f);
            
            GameObject ldName = Instantiate(leaderboardName, leaderboardName.transform.parent);
            RectTransform rt = ldName.GetComponent<RectTransform>();
            rt.anchoredPosition += leaderboardOffset;
            bot.leaderboardPos = rt;
            ldName.GetComponent<TMP_Text>().text = botName;
            
            leaderboardOffset += Vector2.down * 35f;
            GameObject ldPos = Instantiate(leaderboardPos, leaderboardPos.transform.parent);
            rt = ldPos.GetComponent<RectTransform>();
            rt.anchoredPosition += leaderboardOffset;
            
            leaderboardPositions.Add(rt);

            ldPos.GetComponent<TMP_Text>().text = $"{leaderboardPositions.Count}:";
        }

        leaderboardName.GetComponent<RectTransform>().anchoredPosition += leaderboardOffset;
    }

    private void OnLapFinish()
    {
        if (_levelManager.currentLap <= _levelManager.laps)
        {
            lapsText.text = $"Lap {_levelManager.currentLap} / {_levelManager.laps}";
        }
    }

    private void OnStageFinish()
    {
        _finished = true;

        int pos = _levelManager.player.currentPosition;

        string ending = pos switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
        
        summaryPosition.text = $"{_levelManager.player.currentPosition}{ending}";
        
        raceEndMenu.SetActive(true);
    }

    private void RecalculatePlayerDistanceLimit()
    {
        float distance = _racingLine.totalDistance * (_levelManager.currentLap - 1);

        int checkpointNode = _racingLine.GetNearestNodeID(_levelManager.lastCheckPoint.GetComponent<CheckPoint>().GetNext().transform.position);
        checkpointNode = _racingLine.ForecastRacingNode(checkpointNode, -1);

        distance += _racingLine.CalculateDistanceBetweenNodes(_racingLine.startNodeId, checkpointNode);

        _racingLine.playerDistanceLimit = distance + 50f;
    }

    private CarBot SpawnBot(Vector3 pos, Quaternion rot, float baseSpeed, float steeringReaction, string name)
    {
        Ray ray = new()
        {
            origin = pos + Vector3.up * 10f,
            direction = Vector3.down
        };
        
        bool hit = Physics.Raycast(ray, out RaycastHit raycastHit,  100f, (1 << 7) | (1 << 0), QueryTriggerInteraction.Ignore);

        CarBot carBot = null;
        
        if (hit)
        {
            Vector3 rotation = rot.eulerAngles;
            rotation.x = 0f;
            rotation.z = 0f;

            //int carId = Random.Range(0, GameManager.Get().raceCars.Count);
            
            GameObject bot = Instantiate(_levelManager.pickedCar, raycastHit.point, Quaternion.Euler(rotation));
            carBot = bot.AddComponent<CarBot>();
            carBot.maxAcceleration = baseSpeed;
            carBot.steeringReaction = steeringReaction;
            carBot.name = name;
            
            if (GameManager.Get())
            {
                int color = Random.Range(0, GameManager.Get().carColors.Count);
                bot.GetComponent<Car>().SetColor(GameManager.Get().carColors[color]);
            }
        }
        
        return carBot;
    }
    
    private IEnumerator UpdatePositions()
    {
        while (!_finished)
        {
            carControllers = carControllers.OrderByDescending(car => car.totalDistance).ToList();
            for (int i = 0; i < carControllers.Count; i++)
            {
                carControllers[i].currentPosition = i + 1;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
}
