using System;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public string playerName;
    
    public int currentNodeId;
    public int currentLap = 0;
    public float totalDistance;

    public RectTransform leaderboardPos;

    public int currentPosition;

    protected bool isPlayer;

    protected RaceManager raceManager;
    protected LevelManager levelManager;
    protected RacingLine racingLine;
    protected Car car;

    private void Awake()
    {
        Initialize();
    }
    
    protected virtual void Initialize()
    {
        levelManager = FindFirstObjectByType<LevelManager>();
        racingLine = FindFirstObjectByType<RacingLine>();
        car = GetComponent<Car>();

        if (levelManager.raceMode == RaceMode.Race)
        {
            raceManager = FindFirstObjectByType<RaceManager>();
        }
    }

    protected void CalculateTotalDistance()
    {
        totalDistance = (currentLap - 1) * racingLine.lapDistance;

        totalDistance += racingLine.CalculateDistanceBetweenNodes(racingLine.startNodeId, ForecastRacingNode(-2));

        if (ForecastRacingNode(-1) == racingLine.startNodeId)
        {
            totalDistance -= racingLine.lapDistance;
        }

        Vector3 flatNode = racingLine.orderedNodes[ForecastRacingNode(-2)].position;
        flatNode.y = 0f;
        Vector3 flatPos = transform.position;
        flatPos.y = 0f;
        
        float dist = (flatPos - flatNode).magnitude;
        totalDistance += dist;

        if (isPlayer)
        {
            totalDistance = Mathf.Min(totalDistance, racingLine.playerDistanceLimit);
        }

        totalDistance = Mathf.Min(totalDistance, racingLine.totalDistance);
    }
    
    protected int ForecastRacingNode(int forecast)
    {
        int nodesN = racingLine.orderedNodes.Count;
        
        int nodeId = currentNodeId + (levelManager.reverse ? -forecast : forecast);
        if (nodeId >= nodesN) nodeId -= nodesN;
        if (nodeId < 0) nodeId += nodesN;

        return nodeId;
    }

    protected void HandleLeaderboardName()
    {
        if (!raceManager) return;
        
        Vector3 newPos = leaderboardPos.position;
        newPos.y = raceManager.leaderboardPositions[currentPosition - 1].position.y;

        leaderboardPos.position = Vector3.Lerp(leaderboardPos.position, newPos, Time.deltaTime * 5f);
    }

    private readonly List<int> _rewindNodeIds = new();
    private readonly List<int> _rewindLaps = new();
    protected void HandleRewind()
    {
        if (!levelManager) return;

        if (levelManager.rewind)
        {
            RewindState();
        }
        else
        {
            RecordState();
        }
    }

    private void RecordState()
    {
        _rewindNodeIds.Insert(0, currentNodeId);
        _rewindLaps.Insert(0, currentLap);
        
        const int maxRewindSteps = 30 * 60;

        if (_rewindNodeIds.Count > maxRewindSteps)
        {
            _rewindNodeIds.RemoveAt(_rewindNodeIds.Count - 1);
            _rewindLaps.RemoveAt(_rewindLaps.Count - 1);
        }
    }

    private void RewindState()
    {
        if (_rewindNodeIds.Count <= 1) return;
        
        currentNodeId = _rewindNodeIds[0];
        _rewindNodeIds.RemoveAt(0);

        currentLap = _rewindLaps[0];
        _rewindLaps.RemoveAt(0);
    }
}
