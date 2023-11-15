using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoSingleton<PlayerController>
{
    [SerializeField] private Transform currentPlanet;

    // Rotation
    private Vector2 mouseMove;
    private bool isDraggingPlanet;
    [SerializeField] private float rotateSensibility;

    // Zoom
    [SerializeField] private Transform camera;
    [SerializeField] private float baseDistance;
    [SerializeField] private Vector2 minMaxDistance;
    [SerializeField] private float cameraSpeed;
    [SerializeField] private float stepValue;
    private float currentDistance;
    private Vector2 scroll;

    private void Start()
    {
        currentDistance = baseDistance;
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
        //RotateDraggedPlanet();
    }

    private void LateUpdate()
    {
        Zoom();
    }

    private void RotateAroundDraggedPlanet()
    {
        currentPlanet.RotateAround(Vector3.up, math.radians(-mouseMove.x * rotateSensibility));
        currentPlanet.RotateAround(Vector3.right, math.radians(mouseMove.y * rotateSensibility));
    }

    private void SetDistance(float delta)
    {
        currentDistance += delta * stepValue;
        currentDistance = math.clamp(currentDistance, minMaxDistance.x, minMaxDistance.y);
    }

    private void Zoom()
    {
        Vector3 targetPos = currentPlanet.position - Vector3.forward * baseDistance - camera.forward * currentDistance;
        camera.position = Vector3.Lerp(camera.position, targetPos, cameraSpeed * Time.deltaTime);
    }

    public void SetNewTarget(int i)
    {
        currentPlanet = WorldManager.instance.GetPlanet(i);
    }
}