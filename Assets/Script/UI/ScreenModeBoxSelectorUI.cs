using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScreenModeBoxSelectorUI : MonoBehaviour
{
    [System.Serializable]
    public class ScreenModeOption
    {
        public string label;
        public FullScreenMode mode;
    }

    [Header("UI")]
    public TextMeshProUGUI valueText;
    public Button boxButton;

    [Header("Options")]
    public ScreenModeOption[] options =
    {
        new ScreenModeOption
        {
            label = "전체화면",
            mode = FullScreenMode.ExclusiveFullScreen
        },
        new ScreenModeOption
        {
            label = "창모드",
            mode = FullScreenMode.Windowed
        },
        new ScreenModeOption
        {
            label = "전체 창모드",
            mode = FullScreenMode.FullScreenWindow
        }
    };

    [Header("Start")]
    public int startIndex = 0;

    [Header("Apply")]
    public bool applyImmediately = true;

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

        if (boxButton != null)
            boxButton.onClick.AddListener(SelectNext);
    }

    private void SyncFromSettings()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.Settings != null)
            currentIndex = SettingsManager.Instance.Settings.screenModeIndex;
        else
            currentIndex = startIndex;

        currentIndex = Mathf.Clamp(currentIndex, 0, GetOptionCount() - 1);
    }

    private void SelectNext()
    {
        currentIndex++;

        if (currentIndex >= GetOptionCount())
            currentIndex = 0;

        if (applyImmediately && SettingsManager.Instance != null)
            SettingsManager.Instance.SetScreenModeIndex(currentIndex);

        UpdateUI();
    }

    private void UpdateUI()
    {
        currentIndex = Mathf.Clamp(currentIndex, 0, GetOptionCount() - 1);

        if (valueText != null)
            valueText.text = GetCurrentLabel();
    }

    public FullScreenMode GetCurrentMode()
    {
        if (options != null && options.Length > 0)
            return options[currentIndex].mode;

        switch (currentIndex)
        {
            case 1:
                return FullScreenMode.Windowed;
            case 2:
                return FullScreenMode.FullScreenWindow;
            default:
                return FullScreenMode.ExclusiveFullScreen;
        }
    }

    public string GetCurrentLabel()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.Settings != null)
            return SettingsManager.Instance.GetScreenModeString(currentIndex);

        if (options != null && options.Length > 0)
            return options[currentIndex].label;

        return "전체화면";
    }

    private int GetOptionCount()
    {
        if (options != null && options.Length > 0)
            return options.Length;

        return SettingsManager.ScreenModeCount;
    }
}
