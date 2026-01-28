using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageUI : MonoBehaviour
{
    public GameObject SettingWindow;
    // Start is called before the first frame update
    private void Start()
    {
        StageUIInput.OnEscapePressed += HandleEscape;
    }

    private void OnDestroy()
    {
        
        StageUIInput.OnEscapePressed -= HandleEscape;
    }

    void HandleEscape()
    {
        ToggleSettings();
    }

    void ToggleSettings()
    {
        bool open = !SettingWindow.activeSelf;
        SettingWindow.SetActive(open);
        Debug.Log("ESC");
    }
}
