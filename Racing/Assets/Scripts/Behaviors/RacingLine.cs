using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class RacingLine : MonoBehaviour
{
    [SerializeField] private int racingLineSegments = 2;
    
    public List<Transform> orderedNodes;

    private void Awake()
    {
        Transform racingLineParent = transform;
        
        Regex regex = new(@"\((\d+)\)$");

        orderedNodes = racingLineParent.Cast<Transform>()
            .OrderBy(t =>
            {
                Match match = regex.Match(t.gameObject.name);
                return match.Success ? int.Parse(match.Groups[1].Value) : 0;
            })
            .ToList();
        
        CreatePerfectRacingLine(racingLineSegments);
    }

    [ContextMenu("InitLine")]
    public void Init()
    {
        Transform racingLineParent = transform;
        
        Regex regex = new(@"\((\d+)\)$");

        orderedNodes = racingLineParent.Cast<Transform>()
            .OrderBy(t =>
            {
                Match match = regex.Match(t.gameObject.name);
                return match.Success ? int.Parse(match.Groups[1].Value) : 0;
            })
            .ToList();
    }
    
    private void CreatePerfectRacingLine(int segmentsPerCurve = 2)
    {
        List<Transform> smoothNodes = new();

        for (int i = 0; i < orderedNodes.Count; i++)
        {
            Transform currentNode = orderedNodes[i];
            Transform nextNode = orderedNodes[(i + 1) % orderedNodes.Count];

            Vector3 p0 = (i == 0) ? currentNode.position : orderedNodes[i - 1].position;
            Vector3 p1 = currentNode.position;
            Vector3 p2 = nextNode.position;
            Vector3 p3 = (i + 2 < orderedNodes.Count) ? orderedNodes[i + 2].position : nextNode.position;

            for (int j = 0; j < segmentsPerCurve; j++)
            {
                float t = (float)j / (float)segmentsPerCurve;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);

                GameObject newNode = new GameObject($"SmoothNode_{smoothNodes.Count}");
                newNode.transform.position = point;
                newNode.transform.parent = transform;
                smoothNodes.Add(newNode.transform);
            }
        }

        orderedNodes = smoothNodes;
    }
    
    public void MultiplyNodes(float tension = 0.5f, int segmentsPerCurve = 2)
    {
        List<Transform> newNodes = new();

        for (int i = 0; i < orderedNodes.Count; i++)
        {
            Transform currentNode = orderedNodes[i];
            Transform nextNode = orderedNodes[(i + 1) % orderedNodes.Count];
            
            Vector3 p0 = (i == 0) ? currentNode.position : orderedNodes[i - 1].position;
            Vector3 p1 = currentNode.position;
            Vector3 p2 = nextNode.position;
            Vector3 p3 = (i + 2 < orderedNodes.Count) ? orderedNodes[i + 2].position : nextNode.position;
            
            for (int j = 0; j <= segmentsPerCurve; j++)
            {
                float t = (float)j / (float)segmentsPerCurve;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);
                GameObject newNode = new($"Node_{newNodes.Count}")
                {
                    transform =
                    {
                        position = point,
                        parent = transform
                    }
                };
                newNodes.Add(newNode.transform);
            }
        }
        
        orderedNodes = newNodes;
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
    }
    
    private void OnDrawGizmos()
    {
        if (orderedNodes == null || orderedNodes.Count < 2)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < orderedNodes.Count; i++)
        {
            Gizmos.DrawSphere(orderedNodes[i].position, 0.2f);
            
            if (i < orderedNodes.Count - 1)
            {
                Gizmos.DrawLine(orderedNodes[i].position, orderedNodes[i + 1].position);
            }
            else
            {
                Gizmos.DrawLine(orderedNodes[i].position, orderedNodes[0].position);
            }
        }
    }
    
    public int GetNearestNodeID(Vector3 targetPosition)
    {
        int nearestID = -1;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < orderedNodes.Count; i++)
        {
            float distance = Vector3.Distance(targetPosition, orderedNodes[i].position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestID = i;
            }
        }

        return nearestID;
    }
}
