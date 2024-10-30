using System;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public string name;
    
    public int currentNodeId;
    public int currentLap = 0;
    public float totalDistance;

    public RectTransform leaderboardPos;

    public int currentPosition;

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
        totalDistance = (currentLap - 1) * racingLine.totalDistance;

        if (ForecastRacingNode(-1) != racingLine.startNodeId)
        {
            totalDistance += racingLine.CalculateDistanceBetweenNodes(racingLine.startNodeId, ForecastRacingNode(-2));
        }

        Vector3 flatNode = racingLine.orderedNodes[ForecastRacingNode(-2)].position;
        flatNode.y = 0f;
        Vector3 flatPos = transform.position;
        flatPos.y = 0f;
        
        float dist = (flatPos - flatNode).magnitude;
        totalDistance += dist;
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
}
