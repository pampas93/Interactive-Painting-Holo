using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class BrushWidth : MonoBehaviour
{
    [HideInInspector]
    public float LineWidth = 0.01f;

    [SerializeField]
    private PinchSlider Slider;

    [SerializeField]
    private Transform Indicator;

    // Capacity of 0.001f  to 0.03f
    private float maxWidth = 0.035f;
    private float minWidth = 0.003f;

    private float deltaWidth;
    void Start()
    {
        deltaWidth = maxWidth - minWidth;
        Slider.SliderValue = (LineWidth - minWidth) / deltaWidth;
    }

    public void OnValueUpdate(SliderEventData data)
    {
        float brushVal = (data.NewValue * deltaWidth) + minWidth;
        LineWidth = brushVal;
        // Update Indicator
        float size = brushVal / maxWidth;
        Indicator.localScale = new Vector3(size, size, size);
    }
}
