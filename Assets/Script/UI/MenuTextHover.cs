using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class MenuTextHover : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private static List<MenuTextHover> allMenus = new List<MenuTextHover>();

    [Header("Text")]
    public TextMeshProUGUI targetText;

    [Header("Default Selected")]
    public bool defaultSelected = false;

    [Header("Alpha")]
    [Range(0f, 1f)] public float normalAlpha = 0.45f;
    [Range(0f, 1f)] public float selectedAlpha = 1f;

    [Header("Click Event")]
    public UnityEvent onClick;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TextMeshProUGUI>();

        if (!allMenus.Contains(this))
            allMenus.Add(this);
    }

    private void Start()
    {
        if (defaultSelected)
        {
            SelectThis();
        }
        else
        {
            SetAlpha(normalAlpha);
        }
    }

    private void OnDestroy()
    {
        if (allMenus.Contains(this))
            allMenus.Remove(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SelectThis();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (onClick != null)
            onClick.Invoke();
    }

    private void SelectThis()
    {
        foreach (MenuTextHover menu in allMenus)
        {
            if (menu != null)
                menu.SetAlpha(menu.normalAlpha);
        }

        SetAlpha(selectedAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (targetText == null) return;

        Color color = targetText.color;
        color.a = alpha;
        targetText.color = color;
    }
}