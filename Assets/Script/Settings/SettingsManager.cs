using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public struct ResolutionOption
    {
        public readonly int width;
        public readonly int height;

        public ResolutionOption(int width, int height)
        {
            this.width = width;
            this.height = height;
        }
    }

    public static readonly ResolutionOption[] FixedResolutions =
    {
        new ResolutionOption(3840, 2160),
        new ResolutionOption(2560, 1440),
        new ResolutionOption(1920, 1080),
        new ResolutionOption(1600, 900),
        new ResolutionOption(1280, 720)
    };

    private static readonly FullScreenMode[] ScreenModes =
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.Windowed,
        FullScreenMode.FullScreenWindow
    };

    private static readonly string[] ScreenModeLabels =
    {
        "전체화면",
        "창모드",
        "전체 창모드"
    };

    public static SettingsManager Instance { get; private set; }
    public static int ScreenModeCount => ScreenModes.Length;

    public int MinBrightness => defaultSettings != null ? defaultSettings.minBrightness : 0;

    [SerializeField] private DefaultSettings defaultSettings;

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Brightness")]
    [SerializeField] private CanvasGroup brightnessOverlay;

    public GameObject settingsUIInstance;

    private SettingsData settings;
    public SettingsData Settings => settings;
    public int ResolutionCount => FixedResolutions.Length;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (Instance != null)
            return;

        GameObject managerObject = new GameObject("RuntimeSettingsManager");
        managerObject.AddComponent<SettingsManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (GetComponentInParent<InGameSettingsMenuController>() != null)
            {
                Instance.AbsorbSceneReferences(defaultSettings, audioMixer, null, null);
            }
            else
            {
                Instance.AbsorbSceneReferences(defaultSettings, audioMixer, brightnessOverlay, settingsUIInstance);
            }

            enabled = false;
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        EnsureBrightnessOverlay();
        ApplySettings();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureBrightnessOverlay();
        TryResolveAudioMixerFromScene();
        ApplySettings();
    }

    public void RegisterAudioMixer(AudioMixer mixer)
    {
        if (mixer == null)
            return;

        audioMixer = mixer;
        ApplyVolume();
    }

    public void RegisterBrightnessOverlay(CanvasGroup overlay)
    {
        if (overlay == null)
            return;

        brightnessOverlay = overlay;
        ApplyBrightness();
    }

    public string GetResolutionString(int index)
    {
        index = Mathf.Clamp(index, 0, FixedResolutions.Length - 1);
        ResolutionOption option = FixedResolutions[index];
        return option.width + " x " + option.height;
    }

    public string GetCurrentResolutionString()
    {
        return GetResolutionString(settings.resolutionIndex);
    }

    public string GetScreenModeString(int index)
    {
        index = Mathf.Clamp(index, 0, ScreenModeLabels.Length - 1);
        return ScreenModeLabels[index];
    }

    public string GetCurrentScreenModeString()
    {
        return GetScreenModeString(settings.screenModeIndex);
    }

    private void Update()
    {
        ClearDestroyedSettingsUIReference();

        if (SettingsUI.ShouldBlockEscapeNavigation)
            return;

        if (InGameSettingsMenuController.Exists)
            return;

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (FindObjectOfType<TitleUIController>() != null)
            return;

        if (settingsUIInstance != null)
            settingsUIInstance.SetActive(!settingsUIInstance.activeSelf);
    }

    public void LoadSettings()
    {
        SettingsData defaults = defaultSettings != null ? defaultSettings.ToData() : SettingsData.CreateDefault();
        settings = SettingsData.Load(defaults);
    }

    public void SaveSettings()
    {
        SettingsData.Save(settings);
    }

    public SettingsData GetSettings()
    {
        return settings;
    }

    public void SetMasterVolume(int value)
    {
        settings.masterVolume = Mathf.Clamp(value, 0, 100);
        ApplyVolume();
        SaveSettings();
    }

    public void SetBgmVolume(int value)
    {
        settings.bgmVolume = Mathf.Clamp(value, 0, 100);
        ApplyVolume();
        SaveSettings();
    }

    public void SetSfxVolume(int value)
    {
        settings.sfxVolume = Mathf.Clamp(value, 0, 100);
        ApplyVolume();
        SaveSettings();
    }

    public void SetBrightness(int value)
    {
        settings.brightness = Mathf.Clamp(value, MinBrightness, 100);
        ApplyBrightness();
        SaveSettings();
    }

    public void SetResolutionIndex(int index)
    {
        settings.resolutionIndex = Mathf.Clamp(index, 0, FixedResolutions.Length - 1);
        ApplyResolutionAndScreenMode();
        SaveSettings();
    }

    public void SetFullscreen(bool fullscreen)
    {
        settings.screenModeIndex = fullscreen ? 0 : 1;
        ApplyResolutionAndScreenMode();
        SaveSettings();
    }

    public void SetScreenModeIndex(int index)
    {
        settings.screenModeIndex = Mathf.Clamp(index, 0, ScreenModes.Length - 1);
        ApplyResolutionAndScreenMode();
        SaveSettings();
    }

    public void CycleScreenMode()
    {
        SetScreenModeIndex((settings.screenModeIndex + 1) % ScreenModes.Length);
    }

    public void ApplySettings()
    {
        if (settings == null)
            LoadSettings();

        settings.Clamp();
        settings.brightness = Mathf.Max(settings.brightness, MinBrightness);
        ApplyVolume();
        ApplyBrightness();
        ApplyResolutionAndScreenMode();
    }

    public void ApplyVolume()
    {
        if (settings == null)
            return;

        TryResolveAudioMixerFromScene();

        float master = settings.masterVolume / 100f;
        float bgm = settings.bgmVolume / 100f;
        float sfx = settings.sfxVolume / 100f;

        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", LinearToDecibel(master));
            audioMixer.SetFloat("BGMVolume", LinearToDecibel(bgm));
            audioMixer.SetFloat("SFXVolume", LinearToDecibel(sfx));
        }

        AudioListener.volume = master;
    }

    public void ApplyBrightness()
    {
        if (settings == null)
            return;

        EnsureBrightnessOverlay();

        if (brightnessOverlay == null)
            return;

        brightnessOverlay.alpha = 1f - (settings.brightness / 100f);
    }

    public void ApplyResolutionAndScreenMode()
    {
        if (settings == null)
            return;

        int resolutionIndex = Mathf.Clamp(settings.resolutionIndex, 0, FixedResolutions.Length - 1);
        int screenModeIndex = Mathf.Clamp(settings.screenModeIndex, 0, ScreenModes.Length - 1);
        ResolutionOption resolution = FixedResolutions[resolutionIndex];
        FullScreenMode mode = ScreenModes[screenModeIndex];

        Screen.SetResolution(resolution.width, resolution.height, mode);
    }

    public void ToggleSettingsUI()
    {
        ClearDestroyedSettingsUIReference();

        if (settingsUIInstance != null)
            settingsUIInstance.SetActive(!settingsUIInstance.activeSelf);
    }

    private void AbsorbSceneReferences(DefaultSettings sceneDefaults, AudioMixer sceneMixer, CanvasGroup sceneBrightnessOverlay, GameObject sceneSettingsUI)
    {
        if (defaultSettings == null && sceneDefaults != null)
            defaultSettings = sceneDefaults;

        if (sceneMixer != null)
            audioMixer = sceneMixer;

        if (sceneBrightnessOverlay != null)
            brightnessOverlay = sceneBrightnessOverlay;

        if (sceneSettingsUI != null)
            settingsUIInstance = sceneSettingsUI;

        if (settings == null)
            LoadSettings();

        ApplySettings();
    }

    private void ClearDestroyedSettingsUIReference()
    {
        if (settingsUIInstance == null)
            settingsUIInstance = null;
    }

    private void TryResolveAudioMixerFromScene()
    {
        if (audioMixer != null)
            return;

        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source.outputAudioMixerGroup == null)
                continue;

            audioMixer = source.outputAudioMixerGroup.audioMixer;
            return;
        }
    }

    private float LinearToDecibel(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }

    private void EnsureBrightnessOverlay()
    {
        if (brightnessOverlay != null)
            return;

        GameObject canvasObject = new GameObject("RuntimeBrightnessOverlayCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlayObject = new GameObject("RuntimeBrightnessOverlay");
        overlayObject.transform.SetParent(canvasObject.transform, false);

        Image image = overlayObject.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        brightnessOverlay = overlayObject.AddComponent<CanvasGroup>();
        brightnessOverlay.blocksRaycasts = false;
        brightnessOverlay.interactable = false;
    }
}
