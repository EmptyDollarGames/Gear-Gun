using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
    }
    public GameObject crosshairContainer;
    public void ChangeCrosshair(Sprite icon, float width, float height)
    {

        crosshairContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        crosshairContainer.GetComponent<Image>().sprite = icon;
    }
}
