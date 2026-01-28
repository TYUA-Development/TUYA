using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [SerializeField]
    private DefaultSettings defaultSettings;
    SettingsData settings;

    //public event Action OnEscapePress;

    private void Awake()
    {
        
        if (Instance != null && Instance != this) 
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }

        LoadSettings();
    }

    public void LoadSettings()
    {
        settings = SettingsData.Load(defaultSettings.ToData());
    }

    public void SaveSettings()
    {
        SettingsData.Save(settings);
    }
}
