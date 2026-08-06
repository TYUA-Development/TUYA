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
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loop;

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
