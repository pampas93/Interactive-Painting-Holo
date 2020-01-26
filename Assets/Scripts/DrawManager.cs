using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

public class DrawManager : MonoSingleton<DrawManager>, IMixedRealityPointerHandler, IMixedRealityHandJointHandler
{
    public bool EnablePainting = true;

    [SerializeField]
    private BrushWidth brushWidth;

    [SerializeField]
    private Material defaultMaterial;
    
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
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityHandJointHandler>(this);
    }

    void OnDisable()
    {
        // PointerUtils.SetGazePointerBehavior(PointerBehavior.Default);
        // PointerUtils.SetHandRayPointerBehavior(PointerBehavior.Default);
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(this);
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityHandJointHandler>(this);
    }

    void UpdateLine()
    {
        if(prevPointDistance == null)
        {
            prevPointDistance = grabPosition;
        }

        if(prevPointDistance != null && Mathf.Abs(Vector3.Distance(prevPointDistance, 
            grabPosition)) >= minDistanceBeforeNewPoint)
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
        goLineRenderer.startWidth = brushWidth.LineWidth;
        goLineRenderer.endWidth = brushWidth.LineWidth;
        goLineRenderer.useWorldSpace = true;
        goLineRenderer.material = CreateMaterialInstance(defaultColor, 
            $"Material_{lines.Count}", defaultMaterial);
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

    public void UndoLine()
    {
        if (lines.Count > 0)
        {
            var toDelete = lines[lines.Count-1];
            Destroy(toDelete.gameObject);
            lines.RemoveAt(lines.Count-1);
        }
    }

    private Material CreateMaterialInstance(Color color, string name, Material material)
    {
        Material newMat = new Material(material);
        newMat.name = name;
        newMat.color = color;
        return newMat;
    }


#region IMixedRealityPointerHandler
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (!EnablePainting) return;
        // grabPosition = eventData.Pointer.Position;
        AddNewLineRenderer();
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        if (!EnablePainting) return;
        // grabPosition = eventData.Pointer.Position;
        UpdateLine();
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        // grabPosition = eventData.Pointer.Position;
        // AddNewLineRenderer();
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData) {}
#endregion

#region IMixedRealityHandJointHandler
    public void OnHandJointsUpdated(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
    {
        if (eventData.Handedness == Handedness.Right)
        {
            MixedRealityPose indexJointPose = eventData.InputData[TrackedHandJoint.IndexTip];
            if (indexJointPose != null)
            {
                grabPosition = indexJointPose.Position;
            }
        }
    }
#endregion
}
