using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    private class ControlBindingUI
    {
        public KeyBindingAction action;
        public string rowName;
        public Button button;
        public TMP_Text tmpText;
        public Text legacyText;
    }

    [Header("Panels")]
    public GameObject settingsMainPanel;
    public GameObject audioPanel;
    public GameObject graphicsPanel;
    public GameObject controlPanel;

    [Header("Main Panel Buttons")]
    public Button audioButton;
    public Button graphicsButton;
    public Button controlButton;
    public Button mainCloseButton;

    [Header("Audio")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;
    public Button audioBackButton;

    [Header("Graphics")]
    public Slider brightnessSlider;
    public Button leftResolutionButton;
    public Button rightResolutionButton;
    public TMP_Text resolutionValueText;
    public Button screenModeButton;
    public TMP_Text screenModeText;
    public Button graphicsBackButton;

    [Header("Control")]
    public Button controlBackButton;

    private bool listenersReady;
    private ControlBindingUI[] controlBindings;
    private bool controlBindingsReady;
    private bool waitingForKeyInput;
    private KeyBindingAction waitingAction;
    private int keyInputWaitStartFrame;

    public static bool IsWaitingForKeyInput { get; private set; }
    public static bool ShouldBlockEscapeNavigation
    {
        get { return IsWaitingForKeyInput || keyInputCancelFrame == Time.frameCount; }
    }

    private static int keyInputCancelFrame = -1;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void Start()
    {
        RegisterWithManager();
        SetupListeners();
    }

    private void OnEnable()
    {
        RegisterWithManager();
        SetupListeners();
        ShowMainPanel();
        InitFromSettings();
    }

    private void OnDisable()
    {
        CancelKeyInputWait();
    }

    private void Update()
    {
        UpdateKeyInputWait();
    }

    private void RegisterWithManager()
    {
        if (GetComponentInParent<InGameSettingsMenuController>() != null)
            return;

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.settingsUIInstance = gameObject;
    }

    private void InitFromSettings()
    {
        if (SettingsManager.Instance == null || SettingsManager.Instance.Settings == null)
            return;

        SettingsData settings = SettingsManager.Instance.Settings;

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume / 100f);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.SetValueWithoutNotify(settings.bgmVolume / 100f);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume / 100f);

        if (brightnessSlider != null)
        {
            float minNormalized = SettingsManager.Instance != null ? SettingsManager.Instance.MinBrightness / 100f : 0f;
            brightnessSlider.SetValueWithoutNotify(settings.brightness / 100f);
            brightnessSlider.minValue = minNormalized;
        }

        UpdateResolutionText();
        UpdateScreenModeText();
        UpdateControlBindingTexts();
    }

    private void SetupListeners()
    {
        if (listenersReady)
            return;

        listenersReady = true;

        if (audioButton != null)
            audioButton.onClick.AddListener(() => ShowSubPanel(audioPanel));

        if (graphicsButton != null)
            graphicsButton.onClick.AddListener(() => ShowSubPanel(graphicsPanel));

        if (controlButton != null)
            controlButton.onClick.AddListener(() => ShowSubPanel(controlPanel));

        if (mainCloseButton != null)
            mainCloseButton.onClick.AddListener(() => gameObject.SetActive(false));

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(value => SetMasterVolume(value));

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(value => SetBgmVolume(value));

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(value => SetSfxVolume(value));

        if (audioBackButton != null)
            audioBackButton.onClick.AddListener(ShowMainPanel);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(value => SetBrightness(value));

        bool hasResolutionSelector = HasResolutionSelector();

        if (!hasResolutionSelector && leftResolutionButton != null)
            leftResolutionButton.onClick.AddListener(() => ChangeResolution(-1));

        if (!hasResolutionSelector && rightResolutionButton != null)
            rightResolutionButton.onClick.AddListener(() => ChangeResolution(1));

        bool hasScreenModeSelector = HasScreenModeSelector();

        if (!hasScreenModeSelector && screenModeButton != null)
            screenModeButton.onClick.AddListener(ToggleScreenMode);

        if (graphicsBackButton != null)
            graphicsBackButton.onClick.AddListener(ShowMainPanel);

        if (controlBackButton != null)
            controlBackButton.onClick.AddListener(ShowMainPanel);

        SetupControlBindings();
    }

    private void SetMasterVolume(float value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetMasterVolume(Mathf.RoundToInt(value * 100f));
    }

    private void SetBgmVolume(float value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetBgmVolume(Mathf.RoundToInt(value * 100f));
    }

    private void SetSfxVolume(float value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetSfxVolume(Mathf.RoundToInt(value * 100f));
    }

    private void SetBrightness(float value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetBrightness(Mathf.RoundToInt(value * 100f));
    }

    private bool HasResolutionSelector()
    {
        ResolutionArrowSelectorUI[] selectors = GetComponentsInChildren<ResolutionArrowSelectorUI>(true);
        for (int i = 0; i < selectors.Length; i++)
        {
            ResolutionArrowSelectorUI selector = selectors[i];
            if (selector.leftButton == leftResolutionButton || selector.rightButton == rightResolutionButton)
                return true;
        }

        return false;
    }

    private bool HasScreenModeSelector()
    {
        ScreenModeBoxSelectorUI[] selectors = GetComponentsInChildren<ScreenModeBoxSelectorUI>(true);
        for (int i = 0; i < selectors.Length; i++)
        {
            ScreenModeBoxSelectorUI selector = selectors[i];
            if (selector.boxButton == screenModeButton)
                return true;
        }

        return false;
    }

    private void ShowMainPanel()
    {
        CancelKeyInputWait();

        if (settingsMainPanel != null)
            settingsMainPanel.SetActive(true);

        if (audioPanel != null)
            audioPanel.SetActive(false);

        if (graphicsPanel != null)
            graphicsPanel.SetActive(false);

        if (controlPanel != null)
            controlPanel.SetActive(false);
    }

    private void ShowSubPanel(GameObject panel)
    {
        CancelKeyInputWait();

        if (settingsMainPanel != null)
            settingsMainPanel.SetActive(false);

        if (audioPanel != null)
            audioPanel.SetActive(false);

        if (graphicsPanel != null)
            graphicsPanel.SetActive(false);

        if (controlPanel != null)
            controlPanel.SetActive(false);

        if (panel != null)
            panel.SetActive(true);

        if (panel == controlPanel)
            UpdateControlBindingTexts();
    }

    private void ChangeResolution(int direction)
    {
        if (SettingsManager.Instance == null || SettingsManager.Instance.Settings == null)
            return;

        SettingsData settings = SettingsManager.Instance.Settings;
        int newIndex = Mathf.Clamp(
            settings.resolutionIndex + direction,
            0,
            SettingsManager.Instance.ResolutionCount - 1
        );

        SettingsManager.Instance.SetResolutionIndex(newIndex);
        UpdateResolutionText();
    }

    private void ToggleScreenMode()
    {
        if (SettingsManager.Instance == null)
            return;

        SettingsManager.Instance.CycleScreenMode();
        UpdateScreenModeText();
    }

    private void UpdateResolutionText()
    {
        if (resolutionValueText == null || SettingsManager.Instance == null)
            return;

        resolutionValueText.text = SettingsManager.Instance.GetCurrentResolutionString();
    }

    private void UpdateScreenModeText()
    {
        if (screenModeText == null || SettingsManager.Instance == null)
            return;

        screenModeText.text = SettingsManager.Instance.GetCurrentScreenModeString();
    }

    private void SetupControlBindings()
    {
        if (controlBindingsReady)
            return;

        controlBindingsReady = true;

        controlBindings = new ControlBindingUI[]
        {
            CreateControlBinding(KeyBindingAction.MoveLeft, "MoveLeftRow"),
            CreateControlBinding(KeyBindingAction.MoveRight, "MoveRightRow"),
            CreateControlBinding(KeyBindingAction.Jump, "JumpRow"),
            CreateControlBinding(KeyBindingAction.Aim, "AimRow"),
            CreateControlBinding(KeyBindingAction.Shoot, "ShootRow")
        };

        for (int i = 0; i < controlBindings.Length; i++)
        {
            ControlBindingUI binding = controlBindings[i];
            if (binding == null || binding.button == null)
                continue;

            KeyBindingAction action = binding.action;
            binding.button.onClick.AddListener(() => StartKeyInputWait(action));
        }

        UpdateControlBindingTexts();
    }

    private ControlBindingUI CreateControlBinding(KeyBindingAction action, string rowName)
    {
        ControlBindingUI binding = new ControlBindingUI
        {
            action = action,
            rowName = rowName
        };

        Transform row = FindChildByName(controlPanel != null ? controlPanel.transform : transform, rowName);
        if (row == null)
        {
            Debug.LogWarning("[SettingsUI] Control row not found: " + rowName);
            return binding;
        }

        Transform buttonTransform = FindChildByName(row, "KeyBoxButton");
        if (buttonTransform == null)
        {
            Debug.LogWarning("[SettingsUI] KeyBoxButton not found in row: " + rowName);
            return binding;
        }

        binding.button = buttonTransform.GetComponent<Button>();
        if (binding.button == null)
            binding.button = buttonTransform.GetComponentInChildren<Button>(true);

        if (binding.button == null)
            Debug.LogWarning("[SettingsUI] Button component not found in row: " + rowName);

        binding.tmpText = buttonTransform.GetComponentInChildren<TMP_Text>(true);
        binding.legacyText = buttonTransform.GetComponentInChildren<Text>(true);

        if (binding.tmpText == null && binding.legacyText == null)
            Debug.LogWarning("[SettingsUI] Key text component not found in row: " + rowName);

        return binding;
    }

    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
                return children[i];
        }

        return null;
    }

    private void StartKeyInputWait(KeyBindingAction action)
    {
        waitingForKeyInput = true;
        IsWaitingForKeyInput = true;
        waitingAction = action;
        keyInputWaitStartFrame = Time.frameCount;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        SetControlBindingText(action, "...");
    }

    private void UpdateKeyInputWait()
    {
        if (!waitingForKeyInput)
            return;

        if (Time.frameCount <= keyInputWaitStartFrame)
            return;

        KeyCode pressedKey;
        if (!KeyBindingSettings.TryGetPressedKey(out pressedKey))
            return;

        if (pressedKey == KeyCode.Escape)
        {
            CancelKeyInputWait();
            return;
        }

        bool saved = KeyBindingSettings.SetKey(waitingAction, pressedKey);
        waitingForKeyInput = false;
        IsWaitingForKeyInput = false;

        if (!saved)
        {
            Debug.LogWarning("[SettingsUI] Key binding was not changed: " + KeyBindingSettings.GetDisplayName(pressedKey));
        }

        UpdateControlBindingTexts();
    }

    private void CancelKeyInputWait()
    {
        if (!waitingForKeyInput)
            return;

        waitingForKeyInput = false;
        IsWaitingForKeyInput = false;
        keyInputCancelFrame = Time.frameCount;
        UpdateControlBindingTexts();
    }

    private void UpdateControlBindingTexts()
    {
        KeyBindingSettings.EnsureLoaded();

        if (controlBindings == null)
            return;

        for (int i = 0; i < controlBindings.Length; i++)
        {
            ControlBindingUI binding = controlBindings[i];
            if (binding == null)
                continue;

            SetControlBindingText(binding, KeyBindingSettings.GetDisplayName(binding.action));
        }
    }

    private void SetControlBindingText(KeyBindingAction action, string text)
    {
        ControlBindingUI binding = FindControlBinding(action);
        if (binding != null)
            SetControlBindingText(binding, text);
    }

    private void SetControlBindingText(ControlBindingUI binding, string text)
    {
        if (binding.tmpText != null)
            binding.tmpText.text = text;

        if (binding.legacyText != null)
            binding.legacyText.text = text;
    }

    private ControlBindingUI FindControlBinding(KeyBindingAction action)
    {
        if (controlBindings == null)
            return null;

        for (int i = 0; i < controlBindings.Length; i++)
        {
            if (controlBindings[i] != null && controlBindings[i].action == action)
                return controlBindings[i];
        }

        return null;
    }
}
