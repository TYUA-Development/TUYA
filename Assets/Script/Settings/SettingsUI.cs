using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
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

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.settingsUIInstance = gameObject;
        SetupListeners();
    }

    private void OnEnable()
    {
        ShowMainPanel();
        InitFromSettings();
    }

    private void InitFromSettings()
    {
        SettingsData s = SettingsManager.Instance.Settings;

        if (masterVolumeSlider) masterVolumeSlider.SetValueWithoutNotify(s.masterVolume / 100f);
        if (bgmVolumeSlider)    bgmVolumeSlider.SetValueWithoutNotify(s.bgmVolume    / 100f);
        if (sfxVolumeSlider)    sfxVolumeSlider.SetValueWithoutNotify(s.sfxVolume    / 100f);
        if (brightnessSlider)   brightnessSlider.SetValueWithoutNotify(s.brightness  / 100f);

        UpdateResolutionText();
        UpdateScreenModeText();
    }

    private void SetupListeners()
    {
        // 메인 탭 버튼
        if (audioButton)    audioButton.onClick.AddListener(() => ShowSubPanel(audioPanel));
        if (graphicsButton) graphicsButton.onClick.AddListener(() => ShowSubPanel(graphicsPanel));
        if (controlButton)  controlButton.onClick.AddListener(() => ShowSubPanel(controlPanel));
        if (mainCloseButton) mainCloseButton.onClick.AddListener(() => gameObject.SetActive(false));

        // 오디오
        if (masterVolumeSlider) masterVolumeSlider.onValueChanged.AddListener(
            v => SettingsManager.Instance.SetMasterVolume(Mathf.RoundToInt(v * 100)));
        if (bgmVolumeSlider) bgmVolumeSlider.onValueChanged.AddListener(
            v => SettingsManager.Instance.SetBgmVolume(Mathf.RoundToInt(v * 100)));
        if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.AddListener(
            v => SettingsManager.Instance.SetSfxVolume(Mathf.RoundToInt(v * 100)));
        if (audioBackButton) audioBackButton.onClick.AddListener(ShowMainPanel);

        // 그래픽
        if (brightnessSlider) brightnessSlider.onValueChanged.AddListener(
            v => SettingsManager.Instance.SetBrightness(Mathf.RoundToInt(v * 100)));
        if (leftResolutionButton)  leftResolutionButton.onClick.AddListener(() => ChangeResolution(-1));
        if (rightResolutionButton) rightResolutionButton.onClick.AddListener(() => ChangeResolution(1));
        if (screenModeButton) screenModeButton.onClick.AddListener(ToggleScreenMode);
        if (graphicsBackButton) graphicsBackButton.onClick.AddListener(ShowMainPanel);

        // 조작
        if (controlBackButton) controlBackButton.onClick.AddListener(ShowMainPanel);
    }

    private void ShowMainPanel()
    {
        if (settingsMainPanel) settingsMainPanel.SetActive(true);
        if (audioPanel)    audioPanel.SetActive(false);
        if (graphicsPanel) graphicsPanel.SetActive(false);
        if (controlPanel)  controlPanel.SetActive(false);
    }

    private void ShowSubPanel(GameObject panel)
    {
        if (settingsMainPanel) settingsMainPanel.SetActive(false);
        if (audioPanel)    audioPanel.SetActive(false);
        if (graphicsPanel) graphicsPanel.SetActive(false);
        if (controlPanel)  controlPanel.SetActive(false);
        if (panel) panel.SetActive(true);
    }

    private void ChangeResolution(int dir)
    {
        SettingsData s = SettingsManager.Instance.Settings;
        int newIdx = Mathf.Clamp(s.resolutionIndex + dir, 0, SettingsManager.Instance.ResolutionCount - 1);
        SettingsManager.Instance.SetResolutionIndex(newIdx);
        UpdateResolutionText();
    }

    private void ToggleScreenMode()
    {
        SettingsManager.Instance.SetFullscreen(!SettingsManager.Instance.Settings.fullscreen);
        UpdateScreenModeText();
    }

    private void UpdateResolutionText()
    {
        if (resolutionValueText)
            resolutionValueText.text = SettingsManager.Instance.GetResolutionString(
                SettingsManager.Instance.Settings.resolutionIndex);
    }

    private void UpdateScreenModeText()
    {
        if (screenModeText)
            screenModeText.text = SettingsManager.Instance.Settings.fullscreen ? "전체화면" : "창 모드";
    }
}
