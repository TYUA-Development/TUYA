using System.Collections;
using UnityEngine;

public class RisingObjectController : MonoBehaviour
{
    [Header("Rise Target")]
    [Tooltip("비워두면 이 오브젝트가 직접 상승합니다.")]
    public Transform objectToRise;

    [Tooltip("현재 위치에서 위로 올라갈 높이")]
    public float riseHeight = 3f;

    [Tooltip("상승 시간")]
    public float riseDuration = 3.5f;

    [Tooltip("상승 시작 전에 잠깐 멈칫하는 시간")]
    public float startDelay = 0.25f;

    [Tooltip("마지막에 살짝 멈추며 도착하는 느낌")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Small Shake Before Rise")]
    [Tooltip("상승 직전에 아주 약하게 떨림")]
    public bool usePreShake = true;

    [Tooltip("상승 전 떨림 시간")]
    public float preShakeTime = 0.45f;

    [Tooltip("상승 전 떨림 강도")]
    public float preShakePower = 0.025f;

    [Header("Shake During Rise")]
    [Tooltip("상승 중에도 계속 약하게 떨림")]
    public bool useShakeDuringRise = true;

    [Tooltip("상승 중 떨림 강도")]
    public float riseShakePower = 0.035f;

    [Tooltip("상승 중 떨림 속도")]
    public float riseShakeSpeed = 35f;

    [Tooltip("상승 후반부로 갈수록 떨림이 줄어듭니다.")]
    public bool fadeOutShakeNearEnd = true;

    [Header("Particles")]
    [Tooltip("바닥 먼지 파티클")]
    public ParticleSystem dustParticle;

    [Tooltip("빛 입자 파티클")]
    public ParticleSystem lightParticle;

    [Tooltip("작은 돌 부스러기 파티클")]
    public ParticleSystem debrisParticle;

    [Tooltip("완료 파티클")]
    public ParticleSystem completeParticle;

    [Header("Audio")]
    [Tooltip("상승 시작 소리")]
    public AudioSource riseStartAudio;

    [Tooltip("상승 중 반복되는 낮은 부유음 / 진동음")]
    public AudioSource riseLoopAudio;

    [Tooltip("돌 부스러기 / 마찰음")]
    public AudioSource debrisAudio;

    [Tooltip("상승 완료 소리")]
    public AudioSource completeAudio;

    [Header("State")]
    public bool hasRisen;

    private Coroutine riseCoroutine;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    private void Awake()
    {
        if (objectToRise == null)
            objectToRise = transform;

        startPosition = objectToRise.position;
        targetPosition = startPosition + new Vector3(0f, riseHeight, 0f);

        StopParticle(dustParticle);
        StopParticle(lightParticle);
        StopParticle(debrisParticle);
        StopParticle(completeParticle);

        if (riseLoopAudio != null)
        {
            riseLoopAudio.loop = true;
            riseLoopAudio.Stop();
        }
    }

    public void StartRise()
    {
        if (hasRisen)
            return;

        if (riseCoroutine != null)
            StopCoroutine(riseCoroutine);

        riseCoroutine = StartCoroutine(RiseRoutine());
    }

    private IEnumerator RiseRoutine()
    {
        hasRisen = true;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (usePreShake)
            yield return StartCoroutine(PreShakeRoutine());

        PlayParticle(dustParticle);
        PlayParticle(lightParticle);
        PlayParticle(debrisParticle);

        PlayAudio(riseStartAudio);
        PlayAudio(debrisAudio);

        if (riseLoopAudio != null)
            riseLoopAudio.Play();

        float timer = 0f;

        while (timer < riseDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / riseDuration);
            float curveT = riseCurve.Evaluate(t);

            Vector3 basePosition = Vector3.Lerp(startPosition, targetPosition, curveT);

            if (useShakeDuringRise)
            {
                float shakeMultiplier = 1f;

                if (fadeOutShakeNearEnd)
                    shakeMultiplier = 1f - t;

                float shakeX = Mathf.Sin(Time.time * riseShakeSpeed) * riseShakePower * shakeMultiplier;
                float shakeY = Mathf.Cos(Time.time * riseShakeSpeed * 0.8f) * riseShakePower * 0.45f * shakeMultiplier;

                objectToRise.position = basePosition + new Vector3(shakeX, shakeY, 0f);
            }
            else
            {
                objectToRise.position = basePosition;
            }

            yield return null;
        }

        objectToRise.position = targetPosition;

        StopParticle(dustParticle);
        StopParticle(lightParticle);
        StopParticle(debrisParticle);

        if (riseLoopAudio != null)
            riseLoopAudio.Stop();

        PlayParticle(completeParticle);
        PlayAudio(completeAudio);

        riseCoroutine = null;
    }

    private IEnumerator PreShakeRoutine()
    {
        Vector3 originalPosition = objectToRise.position;
        float timer = 0f;

        while (timer < preShakeTime)
        {
            timer += Time.deltaTime;

            float randomX = Random.Range(-preShakePower, preShakePower);
            float randomY = Random.Range(-preShakePower, preShakePower);

            objectToRise.position = originalPosition + new Vector3(randomX, randomY, 0f);

            yield return null;
        }

        objectToRise.position = originalPosition;
    }

    private void PlayParticle(ParticleSystem particle)
    {
        if (particle == null)
            return;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play();
    }

    private void StopParticle(ParticleSystem particle)
    {
        if (particle == null)
            return;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void PlayAudio(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
        audioSource.Play();
    }
}