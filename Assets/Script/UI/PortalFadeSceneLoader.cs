using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalFadeSceneLoader : MonoBehaviour
{
    [Header("Fade Image")]
    public CanvasGroup fadeCanvasGroup;

    [Header("Scene")]
    public string nextSceneName = "SeungHyun2_Restore";

    [Header("Fade Settings")]
    public float fadeOutTime = 1.5f;

    [Range(0f, 1f)]
    public float maxFadeAlpha = 1f;

    private bool isLoading = false;

    void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading) return;
        if (!other.CompareTag("Player")) return;

        StartCoroutine(FadeOutAndLoadScene());
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        isLoading = true;

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.blocksRaycasts = true;

        float timer = 0f;

        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, maxFadeAlpha, timer / fadeOutTime);
            yield return null;
        }

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = maxFadeAlpha;

        SceneManager.LoadScene(nextSceneName);
    }
}
