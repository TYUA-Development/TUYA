using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleFadeSceneLoader : MonoBehaviour
{
    public CanvasGroup fadeCanvasGroup;
    public string nextSceneName = "Forest";

    public float fadeOutTime = 1.2f;
    public float fadeInTime = 1.0f;

    private bool isLoading = false;

    void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    public void StartNewGame()
    {
        if (isLoading) return;

        StartCoroutine(FadeOutAndLoadScene());
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        isLoading = true;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;

            float timer = 0f;

            while (timer < fadeOutTime)
            {
                timer += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeOutTime);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}