using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    public Renderer QuadRenderer;
    public SliderIndicator Slider;
    public Image PickedColor;

    Texture2D texture;

    void Start()
    {
        texture = QuadRenderer.material.mainTexture as Texture2D;
        Slider.OnCollision += OnIndicatorChanged;
    }

    void OnIndicatorChanged(Vector3 contactPoint)
    {
        if (Physics.Raycast(contactPoint, Vector3.forward, out RaycastHit oHit, 100f))
        {
            var rend = oHit.collider.GetComponent<Renderer>();
            if (rend == null)
                return;
            var pixelUV = oHit.lightmapCoord;
            var color = texture.GetPixelBilinear(pixelUV.x, pixelUV.y);
            PickedColor.color = color;
        }

    }
}
