using System;
using UnityEngine;

public class ColorPicker : MonoBehaviour
{
    public Renderer QuadRenderer;
    public SliderIndicator Slider;
    public Renderer SphereColor;

    public event Action<Color> OnColorChange;

    Texture2D texture;
    Material sphereMat;

    void Start()
    {
        texture = QuadRenderer.material.mainTexture as Texture2D;
        Slider.OnCollision += OnIndicatorChanged;
        sphereMat = SphereColor.material;
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
            sphereMat.color = color;
            OnColorChange?.Invoke(color);
        }

    }
}
