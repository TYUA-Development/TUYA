using UnityEngine;

public enum Language
{
    Korean = 0,
    English = 1,
    Japanese = 2,
    ChineseSimplified = 3,
    ChineseTraditional = 4
}

[System.Serializable]
public class SettingsData
{
    public const string MasterVolumeKey = "Settings.MasterVolume";
    public const string BgmVolumeKey = "Settings.BgmVolume";
    public const string SfxVolumeKey = "Settings.SfxVolume";
    public const string BrightnessKey = "Settings.Brightness";
    public const string ResolutionIndexKey = "Settings.ResolutionIndex";
    public const string ScreenModeIndexKey = "Settings.ScreenModeIndex";
    public const string LanguageIndexKey = "Settings.LanguageIndex";

    public int masterVolume;
    public int bgmVolume;
    public int sfxVolume;
    public int brightness;
    public int resolutionIndex;
    public int screenModeIndex;
    public int languageIndex;

    public bool fullscreen
    {
        get { return screenModeIndex != 1; }
        set { screenModeIndex = value ? 0 : 1; }
    }

    public static void Save(SettingsData data)
    {
        if (data == null)
            return;

        PlayerPrefs.SetInt(MasterVolumeKey, Mathf.Clamp(data.masterVolume, 0, 100));
        PlayerPrefs.SetInt(BgmVolumeKey, Mathf.Clamp(data.bgmVolume, 0, 100));
        PlayerPrefs.SetInt(SfxVolumeKey, Mathf.Clamp(data.sfxVolume, 0, 100));
        PlayerPrefs.SetInt(BrightnessKey, Mathf.Clamp(data.brightness, 0, 100));
        PlayerPrefs.SetInt(ResolutionIndexKey, Mathf.Clamp(data.resolutionIndex, 0, SettingsManager.FixedResolutions.Length - 1));
        PlayerPrefs.SetInt(ScreenModeIndexKey, Mathf.Clamp(data.screenModeIndex, 0, SettingsManager.ScreenModeCount - 1));
        PlayerPrefs.SetInt(LanguageIndexKey, Mathf.Clamp(data.languageIndex, 0, SettingsManager.LanguageCount - 1));

        PlayerPrefs.Save();
    }

    public static SettingsData Load(SettingsData defaults)
    {
        SettingsData fallback = defaults ?? CreateDefault();
        SettingsData data = new SettingsData();

        data.masterVolume = PlayerPrefs.GetInt(MasterVolumeKey, PlayerPrefs.GetInt("MasterVolume", fallback.masterVolume));
        data.bgmVolume = PlayerPrefs.GetInt(BgmVolumeKey, PlayerPrefs.GetInt("BgmVolume", fallback.bgmVolume));
        data.sfxVolume = PlayerPrefs.GetInt(SfxVolumeKey, PlayerPrefs.GetInt("SfxVolume", fallback.sfxVolume));
        data.brightness = PlayerPrefs.GetInt(BrightnessKey, PlayerPrefs.GetInt("Brightness", fallback.brightness));
        data.resolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey, PlayerPrefs.GetInt("ResolutionIndex", fallback.resolutionIndex));

        int legacyFullscreen = PlayerPrefs.GetInt("Fullscreen", fallback.fullscreen ? 1 : 0);
        int legacyScreenMode = legacyFullscreen == 1 ? 0 : 1;
        data.screenModeIndex = PlayerPrefs.GetInt(ScreenModeIndexKey, legacyScreenMode);

        data.languageIndex = PlayerPrefs.GetInt(LanguageIndexKey, fallback.languageIndex);

        data.Clamp();
        return data;
    }

    public static SettingsData CreateDefault()
    {
        return new SettingsData
        {
            masterVolume = 100,
            bgmVolume = 100,
            sfxVolume = 100,
            brightness = 100,
            resolutionIndex = 2,
            screenModeIndex = 0,
            languageIndex = 0
        };
    }

    public void Clamp()
    {
        masterVolume = Mathf.Clamp(masterVolume, 0, 100);
        bgmVolume = Mathf.Clamp(bgmVolume, 0, 100);
        sfxVolume = Mathf.Clamp(sfxVolume, 0, 100);
        brightness = Mathf.Clamp(brightness, 0, 100);
        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, SettingsManager.FixedResolutions.Length - 1);
        screenModeIndex = Mathf.Clamp(screenModeIndex, 0, SettingsManager.ScreenModeCount - 1);
        languageIndex = Mathf.Clamp(languageIndex, 0, SettingsManager.LanguageCount - 1);
    }
}

[CreateAssetMenu(menuName = "Settings/Default Settings", fileName = "DefaultSettings")]
public class DefaultSettings : ScriptableObject
{
    public int masterVolume = 100;
    public int bgmVolume = 100;
    public int sfxVolume = 100;
    public int brightness = 100;
    [Range(0, 100)]
    public int minBrightness = 0;
    public int resolutionIndex = 2;
    public int screenModeIndex = 0;
    public int languageIndex = 0;

    public bool fullscreen
    {
        get { return screenModeIndex != 1; }
        set { screenModeIndex = value ? 0 : 1; }
    }

    public SettingsData ToData()
    {
        SettingsData data = new SettingsData
        {
            masterVolume = masterVolume,
            bgmVolume = bgmVolume,
            sfxVolume = sfxVolume,
            brightness = brightness,
            resolutionIndex = resolutionIndex,
            screenModeIndex = screenModeIndex,
            languageIndex = languageIndex
        };

        data.Clamp();
        return data;
    }
}
