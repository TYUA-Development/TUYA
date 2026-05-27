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
        Debug.Log("이어하기 기능은 아직 준비되지 않았습니다.");
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        Debug.Log("게임 종료 버튼 눌림. 빌드에서는 게임이 종료됩니다.");
#endif
    }
}