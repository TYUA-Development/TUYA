using System.Collections;
using UnityEngine;

public class WindMachineActivationController : MonoBehaviour
{
    [Header("Propellers")]
    public PropellerSpinner centerPropeller;
    public PropellerSpinner[] backgroundPropellers;

    [Header("Rotating Passage")]
    public RotatingPassageLooper rotatingPassage;

    [Header("Speed")]
    public float centerPropellerSpeed = 420f;
    public float backgroundPropellerSpeed = 220f;

    [Header("Timing")]
    public float lockDelay = 0.15f;
    public float centerStartDelay = 0.25f;
    public float backgroundStartDelay = 0.45f;
    public float backgroundStaggerDelay = 0.18f;
    public float passageStartDelay = 0.8f;

    [Header("Audio")]
    public AudioSource oneShotAudioSource;
    public AudioSource loopAudioSource;

    public AudioClip arrowHitClip;
    public AudioClip lockClickClip;
    public AudioClip machineStartClip;
    public AudioClip backgroundStartClip;
    public AudioClip passageStartClip;

    [Header("Loop Audio")]
    public AudioClip machineLoopClip;
    public float loopTargetVolume = 0.45f;
    public float loopFadeInTime = 1.5f;

    [Header("Particles")]
    public ParticleSystem[] startParticles;
    public ParticleSystem[] windParticles;

    [Header("Stop")]
    [Tooltip("StopGradual 호출 시 루프 오디오 페이드 아웃 시간(초)")]
    public float stopFadeOutTime = 1.5f;

    [Header("Option")]
    public bool activateOnlyOnce = true;

    private bool activated = false;

    public void Activate()
    {
        if (activateOnlyOnce && activated)
            return;

        activated = true;
        StartCoroutine(ActivationSequence());
    }

    public void Stop()
    {
        StopAllCoroutines();
        activated = false;

        if (centerPropeller != null)
            centerPropeller.StopSpin();

        if (backgroundPropellers != null)
            foreach (var p in backgroundPropellers)
                if (p != null) p.StopSpin();

        if (rotatingPassage != null)
            rotatingPassage.StopLoop();

        if (loopAudioSource != null)
            loopAudioSource.Stop();

        StopParticleArray(startParticles);
        StopParticleArray(windParticles);
    }

    public void StopGradual()
    {
        StopAllCoroutines();
        activated = false;

        // 프로펠러: acceleration 기반 감속으로 자연스럽게 멈춤
        if (centerPropeller != null)
            centerPropeller.StopSpin();

        if (backgroundPropellers != null)
            foreach (var p in backgroundPropellers)
                if (p != null) p.StopSpin();

        // 통로: 루프만 중단 (현재 코루틴 자연 종료)
        if (rotatingPassage != null)
            rotatingPassage.StopLoop();

        // 오디오: 페이드 아웃
        if (loopAudioSource != null && loopAudioSource.isPlaying)
            StartCoroutine(FadeOutAudio(loopAudioSource, stopFadeOutTime));

        // 파티클: 신규 방출 중단, 기존 파티클은 자연 소멸
        StopParticleArrayGradual(startParticles);
        StopParticleArrayGradual(windParticles);
    }

    private IEnumerator FadeOutAudio(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(timer / duration));
            yield return null;
        }

        source.Stop();
        source.volume = startVolume;
    }

    private void StopParticleArray(ParticleSystem[] particles)
    {
        if (particles == null) return;
        foreach (var p in particles)
            if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void StopParticleArrayGradual(ParticleSystem[] particles)
    {
        if (particles == null) return;
        foreach (var p in particles)
            if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private IEnumerator ActivationSequence()
    {
        // 1. ȭ�� ������
        PlayOneShot(arrowHitClip);

        yield return new WaitForSeconds(lockDelay);

        // 2. ö��, ��� ����
        PlayOneShot(lockClickClip);
        PlayParticles(startParticles);

        yield return new WaitForSeconds(centerStartDelay);

        // 3. ��� ������
        PlayOneShot(machineStartClip);

        // 4. ���� ����� ����
        StartMachineLoop();

        // 5. �߾� ���� �����緯 ȸ�� ����
        if (centerPropeller != null)
        {
            centerPropeller.SetTargetSpeed(centerPropellerSpeed);
        }

        yield return new WaitForSeconds(backgroundStartDelay);

        // 6. ��� �����緯 ���� ����
        PlayOneShot(backgroundStartClip);

        if (backgroundPropellers != null)
        {
            for (int i = 0; i < backgroundPropellers.Length; i++)
            {
                if (backgroundPropellers[i] != null)
                {
                    backgroundPropellers[i].SetTargetSpeed(backgroundPropellerSpeed);
                }

                yield return new WaitForSeconds(backgroundStaggerDelay);
            }
        }

        PlayParticles(windParticles);

        yield return new WaitForSeconds(passageStartDelay);

        // 7. ū ���� ��� �ݺ� ȸ�� ����
        PlayOneShot(passageStartClip);

        if (rotatingPassage != null)
        {
            rotatingPassage.StartLoop();
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
            return;

        if (oneShotAudioSource == null)
            return;

        oneShotAudioSource.PlayOneShot(clip);
    }

    private void StartMachineLoop()
    {
        if (loopAudioSource == null)
            return;

        if (machineLoopClip != null)
        {
            loopAudioSource.clip = machineLoopClip;
        }

        loopAudioSource.loop = true;
        loopAudioSource.volume = 0f;
        loopAudioSource.Play();

        StartCoroutine(FadeInLoopAudio());
    }

    private IEnumerator FadeInLoopAudio()
    {
        if (loopAudioSource == null)
            yield break;

        float timer = 0f;

        while (timer < loopFadeInTime)
        {
            timer += Time.deltaTime;

            float t = timer / loopFadeInTime;
            t = Mathf.Clamp01(t);
            t = Mathf.SmoothStep(0f, 1f, t);

            loopAudioSource.volume = Mathf.Lerp(0f, loopTargetVolume, t);

            yield return null;
        }

        loopAudioSource.volume = loopTargetVolume;
    }

    private void PlayParticles(ParticleSystem[] particles)
    {
        if (particles == null)
            return;

        foreach (ParticleSystem particle in particles)
        {
            if (particle != null)
            {
                particle.Play();
            }
        }
    }
}