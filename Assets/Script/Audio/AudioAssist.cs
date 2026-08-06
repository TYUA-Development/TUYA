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

    [Header("3D Sound")]
    [Tooltip("켜면 AudioListener(보통 메인 카메라, 대개 플레이어를 따라다님)와의 거리에 따라 자동으로 소리 크기가 줄어드는 3D 사운드로 재생됩니다. 끄면 거리와 무관하게 항상 같은 크기로 들리는 2D 사운드입니다.")]
    public bool spatial3D = true;

    [Tooltip("이 거리 이내에서는 항상 최대 볼륨으로 들립니다.")]
    public float minDistance = 3f;

    [Tooltip("이 거리를 넘어가면 (Rolloff Mode에 따라) 소리가 거의/완전히 들리지 않습니다.")]
    public float maxDistance = 20f;

    [Tooltip("거리가 멀어질수록 볼륨이 줄어드는 방식")]
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    private AudioSource audioSource;
    private Coroutine curveRoutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loop;

        audioSource.spatialBlend = spatial3D ? 1f : 0f;
        audioSource.rolloffMode = rolloffMode;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;

        if (playOnAwake)
            Play();
    }

    public void Play()
    {
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
        audioSource.spatialBlend = spatial3D ? 1f : 0f;
        audioSource.rolloffMode = rolloffMode;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.volume = volumeCurve.Evaluate(0f) * volume * entry.volume;
        audioSource.Play();

        curveRoutine = StartCoroutine(ApplyVolumeCurve(entry.volume));
    }

    public void Stop()
    {
        if (curveRoutine != null)
        {
            StopCoroutine(curveRoutine);
            curveRoutine = null;
        }

        audioSource.Stop();
    }

    public void FadeOut(float duration)
    {
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
