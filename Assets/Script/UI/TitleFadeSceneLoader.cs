using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleFadeSceneLoader : MonoBehaviour
{
    [Header("Fade Image")]
    public CanvasGroup fadeCanvasGroup;

    [Header("Scene")]
    public string nextSceneName = "Forest";

    [Header("Fade Settings")]
    public float fadeOutTime = 2.8f;

    [Range(0f, 1f)]
    public float maxFadeAlpha = 1f;

    [Header("Title Audio Fade")]
    public AudioSource[] titleAudioSources;
    public float audioFadeOutTime = 2.4f;

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
            fadeCanvasGroup.blocksRaycasts = true;

        float[] startVolumes = new float[titleAudioSources.Length];

        for (int i = 0; i < titleAudioSources.Length; i++)
        {
            if (titleAudioSources[i] != null)
                startVolumes[i] = titleAudioSources[i].volume;
        }

        float timer = 0f;

        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;

            float fadeT = Mathf.Clamp01(timer / fadeOutTime);
            float audioT = Mathf.Clamp01(timer / audioFadeOutTime);

            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, maxFadeAlpha, fadeT);

            for (int i = 0; i < titleAudioSources.Length; i++)
            {
                if (titleAudioSources[i] != null)
                    titleAudioSources[i].volume = Mathf.Lerp(startVolumes[i], 0f, audioT);
            }

            yield return null;
        }

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = maxFadeAlpha;

        for (int i = 0; i < titleAudioSources.Length; i++)
        {
            if (titleAudioSources[i] != null)
                titleAudioSources[i].volume = 0f;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}