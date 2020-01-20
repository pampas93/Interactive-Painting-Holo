using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject brushWidth;

    public void OnActivated(bool active)
    {
        if (!DrawManager.Instance) return;
        DrawManager.Instance.EnablePainting = !active;
    }

    public void OnUndoClick()
    {
        DrawManager.Instance.UndoLine();
    }

    public void OnBrushWidthClick()
    {
        brushWidth.SetActive(!brushWidth.activeInHierarchy);
    }
}
