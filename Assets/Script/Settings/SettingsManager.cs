using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [SerializeField] private DefaultSettings defaultSettings;

    public GameObject settingsUIInstance;
    private SettingsData settings;
    public SettingsData Settings => settings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplySettings();
        Debug.Log(settings.masterVolume.ToString());

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsUI();
        }

        Debug.Log(settings.masterVolume.ToString());
    }

    public void ToggleSettingsUI()
    {
        bool active = !settingsUIInstance.activeSelf;
        settingsUIInstance.SetActive(active);
    }

    public void LoadSettings()
    {
        settings = SettingsData.Load(defaultSettings.ToData());
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

    public void ApplySettings()
    {
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        float master = settings.masterVolume / 100f;
        float bgm = settings.bgmVolume / 100f;
        float sfx = settings.sfxVolume / 100f;

        AudioListener.volume = master;

        Debug.Log($"Master: {master}, BGM: {bgm}, SFX: {sfx}");
    }
}