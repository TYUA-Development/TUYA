using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResolutionArrowSelectorUI : MonoBehaviour
{
    [System.Serializable]
    public class ResolutionOption
    {
        public int width;
        public int height;
    }

    [Header("UI")]
    public TextMeshProUGUI valueText;
    public Button leftButton;
    public Button rightButton;

    [Header("Arrow Visual")]
    public CanvasGroup leftArrowGroup;
    public CanvasGroup rightArrowGroup;

    [Range(0f, 1f)] public float enabledArrowAlpha = 1f;
    [Range(0f, 1f)] public float disabledArrowAlpha = 0.15f;

    [Header("Resolution Options")]
    public ResolutionOption[] options =
    {
        new ResolutionOption { width = 3840, height = 2160 },
        new ResolutionOption { width = 2560, height = 1440 },
        new ResolutionOption { width = 1920, height = 1080 },
        new ResolutionOption { width = 1600, height = 900 },
        new ResolutionOption { width = 1280, height = 720 }
    };

    [Header("Start")]
    public int startIndex = 2;

    private int currentIndex;
    private bool listenersReady;

    private void Awake()
    {
        SetupListeners();
    }

    private void OnEnable()
    {
        SyncFromSettings();
        UpdateUI();
    }

    private void SetupListeners()
    {
        if (listenersReady)
            return;

        listenersReady = true;

        if (leftButton != null)
            leftButton.onClick.AddListener(SelectPrevious);

        if (rightButton != null)
            rightButton.onClick.AddListener(SelectNext);
    }

    private void SyncFromSettings()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.Settings != null)
            currentIndex = SettingsManager.Instance.Settings.resolutionIndex;
        else
            currentIndex = startIndex;

        currentIndex = Mathf.Clamp(currentIndex, 0, GetOptionCount() - 1);
    }

    private void SelectPrevious()
    {
        ChangeResolution(-1);
    }

    private void SelectNext()
    {
        ChangeResolution(1);
    }

    private void ChangeResolution(int direction)
    {
        currentIndex = Mathf.Clamp(currentIndex + direction, 0, GetOptionCount() - 1);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetResolutionIndex(currentIndex);

        UpdateUI();
    }

    private void UpdateUI()
    {
        currentIndex = Mathf.Clamp(currentIndex, 0, GetOptionCount() - 1);

        if (valueText != null)
            valueText.text = GetCurrentWidth() + " x " + GetCurrentHeight();

        bool canGoLeft = currentIndex > 0;
        bool canGoRight = currentIndex < GetOptionCount() - 1;

        if (leftButton != null)
            leftButton.interactable = canGoLeft;

        if (rightButton != null)
            rightButton.interactable = canGoRight;

        if (leftArrowGroup != null)
            leftArrowGroup.alpha = canGoLeft ? enabledArrowAlpha : disabledArrowAlpha;

        if (rightArrowGroup != null)
            rightArrowGroup.alpha = canGoRight ? enabledArrowAlpha : disabledArrowAlpha;
    }

    public int GetCurrentWidth()
    {
        if (options != null && options.Length > 0)
            return options[currentIndex].width;

        return SettingsManager.FixedResolutions[currentIndex].width;
    }

    public int GetCurrentHeight()
    {
        if (options != null && options.Length > 0)
            return options[currentIndex].height;

        return SettingsManager.FixedResolutions[currentIndex].height;
    }

    private int GetOptionCount()
    {
        if (options != null && options.Length > 0)
            return options.Length;

        return SettingsManager.FixedResolutions.Length;
    }
}
