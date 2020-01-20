using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

public class DrawManager : MonoSingleton<DrawManager>, IMixedRealityPointerHandler
{
    [SerializeField, Range(0, 0.03f)]
    private float lineDefaultWidth = 0.005f;
    
    [SerializeField]
    private Color defaultColor = Color.white;

    private Vector3 prevPointDistance = Vector3.zero;
    private List<LineRenderer> lines = new List<LineRenderer>();
    private LineRenderer currentLineRender;
    private int positionCount = 0; // 2 by default
    private Vector3 grabPosition = new Vector3();
    private float minDistanceBeforeNewPoint = 0.001f;

    void OnEnable()
    {
        PointerUtils.SetGazePointerBehavior(PointerBehavior.AlwaysOff);
        // PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOff);
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityPointerHandler>(this);
    }

    void OnDisable()
    {
        PointerUtils.SetGazePointerBehavior(PointerBehavior.Default);
        // PointerUtils.SetHandRayPointerBehavior(PointerBehavior.Default);
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(this);
    }

    void UpdateLine()
    {
        if(prevPointDistance == null)
        {
            prevPointDistance = grabPosition;
        }

        if(prevPointDistance != null && Mathf.Abs(Vector3.Distance(prevPointDistance, grabPosition)) >= minDistanceBeforeNewPoint)
        {
            prevPointDistance = grabPosition;
            AddPoint(prevPointDistance);
        }
    }

    void AddNewLineRenderer()
    {
        positionCount = 0;
        GameObject go = new GameObject($"LineRenderer_{lines.Count}");
        go.transform.parent = RoomManager.Instance.transform;
        // go.transform.position = objectToTrackMovement.transform.position;
        LineRenderer goLineRenderer = go.AddComponent<LineRenderer>();
        goLineRenderer.startWidth = lineDefaultWidth;
        goLineRenderer.endWidth = lineDefaultWidth;
        goLineRenderer.useWorldSpace = true;
        goLineRenderer.material = MaterialUtils.CreateMaterial(defaultColor, $"Material_{lines.Count}");
        goLineRenderer.positionCount = 1;
        goLineRenderer.numCapVertices = 90;
        goLineRenderer.SetPosition(0, grabPosition);

        currentLineRender = goLineRenderer;
        lines.Add(goLineRenderer);
    }

    void AddPoint(Vector3 position)
    {
        currentLineRender.SetPosition(positionCount, position);
        positionCount++;
        currentLineRender.positionCount = positionCount + 1;
        currentLineRender.SetPosition(positionCount, position);
    }

#region IMixedRealityPointerHandler
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        grabPosition = eventData.Pointer.Position;
        AddNewLineRenderer();
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        grabPosition = eventData.Pointer.Position;
        UpdateLine();
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        // grabPosition = eventData.Pointer.Position;
        // AddNewLineRenderer();
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData) {}
#endregion
}
