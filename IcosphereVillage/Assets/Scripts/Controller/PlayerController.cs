using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoSingleton<PlayerController>
{
    [SerializeField] private Planet currentPlanet;

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

    public void Initialize(Planet target)
    {
        currentPlanet = target;
        currentDistance = baseDistance;
        cam = camera.GetComponent<Camera>();
    }

    public void OnRotate(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        mouseMove = ctx.ReadValue<Vector2>();
    }

    public void OnDragPlanet(InputAction.CallbackContext ctx)
    {
        if (ctx.started) isDraggingPlanet = true;
        else if (ctx.canceled) isDraggingPlanet = false;
    }

    public void OnScroll(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        scroll = ctx.ReadValue<Vector2>();
        SetDistance(-scroll.y);
    }

    private void Update()
    {
        if (isDraggingPlanet) RotateAroundDraggedPlanet();
        DetectCurrentTriangle();
        DrawCursor();
    }

    private void LateUpdate()
    {
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

    public Triangle currentTriangle;
    public Transform cursor;
    public MeshFilter cursorFilter;

    public void DetectCurrentTriangle()
    {
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray.origin, ray.direction, out var hit, 100)) return;

        //cursor.position = hit.point;

        foreach (var triangle in currentPlanet.triangles)
        {
            if (IsCursorOverTriangle(triangle, hit.normal))
            {
                currentTriangle = triangle;
                cursor.position = currentPlanet.transform.TransformPoint(Vector3.zero);
                return;
            }
        }

        currentTriangle = null;
    }

    public bool IsCursorOverTriangle(Triangle triangle, Vector3 normal)
    {
        Vector3 recalculatedNormal = currentPlanet.transform.TransformPoint(triangle.normal);

        if (math.dot(normal, recalculatedNormal) >= 0.999) return true;
        return false;
    }

    private Mesh cursorMesh;

    public void DrawCursor()
    {
        if (currentTriangle == null || currentPlanet == null) return;

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
            
            v1 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.elevationTriangle[^1].x]);
            v2 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.elevationTriangle[^1].y]);
            v3 = currentPlanet.transform.TransformPoint(currentPlanet.vertices[currentTriangle.elevationTriangle[^1].z]);
            
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
}