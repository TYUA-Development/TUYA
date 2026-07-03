using UnityEngine;

public class TitleUIController : MonoBehaviour
{
    [Header("Root")]
    public GameObject settingsPanel;

    [Header("Settings Pages")]
    public GameObject settingsMainPanel;
    public GameObject audioPanel;
    public GameObject graphicsPanel;
    public GameObject controlPanel;

    void Start()
    {
        CloseSettings();
    }

    void Update()
    {
        if (SettingsUI.ShouldBlockEscapeNavigation)
            return;

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Back();
            }
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        ShowMainPanel();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        HideAllPages();
    }

    public void ShowMainPanel()
    {
        HideAllPages();

        if (settingsMainPanel != null)
            settingsMainPanel.SetActive(true);
    }

    public void OpenAudioPanel()
    {
        HideAllPages();

        if (audioPanel != null)
            audioPanel.SetActive(true);
    }

    public void OpenGraphicsPanel()
    {
        HideAllPages();

        if (graphicsPanel != null)
            graphicsPanel.SetActive(true);
    }

    public void OpenControlPanel()
    {
        HideAllPages();

        if (controlPanel != null)
            controlPanel.SetActive(true);
    }

    public void Back()
    {
        if (audioPanel != null && audioPanel.activeSelf)
        {
            ShowMainPanel();
            return;
        }

        if (graphicsPanel != null && graphicsPanel.activeSelf)
        {
            ShowMainPanel();
            return;
        }

        if (controlPanel != null && controlPanel.activeSelf)
        {
            ShowMainPanel();
            return;
        }

        CloseSettings();
    }

    void HideAllPages()
    {
        if (settingsMainPanel != null)
            settingsMainPanel.SetActive(false);

        if (audioPanel != null)
            audioPanel.SetActive(false);

        if (graphicsPanel != null)
            graphicsPanel.SetActive(false);

        if (controlPanel != null)
            controlPanel.SetActive(false);
    }
}
