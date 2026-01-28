using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SettingsData
{
    public int masterVolume;
    public int bgmVolume;
    public int sfxVolume;

    public int brightness;

    public int resolutionIndex;
    public bool fullscreen;

    public static void Save(SettingsData data)
    {
        PlayerPrefs.SetInt("MasterVolume", data.masterVolume);
        PlayerPrefs.SetInt("BgmVolume", data.bgmVolume);
        PlayerPrefs.SetInt("SfxVolume", data.sfxVolume);

        PlayerPrefs.SetInt("Brightness", data.brightness);

        PlayerPrefs.SetInt("ResolutionIndex", data.resolutionIndex);

        PlayerPrefs.SetInt("Fullscreen", data.fullscreen ? 1 : 0);

        PlayerPrefs.Save();
    }

    public static SettingsData Load(SettingsData defaults)
    {
        SettingsData data = new SettingsData();

        data.masterVolume = PlayerPrefs.GetInt("MasterVolume", defaults.masterVolume);
        data.bgmVolume = PlayerPrefs.GetInt("BgmVolume", defaults.bgmVolume);
        data.sfxVolume = PlayerPrefs.GetInt("SfxVolume", defaults.sfxVolume);

        data.brightness = PlayerPrefs.GetInt("Brightness", defaults.brightness);

        data.resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", defaults.resolutionIndex);

        data.fullscreen = PlayerPrefs.GetInt("Fullscreen", defaults.fullscreen ? 1 : 0) == 1;

        return data;
    }
}

[CreateAssetMenu(menuName = "Settings/Default Settings", fileName = "DefaultSettings")]
public class DefaultSettings : ScriptableObject
{
    public int masterVolume = 100;
    public int bgmVolume = 100;
    public int sfxVolume = 100;

    public int brightness = 100;

    public int resolutionIndex = 0;
    public bool fullscreen = true;

    public SettingsData ToData() => new SettingsData
    {
        
        masterVolume = masterVolume,
        bgmVolume = bgmVolume,
        sfxVolume = sfxVolume,
        brightness = brightness,
        resolutionIndex = resolutionIndex,
        fullscreen = fullscreen
        
    };
}