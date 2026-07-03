using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenuController : MonoBehaviour
{
    public string newGameSceneName = "Forest";

    public GameObject settingsPanel;

    public void NewGame()
    {
        SceneManager.LoadScene(newGameSceneName);
    }

    public void ContinueGame()
    {
        Debug.Log("Continue is not ready yet.");
    }

    public void OpenSettings()
    {
        TitleUIController titleUI = FindObjectOfType<TitleUIController>();
        if (titleUI != null)
        {
            titleUI.OpenSettings();
            return;
        }

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        TitleUIController titleUI = FindObjectOfType<TitleUIController>();
        if (titleUI != null)
        {
            titleUI.CloseSettings();
            return;
        }

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        Debug.Log("Quit button pressed. The built game will close.");
#endif
    }
}
