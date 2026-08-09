using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiseObject_Traversal : MonoBehaviour
{
    [System.Serializable]
    public class TraversalPoint
    {
        [Tooltip("이동할 목표 좌표(월드 좌표)")]
        public Vector3 targetPosition;

        [Tooltip("이전 지점(또는 시작 위치)에서 이 지점까지 이동하는데 걸리는 시간(초)")]
        public float moveDuration = 2f;

        [Tooltip("이 지점에 도착한 뒤 다음 지점으로 이동하기 전 대기하는 시간(초)")]
        public float waitTime = 0f;
    }

    [Header("Traversal Target")]
    [Tooltip("오브젝트가 순서대로 이동할 목표 좌표 목록")]
    public List<TraversalPoint> traversalPoints = new List<TraversalPoint>();

    [Tooltip("이동을 시작하기 전 대기하는 시간(초)")]
    public float startDelay = 0f;

    [Tooltip("구간 이동 중 위치 보간에 사용할 커브")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Return")]
    [Tooltip("순회 중일 때 Rise()가 다시 호출되면(코어를 다시 맞추는 등) 순회를 멈추고 현재 위치에서 원래 위치로 돌아올지 여부. 꺼두면 순회 중에는 다시 호출해도 반응하지 않고 계속 순회합니다.")]
    public bool enableReturn = true;

    [Tooltip("복귀 시 원래 위치까지 이동하는데 걸리는 시간(초)")]
    public float returnDuration = 2f;

    [Header("Small Shake Before Rise")]
    [Tooltip("이동 시작 전에 살짝 떨리게 할지")]
    public bool usePreShake = true;

    [Tooltip("떨리는 지속 시간")]
    public float preShakeTime = 0.45f;

    [Tooltip("떨리는 세기")]
    public float preShakePower = 0.025f;

    [Header("Shake During Rise")]
    [Tooltip("이동 중에도 흔들리게 할지. 구간(지점 사이)마다 개별적으로 적용됩니다.")]
    public bool useShakeDuringRise = true;

    [Tooltip("이동 중 흔들림 세기")]
    public float riseShakePower = 0.035f;

    [Tooltip("이동 중 흔들림 속도")]
    public float riseShakeSpeed = 35f;

    [Tooltip("각 구간의 후반부로 갈수록 흔들림이 줄어들게 할지")]
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
    [Tooltip("이동 시작 소리")]
    public AudioSource riseStartAudio;

    [Tooltip("이동 중 반복되는 마찰 소리 / 로프 등. AudioAssist의 Volume Curve로 이동 중 볼륨 변화를 설정할 수 있고, Loop를 켜두어야 전체 이동이 끝날 때까지 계속 반복 재생됩니다.")]
    public AudioAssist riseLoopAudio;

    private enum State
    {
        Idle,
        Patrolling,
        Returning
    }

    private Coroutine activeCoroutine;
    private State state = State.Idle;
    private Vector3 restPosition;

    private void Awake()
    {
        StopParticle(dustParticle);
        StopParticle(lightParticle);
        StopParticle(debrisParticle);
        StopParticle(completeParticle);
    }

    // 코어를 맞출 때마다(등) 호출된다. 정지 상태면 목표 지점들을 순서대로 계속 순회하기 시작하고,
    // 순회 중이면(enableReturn이 켜져 있을 때) 순회를 멈추고 현재 위치에서 원래 위치로 돌아간다.
    // 복귀 중에 호출되면 무시한다.
    public void Rise()
    {
        switch (state)
        {
            case State.Idle:
                if (traversalPoints.Count == 0)
                    return;

                restPosition = transform.position;
                state = State.Patrolling;
                activeCoroutine = StartCoroutine(PatrolRoutine());
                break;

            case State.Patrolling:
                if (!enableReturn)
                    return;

                StopAllCoroutines();

                state = State.Returning;
                activeCoroutine = StartCoroutine(ReturnDownRoutine());
                break;

            case State.Returning:
                break;
        }
    }

    private IEnumerator PatrolRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (usePreShake)
            yield return StartCoroutine(PreShakeRoutine());

        PlayParticle(dustParticle);
        PlayParticle(lightParticle);
        PlayParticle(debrisParticle);

        PlayAudio(riseStartAudio);

        if (riseLoopAudio != null)
            riseLoopAudio.Play();

        int index = 0;

        while (true)
        {
            TraversalPoint point = traversalPoints[index];
            Vector3 from = transform.position;

            yield return StartCoroutine(MoveRoutine(from, point.targetPosition, point.moveDuration));

            PlayParticle(completeParticle);

            if (point.waitTime > 0f)
                yield return new WaitForSeconds(point.waitTime);

            index = (index + 1) % traversalPoints.Count;
        }
    }

    private IEnumerator ReturnDownRoutine()
    {
        Vector3 from = transform.position;

        StopParticle(dustParticle);
        StopParticle(lightParticle);
        StopParticle(debrisParticle);

        if (riseLoopAudio != null)
            riseLoopAudio.Stop();

        PlayParticle(dustParticle);
        PlayParticle(debrisParticle);

        if (riseLoopAudio != null)
            riseLoopAudio.Play();

        yield return StartCoroutine(MoveRoutine(from, restPosition, returnDuration));

        StopParticle(dustParticle);
        StopParticle(debrisParticle);

        if (riseLoopAudio != null)
            riseLoopAudio.Stop();

        state = State.Idle;
        activeCoroutine = null;
    }

    private IEnumerator MoveRoutine(Vector3 from, Vector3 to, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
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
