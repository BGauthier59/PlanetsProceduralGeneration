using System;
using System.Collections;
using System.Collections.Generic;
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
    private Camera cam;
    [SerializeField] private float baseDistance;
    [SerializeField] private Vector2 minMaxDistance;
    [SerializeField] private float cameraSpeed;
    [SerializeField] private float stepValue;
    private float currentDistance;
    private Vector2 scroll;

    // Explorer Selection
    public Color selectedColor;
    public Color unselectedColor;
    
    public void Initialize(Planet target)
    {
        currentPlanet = target;
        currentDistance = baseDistance;
        cam = camera.GetComponent<Camera>();
        currentTriangle = null;
        isActive = true;
    }

    public void OnRotate(InputAction.CallbackContext ctx)
    {
        if (!isActive) return;
        if (!ctx.performed) return;
        mouseMove = ctx.ReadValue<Vector2>();
    }

    public void OnDragPlanet(InputAction.CallbackContext ctx)
    {
        if (!isActive) return;
        if (ctx.started) isDraggingPlanet = true;
        else if (ctx.canceled) isDraggingPlanet = false;
    }

    public void OnScroll(InputAction.CallbackContext ctx)
    {
        if (!isActive) return;
        if (!ctx.performed) return;
        scroll = ctx.ReadValue<Vector2>();
        SetDistance(-scroll.y);
    }

    public void OnClick(InputAction.CallbackContext ctx)
    {
        if (!isActive) return;
        if (!ctx.performed) return;
        ClickOnTile();
    }

    private void Update()
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
        Vector3 targetPos = currentPlanet.transform.position - Vector3.forward * baseDistance -
                            camera.forward * currentDistance;
        camera.position = Vector3.Lerp(camera.position, targetPos, cameraSpeed * Time.deltaTime);
    }

    public void SetNewTarget(int i)
    {
        currentPlanet = WorldManager.instance.GetPlanet(i);
    }

    public int currentTriangleIndex = -1;
    public Triangle currentTriangle;
    public Transform cursor;
    public MeshFilter cursorFilter;
    private Mesh cursorMesh;
    public ExplorerBehaviour selectedExplorer;

    public void DetectCurrentTriangle()
    {
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray.origin, ray.direction, out var hit, 100)) return;

        //cursor.position = hit.point;

        Triangle triangle;
        for (int i = 0; i < currentPlanet.triangles.Count; i++)
        {
            triangle = currentPlanet.triangles[i];
            if (IsCursorOverTriangle(triangle, hit.normal, hit.point))
            {
                currentTriangle = triangle;
                currentTriangleIndex = i;
                cursor.position = currentPlanet.transform.TransformPoint(Vector3.zero);
                return;
            }
        }
        
        currentTriangle = null;
        currentTriangleIndex = -1;
    }

    public bool IsCursorOverTriangle(Triangle triangle, Vector3 normal, Vector3 point)
    {
        Vector3 recalculatedNormal = currentPlanet.transform.TransformPoint(triangle.normal);
        Vector3 recalculatedCenter = currentPlanet.transform.
            TransformPoint(triangle.centralPoint + triangle.normal * triangle.heightLevel);

        bool dotTest = math.dot(normal, recalculatedNormal) >= 0.999;
        bool distanceTest = Vector3.Distance(recalculatedCenter, point) < 1;

        if (distanceTest)
        {
            Debug.DrawLine(point, currentPlanet.transform.
                TransformPoint(triangle.centralPoint), Color.red);
        }
        
        if (dotTest) return true;
        return false;
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

    private void ClickOnTile()
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