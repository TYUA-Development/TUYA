using UnityEngine;
using System.Collections;

public class SteppeZoneTrigger : MonoBehaviour
{
    public AudioSource steppeBGM;
    public AudioSource steppeAmbience;

    public float bgmTargetVolume = 0.25f;
    public float ambienceTargetVolume = 0.6f;

    public float fadeInTime = 3f;
    public float fadeOutTime = 4f;

    private Coroutine fadeCoroutine;
    private float fadePercent = 0f;

    private void Start()
    {
        Setup(steppeBGM);
        Setup(steppeAmbience);
        PreloadClip(steppeBGM);
        PreloadClip(steppeAmbience);
    }

    private void Setup(AudioSource source)
    {
        if (source == null) return;

        source.volume = 0f;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void PreloadClip(AudioSource source)
    {
        if (source != null && source.clip != null)
            source.clip.LoadAudioData();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        StartFade(1f, fadeInTime);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        StartFade(0f, fadeOutTime);
    }

    private void StartFade(float targetPercent, float duration)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(targetPercent, duration));
    }

    private IEnumerator WaitForClipLoad(AudioSource source)
    {
        if (source == null || source.clip == null) yield break;
        if (source.clip.loadState == AudioDataLoadState.Unloaded)
            source.clip.LoadAudioData();
        while (source.clip.loadState == AudioDataLoadState.Loading)
            yield return null;
    }

    private IEnumerator FadeRoutine(float targetPercent, float duration)
    {
        if (targetPercent > 0f)
        {
            yield return WaitForClipLoad(steppeBGM);
            yield return WaitForClipLoad(steppeAmbience);
            PlayIfNotPlaying(steppeBGM);
            PlayIfNotPlaying(steppeAmbience);
        }

        float startPercent = fadePercent;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;
            fadePercent = Mathf.Lerp(startPercent, targetPercent, t);

            ApplyVolume();

            yield return null;
        }

        fadePercent = targetPercent;
        ApplyVolume();

        if (fadePercent <= 0f)
        {
            StopIfPlaying(steppeBGM);
            StopIfPlaying(steppeAmbience);
        }

        fadeCoroutine = null;
    }

    private void ApplyVolume()
    {
        if (steppeBGM != null)
            steppeBGM.volume = bgmTargetVolume * fadePercent;

        if (steppeAmbience != null)
            steppeAmbience.volume = ambienceTargetVolume * fadePercent;
    }

    private void PlayIfNotPlaying(AudioSource source)
    {
        if (source != null && !source.isPlaying)
            source.Play();
    }

    private void StopIfPlaying(AudioSource source)
    {
        if (source == null) return;

        source.volume = 0f;

        if (source.isPlaying)
            source.Stop();
    }
}