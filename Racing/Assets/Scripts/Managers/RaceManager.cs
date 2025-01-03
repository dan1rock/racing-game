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
    [SerializeField] private List<float> difficultyFallBehindAdjustment;
    [SerializeField] private List<float> difficultyFallAheadAdjustment;
    
    [SerializeField] private GameObject raceEndMenu;
    [SerializeField] private TMP_Text posText;
    [SerializeField] private TMP_Text lapsText;
    [SerializeField] private TMP_Text summaryPosition;

    [SerializeField] private GameObject leaderboardPos;
    [SerializeField] private GameObject leaderboardDistance;
    [SerializeField] public GameObject leaderboardName;

    [SerializeField] public GameObject playerNameCanvas;

    private bool _finished = false;
    
    public List<CarController> carControllers = new();
    public List<RectTransform> leaderboardPositions = new();
    public List<TMP_Text> leaderboardDistances = new();
    
    private LevelManager _levelManager;
    private RacingLine _racingLine;
    private Coroutine _updatePositionsCoroutine;

    private void Awake()
    {
        _levelManager = FindFirstObjectByType<LevelManager>();
        
        lapsText.text = $"Lap {_levelManager.currentLap} / {_levelManager.laps}";
        leaderboardDistances.Add(leaderboardDistance.GetComponent<TMP_Text>());

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
        
        Invoke(nameof(RecalculatePlayerDistanceLimit), 0.5f);
    }

    private void FixedUpdate()
    {
        if (_levelManager.raceMode != RaceMode.Race) return;
        
        posText.text = $"P{_levelManager.player.currentPosition} / {_levelManager.bots + 1}";
        
        UpdateDistances();

        if (_levelManager.rewind)
        {
            lapsText.text = $"Lap {_levelManager.currentLap} / {_levelManager.laps}";
        }
    }

    private void UpdateDistances()
    {
        float playerDistance = _levelManager.player.GetPlayerDistance();
        
        for (int i = 0; i < leaderboardDistances.Count; i++)
        {
            float diff = playerDistance - carControllers[i].totalDistance;
            
            leaderboardDistances[i].text = (diff > 0f ? "+" : "") + diff.ToString("F0");
        }
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
            
            GameObject ldDistance = Instantiate(leaderboardDistance, leaderboardDistance.transform.parent);
            rt = ldDistance.GetComponent<RectTransform>();
            rt.anchoredPosition += leaderboardOffset;
            
            leaderboardDistances.Add(ldDistance.GetComponent<TMP_Text>());

            ldPos.GetComponent<TMP_Text>().text = $"{leaderboardPositions.Count}:";
        }

        leaderboardName.GetComponent<RectTransform>().anchoredPosition += leaderboardOffset;

        if (GameManager.Get()?.challengeManager)
        {
            leaderboardPos.transform.parent.gameObject.SetActive(false);
        }
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
        
        summaryPosition.text = $"{_levelManager.player.currentPosition}{ending} / {_levelManager.bots + 1}";
        
        raceEndMenu.SetActive(true);
    }

    private void RecalculatePlayerDistanceLimit()
    {
        float distance = _racingLine.lapDistance * (_levelManager.currentLap - 1);

        int checkpointNode = _racingLine.GetNearestNodeID(_levelManager.lastCheckPoint.GetComponent<CheckPoint>().GetNext().transform.position);
        checkpointNode = _racingLine.ForecastRacingNode(checkpointNode, -1);

        distance += _racingLine.CalculateDistanceBetweenNodes(_racingLine.startNodeId, checkpointNode);

        _racingLine.playerDistanceLimit = distance + 50f;
    }

    private int _botsN = 0;
    private CarBot SpawnBot(Vector3 pos, Quaternion rot, float baseSpeed, float steeringReaction, string name)
    {
        Ray ray = new()
        {
            origin = pos + Vector3.up * 10f,
            direction = Vector3.down
        };

        int layerMask = (1 << 7) | (1 << 0) | (1 << 10);
        bool hit = Physics.Raycast(ray, out RaycastHit raycastHit,  100f, layerMask, QueryTriggerInteraction.Ignore);

        CarBot carBot = null;

        GameObject carModel = _levelManager.pickedCar;
        
        if (GameManager.Get()?.challengeManager)
        {
            List<GameObject> cars = GameManager.Get().challengeManager.botCars;
            int id = _botsN == 0 ? 0 : Random.Range(0, cars.Count);
            carModel = cars[id];
        }
        
        if (hit)
        {
            Vector3 rotation = rot.eulerAngles;
            rotation.x = 0f;
            rotation.z = 0f;

            //int carId = Random.Range(0, GameManager.Get().raceCars.Count);
            
            GameObject bot = Instantiate(carModel, raycastHit.point, Quaternion.Euler(rotation));
            carBot = bot.AddComponent<CarBot>();
            carBot.maxAcceleration = baseSpeed;
            carBot.steeringReaction = steeringReaction;
            carBot.playerName = name;
            carBot.fallBehindAdjustment = difficultyFallBehindAdjustment[(int)_levelManager.difficulty];
            carBot.fallAheadAdjustment = difficultyFallAheadAdjustment[(int)_levelManager.difficulty];
            
            bot.GetComponent<Car>().SetRandomColor();
        }

        _botsN++;
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
