using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class SettingsMenuButtonAlpha : MonoBehaviour, IPointerEnterHandler
{
    [Header("Text Target")]
    public TextMeshProUGUI targetText;

    [Header("Alpha Percent")]
    [Range(0, 100)] public int normalAlpha = 10;
    [Range(0, 100)] public int selectedAlpha = 100;

    private SettingsMenuButtonAlpha[] buttonsInSamePanel;

    void Awake()
    {
        if (targetText == null)
            targetText = GetComponentInChildren<TextMeshProUGUI>();

        buttonsInSamePanel = transform.parent.GetComponentsInChildren<SettingsMenuButtonAlpha>(true);
    }

    void OnEnable()
    {
        if (transform.GetSiblingIndex() == GetFirstButtonIndex())
        {
            SelectThis();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SelectThis();
    }

    public void SelectThis()
    {
        foreach (var button in buttonsInSamePanel)
        {
            if (button == null)
                continue;

            button.SetAlpha(button == this ? selectedAlpha : normalAlpha);
        }
    }

    void SetAlpha(int alphaPercent)
    {
        if (targetText == null)
            return;

        Color color = targetText.color;
        color.a = alphaPercent / 100f;
        targetText.color = color;
    }

    int GetFirstButtonIndex()
    {
        int firstIndex = 9999;

        foreach (var button in buttonsInSamePanel)
        {
            if (button != null)
            {
                int index = button.transform.GetSiblingIndex();
                if (index < firstIndex)
                    firstIndex = index;
            }
        }

        return firstIndex;
    }
}