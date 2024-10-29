using System;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class RaceManager : MonoBehaviour
{
    [SerializeField] private GameObject raceEndMenu;
    [SerializeField] private TMP_Text lapsText;

    private bool _finished = false;
    
    private LevelManager _levelManager;

    private void Awake()
    {
        _levelManager = FindObjectOfType<LevelManager>();
        
        lapsText.text = $"Lap {_levelManager.currentLap} / {_levelManager.laps}";

        if (_levelManager.raceMode == RaceMode.Race)
        {
            _levelManager.OnLapFinish += OnLapFinish;
            _levelManager.OnStageFinish += OnStageFinish;
            
            InitRace();
        }
    }

    private void InitRace()
    {
        Transform startPos = GameObject.FindWithTag("Respawn").transform;

        float baseSpeed = 1f;
        float steeringReaction = 0.2f;

        foreach (Vector3 pos in _levelManager.botSpawns)
        {
            SpawnBot(pos, startPos.rotation, baseSpeed, steeringReaction);
            baseSpeed -= 0.05f;
            steeringReaction *= 0.8f;
        }
    }

    public void OnLapFinish()
    {
        if (_levelManager.currentLap <= _levelManager.laps)
        {
            lapsText.text = $"Lap {_levelManager.currentLap} / {_levelManager.laps}";
        }
    }

    private void OnStageFinish()
    {
        _finished = true;
        
        raceEndMenu.SetActive(true);
    }

    private void SpawnBot(Vector3 pos, Quaternion rot, float baseSpeed, float steeringReaction)
    {
        Ray ray = new()
        {
            origin = pos + Vector3.up * 10f,
            direction = Vector3.down
        };
        
        bool hit = Physics.Raycast(ray, out RaycastHit raycastHit,  100f, (1 << 7) | (1 << 0), QueryTriggerInteraction.Ignore);

        if (hit)
        {
            Vector3 rotation = rot.eulerAngles;
            rotation.x = 0f;
            rotation.z = 0f;

            int carId = Random.Range(0, GameManager.Get().raceCars.Count);
            
            GameObject bot = Instantiate(_levelManager.pickedCar, raycastHit.point, Quaternion.Euler(rotation));
            CarBot carBot = bot.AddComponent<CarBot>();
            carBot.maxAcceleration = baseSpeed;
            carBot.steeringReaction = steeringReaction;
            
            if (GameManager.Get())
            {
                int color = Random.Range(0, GameManager.Get().carColors.Count);
                bot.GetComponent<Car>().SetColor(GameManager.Get().carColors[color]);
            }
        }
    }
}
