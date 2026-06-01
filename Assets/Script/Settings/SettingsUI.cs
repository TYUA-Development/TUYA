using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public Slider masterVolumSlider;
    public SettingsData settingsData;
    //private SettingsData currentSettings;
    // Start is called before the first frame update
    void Awake()
    {
        SettingsManager.Instance.settingsUIInstance = gameObject;

        settingsData = SettingsManager.Instance.Settings;

        Init();

        gameObject.SetActive(false);
        //currentSettings = SettingsManager.Instance.settings;
    }

    private void Start()
    {
        masterVolumSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMasterVolume((int)(value * 100));
    }

    public void OnExitButton()
    {
        gameObject.SetActive(false);
    }

    private void Init()
    {
        masterVolumSlider.value = settingsData.masterVolume / 100;
    }
}
