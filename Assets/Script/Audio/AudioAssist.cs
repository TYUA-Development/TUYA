using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AudioAssistClip
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume;
}

[RequireComponent(typeof(AudioSource))]
public class AudioAssist : MonoBehaviour, IAudioAssist
{
    [Header("Clip")]
    public List<AudioAssistClip> clips = new List<AudioAssistClip>();

    [Header("Volume")]
    [Range(0f, 1f)] public float volume = 1f;
    public AnimationCurve volumeCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Pitch")]
    public float minPitch = 1f;
    public float maxPitch = 1f;

    [Header("Playback")]
    public bool loop = false;
    public bool playOnAwake = false;

    private AudioSource audioSource;
    private Coroutine curveRoutine;

    private void Awake()
    {
        EnsureAudioSource();

        if (playOnAwake)
            Play();
    }

    // Awake()에만 의존하면, 이 컴포넌트가 붙은 GameObject가 처음부터 비활성화된 채로
    // 배치되어 있다가 다른 스크립트의 OnEnable()에서 (같은 SetActive(true) 호출의 일부로)
    // Play()를 곧바로 호출하는 경우, Awake가 아직 실행되지 않아 audioSource가 null인 채로
    // NullReferenceException이 난다 (예: Object_Wind.OnEnable() -> loop_Wind.Play()). 그러면
    // 그 예외가 호출자의 코루틴을 그 자리에서 중단시켜 뒤따르는 로직까지 함께 멈춰버릴 수
    // 있으므로, Play/Stop/FadeOut 진입 시 항상 지연 초기화로 방어한다.
    private void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loop;
    }

    public void Play()
    {
        EnsureAudioSource();

        if (clips == null || clips.Count == 0)
            return;

        AudioAssistClip entry = clips[Random.Range(0, clips.Count)];

        if (entry.clip == null)
            return;

        if (curveRoutine != null)
        {
            StopCoroutine(curveRoutine);
            curveRoutine = null;
        }

        audioSource.Stop();
        audioSource.clip = entry.clip;
        audioSource.loop = loop;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.volume = volumeCurve.Evaluate(0f) * volume * entry.volume;
        audioSource.Play();

        curveRoutine = StartCoroutine(ApplyVolumeCurve(entry.volume));
    }

    public void Stop()
    {
        EnsureAudioSource();

        if (curveRoutine != null)
        {
            StopCoroutine(curveRoutine);
            curveRoutine = null;
        }

        audioSource.Stop();
    }

    public void FadeIn(float duration)
    {
        EnsureAudioSource();

        if (clips == null || clips.Count == 0)
            return;

        AudioAssistClip entry = clips[Random.Range(0, clips.Count)];

        if (entry.clip == null)
            return;

        if (curveRoutine != null)
        {
            StopCoroutine(curveRoutine);
            curveRoutine = null;
        }

        audioSource.Stop();
        audioSource.clip = entry.clip;
        audioSource.loop = loop;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.volume = 0f;
        audioSource.Play();

        curveRoutine = StartCoroutine(FadeInRoutine(duration, entry.volume));
    }

    private IEnumerator FadeInRoutine(float duration, float clipVolume)
    {
        float targetVolume = volumeCurve.Evaluate(0f) * volume * clipVolume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, timer / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
        curveRoutine = StartCoroutine(ApplyVolumeCurve(clipVolume));
    }

    public void FadeOut(float duration)
    {
        EnsureAudioSource();

        if (curveRoutine != null)
        {
            StopCoroutine(curveRoutine);
            curveRoutine = null;
        }

        if (!audioSource.isPlaying)
            return;

        if (duration <= 0f)
        {
            audioSource.Stop();
            return;
        }

        curveRoutine = StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        audioSource.Stop();
        curveRoutine = null;
    }

    private IEnumerator ApplyVolumeCurve(float clipVolume)
    {
        AudioClip clip = audioSource.clip;

        if (clip == null || clip.length <= 0f)
            yield break;

        while (audioSource.isPlaying)
        {
            float t = Mathf.Clamp01(audioSource.time / clip.length);
            audioSource.volume = volumeCurve.Evaluate(t) * volume * clipVolume;
            yield return null;
        }

        curveRoutine = null;
    }
}
