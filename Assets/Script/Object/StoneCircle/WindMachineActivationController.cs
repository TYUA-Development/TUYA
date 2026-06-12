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

    private IEnumerator ActivationSequence()
    {
        // 1. 화살 명중음
        PlayOneShot(arrowHitClip);

        yield return new WaitForSeconds(lockDelay);

        // 2. 철컥, 잠금 해제
        PlayOneShot(lockClickClip);
        PlayParticles(startParticles);

        yield return new WaitForSeconds(centerStartDelay);

        // 3. 기계 가동음
        PlayOneShot(machineStartClip);

        // 4. 루프 기계음 시작
        StartMachineLoop();

        // 5. 중앙 작은 프로펠러 회전 시작
        if (centerPropeller != null)
        {
            centerPropeller.SetTargetSpeed(centerPropellerSpeed);
        }

        yield return new WaitForSeconds(backgroundStartDelay);

        // 6. 배경 프로펠러 순차 가동
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

        // 7. 큰 원형 통로 반복 회전 시작
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