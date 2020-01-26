using System;
using UnityEngine;

public class HandMenu : MonoSingleton<HandMenu>
{
    [SerializeField]
    private BrushWidth brushWidth;
    [SerializeField]
    private ColorPicker colorPicker;

    GameObject colorPickerGO { get => colorPicker.gameObject; }
    GameObject brushWidthGO { get => brushWidth.gameObject; }

    public void OnActivated(bool active)
    {
        if (!DrawManager.Instance) return;
        DrawManager.Instance.EnablePainting = !active;
    }

    public void SetupPaintEvents(Action<Color> colorChange, Action<float> widthChange)
    {
        colorPicker.OnColorChange += colorChange;
        brushWidth.OnWidthChange += widthChange;
    }

    public void OnColorPicker()
    {
        if (!colorPickerGO.activeInHierarchy)
            HideAllMenu();

        colorPickerGO.SetActive(!colorPickerGO.activeInHierarchy);
    }

    public void OnUndoClick()
    {
        DrawManager.Instance.UndoLine();
    }

    public void OnBrushWidthClick()
    {
        if (!brushWidthGO.activeInHierarchy)
            HideAllMenu();

        brushWidthGO.SetActive(!brushWidthGO.activeInHierarchy);
    }

    private void HideAllMenu()
    {
        brushWidthGO.SetActive(false);
        colorPickerGO.SetActive(false);
    }
}
