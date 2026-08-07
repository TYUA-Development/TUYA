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
    [Tooltip("이미 올라와 있는 상태에서 Rise()가 다시 호출되면(코어를 다시 맞추는 등) 원래 위치로 돌아올지 여부. 꺼두면 한 번 올라온 뒤로는 다시 호출해도 반응하지 않습니다.")]
    public bool enableReturn = true;

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

    [Tooltip("상승 중 반복되는 마찰 소리 / 로프 등. AudioAssist의 Volume Curve로 상승 중 볼륨 변화를 설정할 수 있고, Loop를 켜두어야 상승이 끝날 때까지 계속 반복 재생됩니다.")]
    public AudioAssist riseLoopAudio;

    private Coroutine moveCoroutine;
    private bool isMoving;
    private bool isUp;
    private Vector3 restPosition;

    private void Awake()
    {
        restPosition = transform.position;

        StopParticle(dustParticle);
        StopParticle(lightParticle);
        StopParticle(debrisParticle);
        StopParticle(completeParticle);
    }

    // 코어를 맞출 때마다(등) 호출된다. 아래에 있으면 올라오고, 이미 올라와 있으면(enableReturn이
    // 켜져 있을 때) 원래 위치로 돌아간다 - 더 이상 일정 시간 뒤 자동으로 돌아오지 않고,
    // 다시 호출되어야만 반응한다. 이동 중에 호출되면 무시한다.
    public void Rise()
    {
        if (isMoving)
            return;

        if (isUp)
        {
            if (!enableReturn)
                return;

            moveCoroutine = StartCoroutine(ReturnDownRoutine());
        }
        else
        {
            moveCoroutine = StartCoroutine(RiseUpRoutine());
        }
    }

    private IEnumerator RiseUpRoutine()
    {
        isMoving = true;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (usePreShake)
            yield return StartCoroutine(PreShakeRoutine());

        Vector3 from = transform.position;

        PlayParticle(dustParticle);
        PlayParticle(lightParticle);
        PlayParticle(debrisParticle);

        PlayAudio(riseStartAudio);

        if (riseLoopAudio != null)
            riseLoopAudio.Play();

        yield return StartCoroutine(MoveRoutine(from, targetPosition));

        StopParticle(dustParticle);
        StopParticle(lightParticle);
        StopParticle(debrisParticle);

        if (riseLoopAudio != null)
            riseLoopAudio.Stop();

        PlayParticle(completeParticle);

        isUp = true;
        isMoving = false;
        moveCoroutine = null;
    }

    private IEnumerator ReturnDownRoutine()
    {
        isMoving = true;

        Vector3 from = transform.position;

        PlayParticle(dustParticle);
        PlayParticle(debrisParticle);

        if (riseLoopAudio != null)
            riseLoopAudio.Play();

        yield return StartCoroutine(MoveRoutine(from, restPosition));

        StopParticle(dustParticle);
        StopParticle(debrisParticle);

        if (riseLoopAudio != null)
            riseLoopAudio.Stop();

        isUp = false;
        isMoving = false;
        moveCoroutine = null;
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
