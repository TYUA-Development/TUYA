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
    [Tooltip("처음 선택될 해상도 번호. 0=3840x2160, 1=2560x1440, 2=1920x1080, 3=1600x900, 4=1280x720")]
    public int startIndex = 2;

    private int currentIndex = 0;

    private void Awake()
    {
        if (leftButton != null)
            leftButton.onClick.AddListener(SelectPrevious);

        if (rightButton != null)
            rightButton.onClick.AddListener(SelectNext);
    }

    private void OnEnable()
    {
        if (options == null || options.Length == 0)
            return;

        currentIndex = Mathf.Clamp(startIndex, 0, options.Length - 1);
        UpdateUI();
    }

    private void SelectPrevious()
    {
        if (options == null || options.Length == 0)
            return;

        if (currentIndex <= 0)
            return;

        currentIndex--;
        UpdateUI();
    }

    private void SelectNext()
    {
        if (options == null || options.Length == 0)
            return;

        if (currentIndex >= options.Length - 1)
            return;

        currentIndex++;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (options == null || options.Length == 0)
            return;

        currentIndex = Mathf.Clamp(currentIndex, 0, options.Length - 1);

        ResolutionOption option = options[currentIndex];

        if (valueText != null)
        {
            valueText.text = option.width + " x " + option.height;
        }

        bool canGoLeft = currentIndex > 0;
        bool canGoRight = currentIndex < options.Length - 1;

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
        if (options == null || options.Length == 0)
            return 1920;

        return options[currentIndex].width;
    }

    public int GetCurrentHeight()
    {
        if (options == null || options.Length == 0)
            return 1080;

        return options[currentIndex].height;
    }
}