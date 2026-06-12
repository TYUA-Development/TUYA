using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class SettingsMenuButtonAlpha : MonoBehaviour, IPointerEnterHandler
{
    [Header("Text Target")]
    public TextMeshProUGUI targetText;

    [Header("Group Target")]
    public CanvasGroup targetGroup;

    [Header("Alpha Percent")]
    [Range(0, 100)] public int normalAlpha = 10;
    [Range(0, 100)] public int selectedAlpha = 100;

    private SettingsMenuButtonAlpha[] itemsInSamePanel;

    void Awake()
    {
        if (targetText == null)
            targetText = GetComponentInChildren<TextMeshProUGUI>();

        if (targetGroup == null)
            targetGroup = GetComponent<CanvasGroup>();

        itemsInSamePanel = transform.parent.GetComponentsInChildren<SettingsMenuButtonAlpha>(true);
    }

    void OnEnable()
    {
        if (transform.GetSiblingIndex() == GetFirstItemIndex())
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
        foreach (var item in itemsInSamePanel)
        {
            if (item == null)
                continue;

            item.SetAlpha(item == this ? selectedAlpha : normalAlpha);
        }
    }

    void SetAlpha(int alphaPercent)
    {
        float alpha = alphaPercent / 100f;

        if (targetGroup != null)
        {
            targetGroup.alpha = alpha;
        }

        if (targetText != null)
        {
            Color color = targetText.color;
            color.a = alpha;
            targetText.color = color;
        }
    }

    int GetFirstItemIndex()
    {
        int firstIndex = 9999;

        foreach (var item in itemsInSamePanel)
        {
            if (item == null)
                continue;

            int index = item.transform.GetSiblingIndex();

            if (index < firstIndex)
                firstIndex = index;
        }

        return firstIndex;
    }
}