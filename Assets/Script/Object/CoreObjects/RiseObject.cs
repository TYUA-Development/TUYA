using System.Collections;
using UnityEngine;

public class RiseObject : MonoBehaviour
{
    [Header("Rise Target")]
    [Tooltip("오브젝트가 올라갈 목표 좌표(월드 좌표)")]
    public Vector3 targetPosition;

    [Tooltip("목표 좌표까지 올라가는데 걸리는 시간(초)")]
    public float riseDuration = 2f;

    [Tooltip("올라가기 전 대기하는 시간(초)")]
    public float startDelay = 0f;

    [Tooltip("상승 중 위치 보간에 사용할 커브")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Return")]
    [Tooltip("목표 좌표에 도달한 뒤 다시 원래 자리로 돌아올지 여부")]
    public bool enableReturn = false;

    [Tooltip("되돌리기가 켜져있을 때, 목표 좌표에 머무르는 시간(초)")]
    public float holdDuration = 2f;

    [Header("Small Shake Before Rise")]
    [Tooltip("상승 시작 전에 살짝 떨리게 할지")]
    public bool usePreShake = true;

    [Tooltip("떨리는 지속 시간")]
    public float preShakeTime = 0.45f;

    [Tooltip("떨리는 세기")]
    public float preShakePower = 0.025f;

    [Header("Shake During Rise")]
    [Tooltip("상승 중에도 흔들리게 할지")]
    public bool useShakeDuringRise = true;

    [Tooltip("상승 중 흔들림 세기")]
    public float riseShakePower = 0.035f;

    [Tooltip("상승 중 흔들림 속도")]
    public float riseShakeSpeed = 35f;

    [Tooltip("상승 후반부로 갈수록 흔들림이 줄어들게 할지")]
    public bool fadeOutShakeNearEnd = true;

    [Header("Particles")]
    [Tooltip("바닥 먼지 파티클")]
    public ParticleSystem dustParticle;

    [Tooltip("빛 번쩍 파티클")]
    public ParticleSystem lightParticle;

    [Tooltip("돌 부스러기 파티클")]
    public ParticleSystem debrisParticle;

    [Tooltip("완료 파티클")]
    public ParticleSystem completeParticle;

    [Header("Audio")]
    [Tooltip("상승 시작 소리")]
    public AudioSource riseStartAudio;

    [Tooltip("상승 중 반복되는 마찰 소리 / 로프 등")]
    public AudioSource riseLoopAudio;

    [Tooltip("돌 부스러기 소리")]
    public AudioSource debrisAudio;

    [Tooltip("상승 완료 소리")]
    public AudioSource completeAudio;

    private Coroutine riseCoroutine;
    private bool hasRisen;

    private void Awake()
    {
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

    public void Rise()
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

        Vector3 startPosition = transform.position;

        PlayParticle(dustParticle);
        PlayParticle(lightParticle);
        PlayParticle(debrisParticle);

        PlayAudio(riseStartAudio);
        PlayAudio(debrisAudio);

        if (riseLoopAudio != null)
            riseLoopAudio.Play();

        yield return StartCoroutine(MoveRoutine(startPosition, targetPosition));

        StopParticle(dustParticle);
        StopParticle(lightParticle);
        StopParticle(debrisParticle);

        if (riseLoopAudio != null)
            riseLoopAudio.Stop();

        PlayParticle(completeParticle);
        PlayAudio(completeAudio);

        if (enableReturn)
        {
            if (holdDuration > 0f)
                yield return new WaitForSeconds(holdDuration);

            PlayParticle(dustParticle);
            PlayParticle(debrisParticle);
            PlayAudio(debrisAudio);

            if (riseLoopAudio != null)
                riseLoopAudio.Play();

            yield return StartCoroutine(MoveRoutine(targetPosition, startPosition));

            StopParticle(dustParticle);
            StopParticle(debrisParticle);

            if (riseLoopAudio != null)
                riseLoopAudio.Stop();

            hasRisen = false;
        }

        riseCoroutine = null;
    }

    private IEnumerator MoveRoutine(Vector3 from, Vector3 to)
    {
        float timer = 0f;

        while (timer < riseDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / riseDuration);
            float curveT = riseCurve.Evaluate(t);

            Vector3 basePosition = Vector3.Lerp(from, to, curveT);

            if (useShakeDuringRise)
            {
                float shakeMultiplier = fadeOutShakeNearEnd ? 1f - t : 1f;

                float shakeX = Mathf.Sin(Time.time * riseShakeSpeed) * riseShakePower * shakeMultiplier;
                float shakeY = Mathf.Cos(Time.time * riseShakeSpeed * 0.8f) * riseShakePower * 0.45f * shakeMultiplier;

                transform.position = basePosition + new Vector3(shakeX, shakeY, 0f);
            }
            else
            {
                transform.position = basePosition;
            }

            yield return null;
        }

        transform.position = to;
    }

    private IEnumerator PreShakeRoutine()
    {
        Vector3 originalPosition = transform.position;
        float timer = 0f;

        while (timer < preShakeTime)
        {
            timer += Time.deltaTime;

            float randomX = Random.Range(-preShakePower, preShakePower);
            float randomY = Random.Range(-preShakePower, preShakePower);

            transform.position = originalPosition + new Vector3(randomX, randomY, 0f);

            yield return null;
        }

        transform.position = originalPosition;
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
