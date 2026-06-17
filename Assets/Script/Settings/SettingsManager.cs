using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [SerializeField] private DefaultSettings defaultSettings;

    [Header("Volume")]
    [Tooltip("AudioMixer 에셋 (없으면 AudioListener.volume으로 마스터 볼륨만 제어)")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Brightness")]
    [Tooltip("전체화면 검정 오버레이 CanvasGroup (alpha 0=밝음, 1=어둠)")]
    [SerializeField] private CanvasGroup brightnessOverlay;

    public GameObject settingsUIInstance;
    private SettingsData settings;
    public SettingsData Settings => settings;

    private Resolution[] resolutions;
    public int ResolutionCount => resolutions != null ? resolutions.Length : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CacheResolutions();
        LoadSettings();
        ApplySettings();
    }

    private void CacheResolutions()
    {
        var all = Screen.resolutions;
        var seen = new HashSet<string>();
        var unique = new List<Resolution>();
        foreach (var r in all)
        {
            string key = r.width + "x" + r.height;
            if (seen.Add(key)) unique.Add(r);
        }
        resolutions = unique.ToArray();
    }

    public string GetResolutionString(int index)
    {
        if (resolutions == null || index < 0 || index >= resolutions.Length) return "-";
        return resolutions[index].width + " x " + resolutions[index].height;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && settingsUIInstance != null)
            settingsUIInstance.SetActive(!settingsUIInstance.activeSelf);
    }

    public void LoadSettings()
    {
        settings = SettingsData.Load(defaultSettings.ToData());
    }

    public void SaveSettings()
    {
        SettingsData.Save(settings);
    }

    public SettingsData GetSettings() => settings;

    public void SetMasterVolume(int value)
    {
        settings.masterVolume = value;
        ApplyVolume();
        SaveSettings();
    }

    public void SetBgmVolume(int value)
    {
        settings.bgmVolume = value;
        ApplyVolume();
        SaveSettings();
    }

    public void SetSfxVolume(int value)
    {
        settings.sfxVolume = value;
        ApplyVolume();
        SaveSettings();
    }

    public void SetBrightness(int value)
    {
        settings.brightness = value;
        ApplyBrightness();
        SaveSettings();
    }

    public void SetResolutionIndex(int index)
    {
        settings.resolutionIndex = Mathf.Clamp(index, 0, ResolutionCount - 1);
        ApplyResolution();
        SaveSettings();
    }

    public void SetFullscreen(bool fullscreen)
    {
        settings.fullscreen = fullscreen;
        ApplyFullscreen();
        SaveSettings();
    }

    public void ApplySettings()
    {
        ApplyVolume();
        ApplyBrightness();
        ApplyResolution();
        ApplyFullscreen();
    }

    private void ApplyVolume()
    {
        float master = settings.masterVolume / 100f;
        float bgm    = settings.bgmVolume    / 100f;
        float sfx    = settings.sfxVolume    / 100f;

        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", LinearToDecibel(master));
            audioMixer.SetFloat("BGMVolume",    LinearToDecibel(bgm));
            audioMixer.SetFloat("SFXVolume",    LinearToDecibel(sfx));
        }
        else
        {
            AudioListener.volume = master;
        }
    }

    private float LinearToDecibel(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }

    private void ApplyBrightness()
    {
        if (brightnessOverlay != null)
            brightnessOverlay.alpha = 1f - (settings.brightness / 100f);
    }

    private void ApplyResolution()
    {
        if (resolutions == null || resolutions.Length == 0) return;
        int idx = Mathf.Clamp(settings.resolutionIndex, 0, resolutions.Length - 1);
        Resolution r = resolutions[idx];
        Screen.SetResolution(r.width, r.height, settings.fullscreen);
    }

    private void ApplyFullscreen()
    {
        Screen.fullScreen = settings.fullscreen;
    }

    public void ToggleSettingsUI()
    {
        if (settingsUIInstance != null)
            settingsUIInstance.SetActive(!settingsUIInstance.activeSelf);
    }
}
