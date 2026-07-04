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

    [Tooltip("Delay after the screen fade finishes before loading the next scene.")]
    public float delayBeforeLoadScene = 5f;

    [Header("Scene Audio Fade")]
    public AudioSource[] sceneAudioSources;
    public bool fadeAllSceneAudioIfEmpty = true;
    public float audioFadeOutTime = 1.5f;

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

        AudioSource[] audioSources = GetSceneAudioSources();
        float[] startVolumes = GetStartVolumes(audioSources);
        float timer = 0f;

        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            float fadeT = fadeOutTime <= 0f ? 1f : Mathf.Clamp01(timer / fadeOutTime);
            float audioT = audioFadeOutTime <= 0f ? 1f : Mathf.Clamp01(timer / audioFadeOutTime);

            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, maxFadeAlpha, fadeT);

            FadeAudioSources(audioSources, startVolumes, audioT);

            yield return null;
        }

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = maxFadeAlpha;

        FadeAudioSources(audioSources, startVolumes, 1f);

        if (delayBeforeLoadScene > 0f)
            yield return new WaitForSeconds(delayBeforeLoadScene);

        SceneManager.LoadScene(nextSceneName);
    }

    private AudioSource[] GetSceneAudioSources()
    {
        if (sceneAudioSources != null && sceneAudioSources.Length > 0)
            return sceneAudioSources;

        if (!fadeAllSceneAudioIfEmpty)
            return new AudioSource[0];

        return FindObjectsOfType<AudioSource>();
    }

    private float[] GetStartVolumes(AudioSource[] audioSources)
    {
        if (audioSources == null)
            return new float[0];

        float[] volumes = new float[audioSources.Length];

        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
                volumes[i] = audioSources[i].volume;
        }

        return volumes;
    }

    private void FadeAudioSources(AudioSource[] audioSources, float[] startVolumes, float t)
    {
        if (audioSources == null || startVolumes == null)
            return;

        int count = Mathf.Min(audioSources.Length, startVolumes.Length);

        for (int i = 0; i < count; i++)
        {
            if (audioSources[i] != null)
                audioSources[i].volume = Mathf.Lerp(startVolumes[i], 0f, t);
        }
    }
}
