using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class ExplorerBehaviour : MonoBehaviour
{
    [SerializeField] private Transform explorer;
    [SerializeField] private Planet home;
    [SerializeField] private int locationIndex;
    [SerializeField] private int DEBUG_Target;

    [ContextMenu("Go on target")]
    public async void Init()
    {
        var data = CalculatePathFinding(DEBUG_Target);
        if (data == null)
        {
            Debug.LogWarning("You can't go there");
            return;
        }

        int[] reversePath = RecalculatePathFinding(DEBUG_Target, data);
        await MoveToTargetPoint(reversePath);
    }

    #region Path Finding

    [CanBeNull]
    private Dictionary<int, int> CalculatePathFinding(int targetLocation)
    {
        int iterator = 0;

        Dictionary<int, int>
            pathFindingData = new Dictionary<int, int>
                { { locationIndex, iterator } }; // Key is triangle index, Value is weight

        int calculatingIndex = locationIndex;
        List<int> currentNeighbours = GetNeighbours(calculatingIndex).ToList();
        List<int> nextNeighbours = new List<int>();

        // We find target
        while (true)
        {
            iterator++;

            foreach (var neighbour in currentNeighbours)
            {
                if (pathFindingData.ContainsKey(neighbour)) continue; // Already shorter path found, index ignored

                // We'll check this index neighbours later
                int[] next = GetNeighbours(neighbour);
                foreach (var n in next)
                {
                    if (!IsNeighbourReachable(neighbour, n)) continue;
                    
                    nextNeighbours.Add(n);
                }

                pathFindingData.Add(neighbour, iterator);

                if (neighbour == targetLocation) return pathFindingData;
            }

            currentNeighbours.Clear();
            foreach (var next in nextNeighbours)
            {
                currentNeighbours.Add(next);
            }

            nextNeighbours.Clear();
            if (currentNeighbours.Count == 0) return null;
        }
    }

    private bool IsNeighbourReachable(int from, int to)
    {
        Triangle start = home.triangles[from];
        Triangle end = home.triangles[to];

        if (math.abs(start.heightLevel - end.heightLevel) > 1) return false;
        
        return true;
    }

    private int[] RecalculatePathFinding(int targetLocation, Dictionary<int, int> data)
    {
        List<int> path = new List<int> { targetLocation };
        int maxDistance = data[targetLocation];
        int stepBack = targetLocation;

        while (true)
        {
            int[] neighbours = GetNeighbours(stepBack);
            foreach (var neighbour in neighbours)
            {
                if (!data.ContainsKey(neighbour)) continue;

                int distance = data[neighbour];
                if (distance == maxDistance - 1)
                {
                    stepBack = neighbour;
                    path.Add(stepBack);
                    if (distance == 0) return path.ToArray();
                    maxDistance--;
                    break;
                }
            }
        }
    }

    private async Task MoveToTargetPoint(int[] reversePath)
    {
        for (int i = reversePath.Length - 1; i >= 0; i--)
        {            
            Triangle current = home.triangles[locationIndex];
            Quaternion rot = explorer.rotation;

            Vector3 p1, p2, p3, p4;
            p1 = explorer.position;
            p2 = explorer.position + current.normal;
            
            locationIndex = reversePath[i];
            Triangle next = home.triangles[locationIndex];
            Quaternion nextRot = Quaternion.LookRotation(next.normal);
            
            p3 = explorer.position + current.normal;
            p4 = next.centralPoint + next.normal * next.heightLevel * home.heightSize;

            float timer = 0;
            while (timer < 0.2f)
            {
                await Task.Yield();
                timer += Time.deltaTime;
                explorer.position = ExBeziers.CubicBeziersCurve(p1, p2, p3, p4, timer / 0.2f);
                explorer.rotation = Quaternion.Slerp(rot, nextRot, timer / 0.2f);
            }
            
            SetPositionOnTriangle();
            await Task.Delay(100);
        }
    }

    #endregion

    [ContextMenu("Set on position")]
    public void SetPositionOnTriangle()
    {
        Triangle triangle = home.triangles[locationIndex];
        explorer.position = triangle.centralPoint + triangle.normal * triangle.heightLevel * home.heightSize;
        explorer.rotation = Quaternion.LookRotation(triangle.normal);
    }

    public int[] GetNeighbours(int index)
    {
        Triangle triangle = home.triangles[index];
        int[] neighbours = new[] { triangle.neighbourA, triangle.neighbourB, triangle.neighbourC };
        return neighbours;
    }
}