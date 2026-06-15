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
            label = "테두리 없는 전체화면",
            mode = FullScreenMode.FullScreenWindow
        },
        new ScreenModeOption
        {
            label = "창모드",
            mode = FullScreenMode.Windowed
        }
    };

    [Header("Start")]
    [Tooltip("0=전체화면, 1=테두리 없는 전체화면, 2=창모드")]
    public int startIndex = 0;

    [Header("Apply")]
    [Tooltip("체크하면 박스를 누를 때 실제 화면 모드도 바로 바뀜")]
    public bool applyImmediately = true;

    private int currentIndex = 0;

    private void Awake()
    {
        if (boxButton != null)
        {
            boxButton.onClick.AddListener(SelectNext);
        }
    }

    private void OnEnable()
    {
        if (options == null || options.Length == 0)
            return;

        currentIndex = Mathf.Clamp(startIndex, 0, options.Length - 1);
        UpdateUI();

        if (applyImmediately)
        {
            ApplyScreenMode();
        }
    }

    private void SelectNext()
    {
        if (options == null || options.Length == 0)
            return;

        currentIndex++;

        if (currentIndex >= options.Length)
        {
            currentIndex = 0;
        }

        UpdateUI();

        if (applyImmediately)
        {
            ApplyScreenMode();
        }
    }

    private void UpdateUI()
    {
        if (options == null || options.Length == 0)
            return;

        ScreenModeOption option = options[currentIndex];

        if (valueText != null)
        {
            valueText.text = option.label;
        }
    }

    private void ApplyScreenMode()
    {
        if (options == null || options.Length == 0)
            return;

        ScreenModeOption option = options[currentIndex];

        Screen.fullScreenMode = option.mode;

        if (option.mode == FullScreenMode.Windowed)
        {
            Screen.fullScreen = false;
        }
        else
        {
            Screen.fullScreen = true;
        }
    }

    public FullScreenMode GetCurrentMode()
    {
        if (options == null || options.Length == 0)
            return FullScreenMode.FullScreenWindow;

        return options[currentIndex].mode;
    }

    public string GetCurrentLabel()
    {
        if (options == null || options.Length == 0)
            return "전체화면";

        return options[currentIndex].label;
    }
}