using System;
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
    private int index;
    
    [SerializeField] private Transform explorer;
    [SerializeField] private Planet home;
    [SerializeField] private float jumpDuration = 0.1f;
    [SerializeField] private int locationIndex;
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private float iconSize = 0.1f;
    [SerializeField] private bool hasRessource;

    [SerializeField] private ExplorerTask task;
    [SerializeField] public int taskTriangle;
    [SerializeField] public Building taskBuilding;
    
    public void Initialize(Planet home, int startLocationIndex)
    {
        this.home = home;
        transform.parent = home.transform;
        locationIndex = startLocationIndex;
        index = WorldManager.instance.AddExplorer(this);

        SetPositionOnTriangle();

        Debug.Log("index is " + index);
    }

    public void Select()
    {
        icon.sprite = PlayerController.instance.actionSprites[1];
        UIManager.instance.SelectExplorerGui(index);
    }

    public void UnSelect()
    {
        UIManager.instance.UnSelectExplorerGui();
    }
    
    public async void TargetTile(int newLocation)
    {

        var data = CalculatePathFinding(newLocation);
        if (data == null)
        {
            Debug.LogWarning("You can't go there");
            ChangeTask(ExplorerTask.None);
            return;
        }

        int[] reversePath = RecalculatePathFinding(newLocation, data);
        await MoveToTargetPoint(reversePath);

        switch (task)
        {
            case ExplorerTask.Recolting:
                TravelForRecolting();
                break;
            case ExplorerTask.Building:
                TravelForBuilding();
                break;
        }
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
        List<int> currentNeighbours = new List<int>();
        List<int> nextNeighbours = GetNeighbours(calculatingIndex).ToList();

        foreach (var neighbour in nextNeighbours)
        {
            if(!IsNeighbourReachable(locationIndex, neighbour)) continue;
            currentNeighbours.Add(neighbour);
        }
        
        nextNeighbours.Clear();

        // We find target
        while (iterator < 2000)
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
        
        Debug.LogWarning("Boucle Infinie WTF");
        return null;
    }
    
    private int FindClosestHangar()
    {
        if (home.triangles[locationIndex].building == Building.Hangar) return locationIndex;
        
        int iterator = 0;

        Dictionary<int, int>
            pathFindingData = new Dictionary<int, int>
                { { locationIndex, iterator } }; // Key is triangle index, Value is weight

        int calculatingIndex = locationIndex;
        List<int> currentNeighbours = GetNeighbours(calculatingIndex).ToList();
        List<int> nextNeighbours = new List<int>();

        // We find target
        while (iterator < 2000)
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

                if (home.triangles[neighbour].building == Building.Hangar) return neighbour;
            }

            currentNeighbours.Clear();
            foreach (var next in nextNeighbours)
            {
                currentNeighbours.Add(next);
            }

            nextNeighbours.Clear();
            if (currentNeighbours.Count == 0) return -1;
        }

        Debug.LogWarning("Boucle Infinie");
        return -1;
    }
    

    private bool IsNeighbourReachable(int from, int to)
    {
        Triangle start = home.triangles[from];
        Triangle end = home.triangles[to];
        
        Debug.Log(math.abs(start.heightLevel - end.heightLevel));

        if (math.abs(start.heightLevel - end.heightLevel) > 1) return false;
        if (end.heightLevel < home.waterLevel) return false;
        
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
                    if (distance != 0)path.Add(stepBack);
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
            Quaternion rot = explorer.localRotation;

            Vector3 p1, p2, p3, p4;
            p1 = explorer.localPosition;
            p2 = explorer.localPosition + current.normal * 0.4f;
            
            LeaveTriangle();
            locationIndex = reversePath[i];
            Triangle next = home.triangles[locationIndex];
            Quaternion nextRot = Quaternion.LookRotation(next.normal);
            
            p3 = explorer.localPosition + current.normal * 0.4f;
            p4 = home.GetTriangleCenterPoint(locationIndex);

            float timer = 0;
            while (timer < jumpDuration)
            {
                await Task.Yield();
                timer += Time.deltaTime;
                explorer.localPosition = ExBeziers.CubicBeziersCurve(p1, p2, p3, p4, timer / jumpDuration);
                explorer.localRotation = Quaternion.Slerp(rot, nextRot, timer / jumpDuration);
            }
            
            SetPositionOnTriangle();
            await Task.Delay(100);
        }
    }

    #endregion

    public void SetPositionOnTriangle()
    {
        Triangle triangle = home.triangles[locationIndex];
        explorer.localPosition = home.GetTriangleCenterPoint(locationIndex);
        explorer.localRotation = Quaternion.LookRotation(triangle.normal);
        Debug.Log(index + " set on " + locationIndex);
        triangle.explorersOnTriangle.Add(index);
    }

    public void LeaveTriangle()
    {
        Triangle triangle = home.triangles[locationIndex];
        Debug.Log(index + " left " + locationIndex);
        triangle.explorersOnTriangle.Remove(index);
    }

    public int[] GetNeighbours(int index)
    {
        Triangle triangle = home.triangles[index];
        int[] neighbours = new[] { triangle.neighbourA, triangle.neighbourB, triangle.neighbourC };
        return neighbours;
    }
    
    private void Update()
    {
        if (PlayerController.instance.GetCurrentPlanet == home)
        {
            icon.gameObject.SetActive(true);
        }
        else
        {
            icon.gameObject.SetActive(false);
        }
        
        icon.transform.rotation = Quaternion.Euler(0,0,0);
        icon.transform.localScale = Vector3.one *
                                    ((1 * Vector3.Distance(icon.transform.position,
                                        PlayerController.instance.cam.transform.position)) * iconSize);
    }

    public void ChangeTask(ExplorerTask newTask)
    {
        task = newTask;
        switch (newTask)
        {
            case ExplorerTask.None:
                icon.sprite = PlayerController.instance.actionSprites[0];
                break;
            
            case ExplorerTask.Recolting:
                icon.sprite = PlayerController.instance.actionSprites[2];
                break;
            
            case ExplorerTask.Building:
                icon.sprite = PlayerController.instance.actionSprites[3];
                break;
            
            case ExplorerTask.Cargoing:
                icon.sprite = PlayerController.instance.actionSprites[4];
                break;
        }
    }

    public void TravelForRecolting()
    {
        if (!hasRessource)
        {
            if (locationIndex == taskTriangle && home.triangles[taskTriangle].treeLevel > 0)
            {
                hasRessource = true;
                        
                // COUPER UN ARBRE
                home.triangles[locationIndex].treeLevel--;
                Destroy(home.triangles[locationIndex].trees[home.triangles[locationIndex].treeLevel]);
                home.triangles[locationIndex].trees.RemoveAt(home.triangles[locationIndex].treeLevel);

                // TROUVER LE HANGAR LE PLUS PROCHE
                int hangar = FindClosestHangar();
                        
                // Y ALLER
                if (hangar == -1)
                {
                    ChangeTask(ExplorerTask.None);
                    return;
                }

                TargetTile(hangar);
            }
            else if (locationIndex == taskTriangle)
            {
                // PLUS D'ARBRE A COUPER
                // TASK = NONE
                if (home.triangles[home.triangles[taskTriangle].neighbourA].treeLevel > 0)
                {
                    taskTriangle = home.triangles[taskTriangle].neighbourA;
                    TravelForRecolting();
                }
                else if (home.triangles[home.triangles[taskTriangle].neighbourB].treeLevel > 0)
                {
                    taskTriangle = home.triangles[taskTriangle].neighbourB;
                    TravelForRecolting();
                }
                else if (home.triangles[home.triangles[taskTriangle].neighbourC].treeLevel > 0)
                {
                    taskTriangle = home.triangles[taskTriangle].neighbourC;
                    TravelForRecolting();
                }
                else
                {
                    ChangeTask(ExplorerTask.None);   
                }
            }
            else
            {
                TargetTile(taskTriangle);
            }
        }
        else if (hasRessource)
        {
            if (home.triangles[locationIndex].building == Building.Hangar)
            {
                hasRessource = false;
                home.hangarRessourceAmount++;
                home.UpdateHangarsTexts();
                        
                // RETOURNE A SA CASE DE TASK
                TargetTile(taskTriangle);
            }
            else
            {
                int hangar = FindClosestHangar();
                        
                if (hangar == -1)
                {
                    ChangeTask(ExplorerTask.None);
                    return;
                }

                TargetTile(hangar);
            }
        }
    }

    public void TravelForBuilding()
    {
        if (!hasRessource)
        {
            if (home.triangles[locationIndex].building == Building.Hangar && home.hangarRessourceAmount > 0)
            {
                // SUR HANGAR ET RESSOURCE
                
                hasRessource = true;

                // ENLEVER DE LA RESSOURCE AU HANGAR
                home.hangarRessourceAmount--;
                home.UpdateHangarsTexts();
                
                TargetTile(taskTriangle);
            }
            else if (home.triangles[locationIndex].building == Building.Hangar)
            {
                // SUR HANGAR SANS RESSOURCE
                
                // TASK = NONE
                Debug.Log("HANGAR SANS RESSOURCES");
                
                ChangeTask(ExplorerTask.None);
            }
            else
            {
                Debug.Log("FINDING HANGAR");
                
                // SUR AUTRE CASE
                int hangar = FindClosestHangar();
                        
                if (hangar == -1)
                {
                    ChangeTask(ExplorerTask.None);
                    return;
                }
                
                Debug.Log("HANGAR FOUND");
                
                TargetTile(hangar);
            }
        }
        else if (hasRessource)
        {
            if (locationIndex == taskTriangle && home.triangles[locationIndex].building == Building.None)
            {
                Debug.Log("ARRIVEE AU CHANTIER");
                hasRessource = false;

                if (home.triangles[locationIndex].building == Building.None && !home.triangles[locationIndex].constructing)
                {
                    // SI LE BUILDING EST PAS ENCORE EN CONSTRUCTION
                    home.triangles[locationIndex].constructing = true;
                    home.triangles[locationIndex].constructingBuilding = taskBuilding;
                    home.CreateConstructionSign(locationIndex);
                }

                home.triangles[locationIndex].constructionAmount++;
                home.UpdateConstructionSign(locationIndex);

                
                
                
                if (home.triangles[locationIndex].constructionAmount >= home.triangles[locationIndex].constructionGoal)
                {
                    Destroy(home.triangles[locationIndex].constructionSign);
                    home.CreateBuilding(locationIndex,home.triangles[locationIndex].constructingBuilding);
                    ChangeTask(ExplorerTask.None);
                    return;
                }
                
                int hangar = FindClosestHangar();
                        
                if (hangar == -1)
                {
                    ChangeTask(ExplorerTask.None);
                    return;
                }

                TargetTile(hangar);
            }
            else if (locationIndex == taskTriangle)
            {
                ChangeTask(ExplorerTask.None);
            }
            else
            {
                TargetTile(taskTriangle);
            }
        }
    }
}

public enum ExplorerTask
{
    None,
    Recolting,
    Building,
    Cargoing,
}