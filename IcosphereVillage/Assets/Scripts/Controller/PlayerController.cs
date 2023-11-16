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

    public bool isActive;

    // Rotation
    private Vector2 mouseMove;
    private bool isDraggingPlanet;
    [SerializeField] private float rotateSensibility;

    // Zoom
    [SerializeField] private Transform camera;
    public Camera cam;
    [SerializeField] private float baseDistance;
    [SerializeField] private Vector2 minMaxDistance;
    [SerializeField] private float cameraSpeed;
    [SerializeField] private float stepValue;
    private float currentDistance;
    private Vector2 scroll;

    // Explorer Selection
    public Sprite[] actionSprites;

    // Rocket
    [SerializeField] private float holdDuration;
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
        ClickOnTile();
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
        currentDistance = math.clamp(currentDistance, minMaxDistance.x, minMaxDistance.y);
    }

    private void Zoom()
    {
        Vector3 targetPos;
        if (isZoomingRocket)
        {
            targetPos = rocket.position - Vector3.forward * baseDistance;
            camera.position = Vector3.Lerp(camera.position, targetPos, cameraSpeed * Time.deltaTime);

        }
        else
        {
            targetPos = currentPlanet.transform.position - Vector3.forward * baseDistance -
                        camera.forward * currentDistance;
            camera.position = targetPos;

        }

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
        currentPlanet = WorldManager.instance.GetPlanet(i);
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

        //cursor.position = hit.point;
        
        Triangle triangle;
        for (int i = 0; i < currentPlanet.triangles.Count; i++)
        {
            triangle = currentPlanet.triangles[i];
            if (IsCursorOverTriangle(triangle, hit.normal, hit.point, i))
            {
                currentTriangle = triangle;
                currentTriangleIndex = i;
                //cursor.position = currentPlanet.transform.TransformPoint(Vector3.zero);
                return;
            }
        }

        SetNoTriangle();
    }

    private void SetNoTriangle()
    {
        currentTriangle = null;
        currentTriangleIndex = -1;
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
    
    public float CursorTriangleDot(Triangle triangle, Vector3 normal, Vector3 point, int i)
    {
        Vector3 recalculatedNormal = (currentPlanet.transform.TransformDirection(triangle.elevationNormal));
        return math.dot(normal, recalculatedNormal);
    }

    public void DrawCursor()
    {
        if (currentTriangleIndex == -1)
        {
            cursorFilter.mesh = null;
            //cursorFilter.mesh = cursorMesh;

            return;
        }

        cursorFilter.mesh = null;

        Vector3 v1, v2, v3;
        if (currentTriangle.heightLevel == 0)
        {
            v1 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.indices[0]]);
            v2 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.indices[1]]);
            v3 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.indices[2]]);
        }
        else
        {
            /*
            v1 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.indices[0]]);
            v2 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.indices[1]]);
            v3 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.indices[2]]);
            */

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

        switch (currentTriangle.building)
        {
            case Building.House:
                break;
            case Building.Hangar:
                break;
            case Building.Rocket:

                int saveIndex = currentTriangleIndex;
                float holdTimer = 0;
                while (holdTimer < holdDuration)
                {
                    holdTimer += Time.deltaTime;
                    await Task.Yield();
                    if (!isHolding)
                    {
                        Debug.Log("stop holding");
                        return;
                    }
                }

                WorldManager.instance.LaunchAircraft(currentPlanet, saveIndex);
                return;

            case Building.Bridge:
                break;
            case Building.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

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
            }
            else
            {
                selectedExplorer.ChangeTask(ExplorerTask.None);
            }
            
            selectedExplorer.TargetTile(currentTriangleIndex);
            UnSelectExplorer();
            
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