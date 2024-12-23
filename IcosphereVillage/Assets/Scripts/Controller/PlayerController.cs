using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoSingleton<PlayerController>
{
    [SerializeField] private Planet currentPlanet;
    private int currentPlanetIndex;
    
    public bool isActive;
    
    // Selection Wheel
    public bool isSelecting;
    public int selection;
    public int selectionTriangle;
    public ExplorerBehaviour selectionExplorer;

    public Vector3 mousePosMem;

    // Rotation
    private Vector2 mouseMove;
    private bool isDraggingPlanet;
    [SerializeField] private float rotateSensibility;

    // Zoom
    [SerializeField] private Transform camera;
    public Camera cam;
    [SerializeField] private float baseDistance;
    [SerializeField] private Vector2 minMaxDistanceSub0;
    [SerializeField] private Vector2 minMaxDistanceSub1;
    private Vector2 minMaxDistanceSub;
    [SerializeField] private float cameraSpeed;
    [SerializeField] private float stepValue;
    private float currentDistance;
    private Vector2 scroll;

    // Explorer Selection
    public Sprite[] actionSprites;
    public Sprite[] buildingSprites;

    // Rocket
    [SerializeField] private float holdDuration;
    [SerializeField] private float maxRocketSize;
    private bool isHolding;
    private bool isZoomingRocket;
    private Transform rocket;

    public void Initialize(Planet target)
    {
        SetCurrentPlanet(target);
        currentDistance = baseDistance;
        cam = camera.GetComponent<Camera>();
        currentTriangle = null;
        isActive = true;
    }

    public void SetCurrentPlanet(Planet p)
    {
        currentPlanet = p;
        minMaxDistanceSub = p.subdivisions == 2 ? minMaxDistanceSub0 : minMaxDistanceSub1;
        SetDistance(0);
        UIManager.instance.RefreshPlanetInfoGui(p.planetName, p.biome.biomeName, p.biome.groundColor, currentPlanetIndex + 1, (int)p.seed);
    }

    public Planet GetCurrentPlanet => currentPlanet;

    private bool IsReadingInput()
    {
        if (!isActive || isZoomingRocket) return false;
        return true;
    }

    public void OnRotate(InputAction.CallbackContext ctx)
    {
        if (!IsReadingInput()) return;
        if (!ctx.performed) return;
        mouseMove = ctx.ReadValue<Vector2>();
    }

    public void OnDragPlanet(InputAction.CallbackContext ctx)
    {
        if (!IsReadingInput()) return;
        if (ctx.started) isDraggingPlanet = true;
        else if (ctx.canceled) isDraggingPlanet = false;
    }

    public void OnScroll(InputAction.CallbackContext ctx)
    {
        if (!IsReadingInput()) return;
        if (!ctx.performed) return;
        scroll = ctx.ReadValue<Vector2>();
        SetDistance(-scroll.y);
    }

    public void OnClick(InputAction.CallbackContext ctx)
    {
        if (!IsReadingInput()) return;
        if (ctx.canceled) isHolding = false;
        if (!ctx.performed) return;
        isHolding = true;
        if (!isSelecting) ClickOnTile();
        else SelectOption();
    }
    
    public void OnRightClick(InputAction.CallbackContext ctx)
    {
        if (!IsReadingInput()) return;
        if (ctx.started) mousePosMem = Input.mousePosition;
        if (ctx.canceled && Input.mousePosition == mousePosMem)
        {
            
            if (!isSelecting)
            {
                if (currentTriangleIndex != -1 && currentPlanet.hangarRessourceAmount > 0)
                {
                    currentPlanet.hangarRessourceAmount--;
                    currentPlanet.CreateWaterPlatform(currentTriangleIndex);
                }
            }   
        }
    }

    public void SelectOption()
    {
        isSelecting = false;
        switch (selection)
        {
            case 0:
                Debug.Log("GO TO TILE");
                selectionExplorer.TargetTile(selectionTriangle);
                selectionExplorer.ChangeTask(ExplorerTask.None);
                break;
            case 1:
                Debug.Log("TASK SET TO BUILDING");
                selectionExplorer.taskTriangle = selectionTriangle;
                selectionExplorer.taskBuilding = Building.Rocket;
                selectionExplorer.ChangeTask(ExplorerTask.Building);
                selectionExplorer.TravelForBuilding();
                break;
            case 2:
                Debug.Log("TASK SET TO BUILDING");
                selectionExplorer.taskTriangle = selectionTriangle;
                selectionExplorer.taskBuilding = Building.Hangar;
                selectionExplorer.ChangeTask(ExplorerTask.Building);
                selectionExplorer.TravelForBuilding();
                break;
            case 3:
                Debug.Log("TASK SET TO BUILDING");
                selectionExplorer.taskTriangle = selectionTriangle;
                selectionExplorer.taskBuilding = Building.House;
                selectionExplorer.ChangeTask(ExplorerTask.Building);
                selectionExplorer.TravelForBuilding();
                break;
        }
        UIManager.instance.HideChoiceWheel();
    }
    public void UpdatePlayer()
    {
        if (!isActive) return;
        if (isDraggingPlanet) RotateAroundDraggedPlanet();
        DetectCurrentTriangle();
        DrawCursor();
    }

    private void LateUpdate()
    {
        if (!isActive) return;
        Zoom();
    }

    private void RotateAroundDraggedPlanet()
    {
        currentPlanet.transform.RotateAround(Vector3.up, math.radians(-mouseMove.x * rotateSensibility));
        currentPlanet.transform.RotateAround(Vector3.right, math.radians(mouseMove.y * rotateSensibility));
    }

    private void SetDistance(float delta)
    {
        currentDistance += delta * stepValue;
        currentDistance = math.clamp(currentDistance, minMaxDistanceSub.x, minMaxDistanceSub.y);
    }

    private void Zoom()
    {
        Vector3 targetPos;
        if (isZoomingRocket)
        {
            targetPos = rocket.position - Vector3.forward * baseDistance;
        }
        else
        {
            targetPos = currentPlanet.transform.position - Vector3.forward * baseDistance -
                        camera.forward * currentDistance;
        }
        camera.position = Vector3.Lerp(camera.position, targetPos, cameraSpeed * Time.deltaTime);
    }

    public void SetZoomOnRocket(Transform target)
    {
        isZoomingRocket = true;
        rocket = target;
    }

    public void ExitRocketZoom()
    {
        isZoomingRocket = false;
        rocket = null;
    }

    public void SetNewTarget(int i)
    {
        currentPlanetIndex = i;
        SetCurrentPlanet(WorldManager.instance.GetPlanet(i));
    }

    public int currentTriangleIndex = -1;
    public Triangle currentTriangle;
    public MeshFilter cursorFilter;
    private Mesh cursorMesh;
    public ExplorerBehaviour selectedExplorer;

    public void DetectCurrentTriangle()
    {
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray.origin, ray.direction, out var hit, 100))
        {
            SetNoTriangle();
            return;
        }
        
        Triangle triangle;
        for (int i = 0; i < currentPlanet.triangles.Count; i++)
        {
            triangle = currentPlanet.triangles[i];
            if (IsCursorOverTriangle(triangle, hit.normal, hit.point, i))
            {
                currentTriangle = triangle;
                currentTriangleIndex = i;
                UIManager.instance.RefreshCurrentTileInfoGui(currentTriangleIndex, currentTriangle.trees.Count);
                return;
            }
        }

        SetNoTriangle();
    }

    private void SetNoTriangle()
    {
        currentTriangle = null;
        currentTriangleIndex = -1;
        UIManager.instance.RefreshCurrentTileInfoGui(currentTriangleIndex, 0);
    }

    public bool IsCursorOverTriangle(Triangle triangle, Vector3 normal, Vector3 point, int i)
    {
        Vector3 recalculatedNormal = (currentPlanet.transform.TransformDirection(triangle.elevationNormal));
        Vector3 recalculatedCenter = currentPlanet.transform.TransformPoint(currentPlanet.GetTriangleCenterPoint(i));

        bool dotTest = math.dot(normal, recalculatedNormal) >= 0.999;
        bool distanceTest = Vector3.Distance(recalculatedCenter, point) < 1;
        
        if (dotTest  && distanceTest) return true;
        return false;
    }

    public void DrawCursor()
    {
        if (currentTriangleIndex == -1)
        {
            cursorFilter.mesh = null;
            return;
        }

        cursorFilter.mesh = null;

        Vector3 v1, v2, v3;
        if (currentTriangle.heightLevel == 0 || currentTriangle.building == Building.Bridge)
        {
            v1 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.indices[0]]);
            v2 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.indices[1]]);
            v3 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.indices[2]]);
        }
        else
        {
            v1 = currentPlanet.transform.TransformPoint(
                currentPlanet.vertices[currentTriangle.elevationTriangle[^1].x]);
            v2 = currentPlanet.transform.TransformPoint(
                currentPlanet.vertices[currentTriangle.elevationTriangle[^1].y]);
            v3 = currentPlanet.transform.TransformPoint(
                currentPlanet.vertices[currentTriangle.elevationTriangle[^1].z]);
        }

        v1 += currentPlanet.transform.TransformDirection(currentTriangle.elevationNormal * 0.01f);
        v2 += currentPlanet.transform.TransformDirection(currentTriangle.elevationNormal * 0.01f);
        v3 += currentPlanet.transform.TransformDirection(currentTriangle.elevationNormal * 0.01f);

        Vector3[] vertices = new Vector3[]
        {
            v1, v2, v3
        };
        cursorMesh = new Mesh
        {
            vertices = vertices,
            triangles = new[] { 0, 1, 2 }
        };
        cursorMesh.RecalculateNormals();

        cursorFilter.mesh = cursorMesh;
    }

    private async void ClickOnTile()
    {
        if (currentTriangle == null || currentPlanet == null) return;

        if (selectedExplorer == null)
        {
            if (currentTriangle.explorersOnTriangle.Count > 0)
            {
                SetSelectedExplorer();
            }
        }
        else
        {
            if (currentPlanet.triangles[currentTriangleIndex].treeLevel > 0)
            {
                selectedExplorer.taskTriangle = currentTriangleIndex;
                selectedExplorer.ChangeTask(ExplorerTask.Recolting);
                selectedExplorer.TravelForRecolting();
                
            }
            else if (currentPlanet.triangles[currentTriangleIndex].constructing)
            {
                selectedExplorer.taskTriangle = currentTriangleIndex;
                selectedExplorer.ChangeTask(ExplorerTask.Building);
                selectedExplorer.TravelForBuilding();
                
            }
            else if (currentPlanet.triangles[currentTriangleIndex].heightLevel >= currentPlanet.waterLevel)
            {
                isSelecting = true;
                selectionTriangle = currentTriangleIndex;
                selectionExplorer = selectedExplorer;
                UIManager.instance.SetChoiceWheel();
            }
            else
            {
                selectedExplorer.ChangeTask(ExplorerTask.None);
            }
            
            UnSelectExplorer();
            
        }

        if (currentTriangle.building == Building.Rocket)
        {
            int saveIndex = currentTriangleIndex;
            Transform rocket = currentPlanet.rocketByIndex[saveIndex];
            float holdTimer = 0;
            while (holdTimer < holdDuration)
            {
                holdTimer += Time.deltaTime;
                rocket.localScale = Vector3.one * math.lerp(1, maxRocketSize, holdTimer / holdDuration);
                await Task.Yield();
                if (!isHolding)
                {
                    rocket.localScale = Vector3.one;
                    return;
                }
            }

            WorldManager.instance.LaunchAircraft(currentPlanet, saveIndex);
        }
    }

    private void SetSelectedExplorer()
    {
        int index = currentTriangle.explorersOnTriangle[0];
        selectedExplorer = WorldManager.instance.GetExplorer(index);
        selectedExplorer.Select();
    }

    private void UnSelectExplorer()
    {
        selectedExplorer.UnSelect();
        selectedExplorer = null;
    }
}