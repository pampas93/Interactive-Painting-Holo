using System;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class BrushWidth : MonoBehaviour
{
    public event Action<float> OnWidthChange;

    [SerializeField]
    private PinchSlider Slider;
    [SerializeField]
    private Transform Indicator;

    private float lineWidth = 0.01f;

    // Capacity of 0.001f  to 0.03f
    private float maxWidth = 0.035f;
    private float minWidth = 0.003f;

    private float deltaWidth;
    void Start()
    {
        deltaWidth = maxWidth - minWidth;
        Slider.SliderValue = (lineWidth - minWidth) / deltaWidth;
        OnWidthChange?.Invoke(lineWidth);
    }

    public void OnValueUpdate(SliderEventData data)
    {
        float brushVal = (data.NewValue * deltaWidth) + minWidth;
        lineWidth = brushVal;
        OnWidthChange?.Invoke(lineWidth);

        // Update Indicator
        float size = brushVal / maxWidth;
        Indicator.localScale = new Vector3(size, size, size);
    }
}
