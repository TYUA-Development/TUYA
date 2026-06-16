using System.Collections;
using UnityEngine;

public class TimedRisingObjectController : MonoBehaviour
{
    [Header("Move Target")]
    [Tooltip("비워두면 이 오브젝트가 직접 움직입니다.")]
    public Transform objectToMove;

    [Header("Position")]
    [Tooltip("시작할 때 현재 위치를 내려간 위치로 저장합니다.")]
    public bool useCurrentPositionAsDownPosition = true;

    [Tooltip("내려간 위치. 월드 좌표 기준입니다.")]
    public Vector3 downPosition;

    [Tooltip("현재 위치에서 위로 올라갈 높이")]
    public float riseHeight = 3f;

    [Tooltip("직접 위 위치를 지정하고 싶으면 체크")]
    public bool useCustomUpPosition = false;

    [Tooltip("직접 지정하는 위 위치. 월드 좌표 기준입니다.")]
    public Vector3 customUpPosition;

    [Header("Rise Timing")]
    [Tooltip("코어가 작동한 뒤 이 오브젝트가 올라가기 전 대기")]
    public float startDelay = 0f;

    [Tooltip("올라가는 시간")]
    public float riseDuration = 3.5f;

    [Tooltip("올라간 상태로 유지하는 시간")]
    public float holdTime = 2.5f;

    [Tooltip("내려오기 전 추가 대기")]
    public float lowerDelay = 0f;

    [Tooltip("내려오는 시간")]
    public float lowerDuration = 2.5f;

    [Header("Curves")]
    [Tooltip("올라갈 때 움직임 곡선")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("내려올 때 움직임 곡선")]
    public AnimationCurve lowerCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Collider Control")]
    [Tooltip("올라가는 동안 콜라이더를 꺼서 플레이어가 같이 밀려 올라가지 않게 합니다.")]
    public bool disableCollidersWhileRising = true;

    [Tooltip("상승 전 떨림 중에도 콜라이더를 끕니다.")]
    public bool disableCollidersDuringPreShake = true;

    [Tooltip("내려가는 동안에도 콜라이더를 끌지 여부. 보통은 꺼두는 걸 추천합니다.")]
    public bool disableCollidersWhileLowering = false;

    [Tooltip("비워두면 이 오브젝트와 자식의 Collider2D를 자동으로 찾습니다.")]
    public Collider2D[] collidersToControl;

    [Header("Small Shake Before Rise")]
    [Tooltip("상승 직전에 아주 약하게 떨림")]
    public bool usePreShake = true;

    [Tooltip("상승 전 떨림 시간")]
    public float preShakeTime = 0.45f;

    [Tooltip("상승 전 떨림 강도")]
    public float preShakePower = 0.025f;

    [Header("Shake During Move")]
    [Tooltip("상승 중에도 계속 약하게 떨림")]
    public bool useShakeDuringRise = true;

    [Tooltip("상승 중 떨림 강도")]
    public float riseShakePower = 0.025f;

    [Tooltip("상승 중 떨림 속도")]
    public float riseShakeSpeed = 28f;

    [Tooltip("상승 후반부로 갈수록 떨림이 줄어듭니다.")]
    public bool fadeOutShakeNearEnd = true;

    [Tooltip("내려올 때도 떨림 사용")]
    public bool useShakeDuringLower = false;

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

    [Tooltip("상승/하강 중 반복되는 낮은 부유음 / 진동음")]
    public AudioSource moveLoopAudio;

    [Tooltip("돌 부스러기 / 마찰음")]
    public AudioSource debrisAudio;

    [Tooltip("상승 완료 소리")]
    public AudioSource riseCompleteAudio;

    [Tooltip("하강 시작 소리")]
    public AudioSource lowerStartAudio;

    [Tooltip("하강 완료 소리")]
    public AudioSource lowerCompleteAudio;

    [Header("State")]
    public bool isRaised;
    public bool isMoving;

    private Coroutine moveCoroutine;
    private Vector3 upPosition;

    private void Awake()
    {
        if (objectToMove == null)
            objectToMove = transform;

        if (collidersToControl == null || collidersToControl.Length == 0)
            collidersToControl = GetComponentsInChildren<Collider2D>();

        StopParticle(dustParticle);
        StopParticle(lightParticle);
        StopParticle(debrisParticle);
        StopParticle(completeParticle);

        if (moveLoopAudio != null)
        {
            moveLoopAudio.loop = true;
            moveLoopAudio.Stop();
        }

        if (debrisAudio != null)
        {
            debrisAudio.loop = true;
            debrisAudio.Stop();
        }
    }

    private void Start()
    {
        if (objectToMove == null)
            objectToMove = transform;

        if (useCurrentPositionAsDownPosition)
        {
            downPosition = objectToMove.position;
        }
        else
        {
            objectToMove.position = downPosition;
        }

        if (useCustomUpPosition)
        {
            upPosition = customUpPosition;
        }
        else
        {
            upPosition = downPosition + new Vector3(0f, riseHeight, 0f);
        }

        SetCollidersEnabled(true);

        isRaised = false;
        isMoving = false;
    }

    public void TriggerRiseAndLower()
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(RiseAndLowerRoutine());
    }

    public void ForceToDownPosition()
    {
        if (objectToMove == null)
            objectToMove = transform;

        objectToMove.position = downPosition;
        SetCollidersEnabled(true);

        isRaised = false;
        isMoving = false;
    }

    public void ForceToUpPosition()
    {
        if (objectToMove == null)
            objectToMove = transform;

        objectToMove.position = upPosition;
        SetCollidersEnabled(true);

        isRaised = true;
        isMoving = false;
    }

    private IEnumerator RiseAndLowerRoutine()
    {
        isMoving = true;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (disableCollidersWhileRising && disableCollidersDuringPreShake)
            SetCollidersEnabled(false);

        if (usePreShake)
            yield return StartCoroutine(PreShakeRoutine());

        if (disableCollidersWhileRising)
            SetCollidersEnabled(false);

        PlayParticle(dustParticle);
        PlayParticle(lightParticle);
        PlayParticle(debrisParticle);

        PlayAudio(riseStartAudio);
        PlayAudio(moveLoopAudio);
        PlayAudio(debrisAudio);

        yield return StartCoroutine(MoveRoutine(
            objectToMove.position,
            upPosition,
            riseDuration,
            riseCurve,
            useShakeDuringRise
        ));

        objectToMove.position = upPosition;

        StopAudio(moveLoopAudio);
        StopAudio(debrisAudio);

        StopParticle(dustParticle);
        StopParticle(lightParticle);
        StopParticle(debrisParticle);

        PlayParticle(completeParticle);
        PlayAudio(riseCompleteAudio);

        // 올라온 뒤에는 다시 밟을 수 있어야 하니까 콜라이더 ON
        SetCollidersEnabled(true);

        isRaised = true;

        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);

        if (lowerDelay > 0f)
            yield return new WaitForSeconds(lowerDelay);

        if (disableCollidersWhileLowering)
            SetCollidersEnabled(false);
        else
            SetCollidersEnabled(true);

        PlayParticle(dustParticle);
        PlayParticle(debrisParticle);

        PlayAudio(lowerStartAudio);
        PlayAudio(moveLoopAudio);
        PlayAudio(debrisAudio);

        yield return StartCoroutine(MoveRoutine(
            objectToMove.position,
            downPosition,
            lowerDuration,
            lowerCurve,
            useShakeDuringLower
        ));

        objectToMove.position = downPosition;

        StopAudio(moveLoopAudio);
        StopAudio(debrisAudio);

        StopParticle(dustParticle);
        StopParticle(lightParticle);
        StopParticle(debrisParticle);

        PlayAudio(lowerCompleteAudio);

        SetCollidersEnabled(true);

        isRaised = false;
        isMoving = false;
        moveCoroutine = null;
    }

    private IEnumerator MoveRoutine(Vector3 from, Vector3 to, float duration, AnimationCurve curve, bool useShake)
    {
        if (duration <= 0f)
        {
            objectToMove.position = to;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float curveT = curve.Evaluate(t);

            Vector3 basePosition = Vector3.Lerp(from, to, curveT);

            if (useShake)
            {
                float shakeMultiplier = 1f;

                if (fadeOutShakeNearEnd)
                    shakeMultiplier = 1f - t;

                float shakeX = Mathf.Sin(Time.time * riseShakeSpeed) * riseShakePower * shakeMultiplier;
                float shakeY = Mathf.Cos(Time.time * riseShakeSpeed * 0.8f) * riseShakePower * 0.45f * shakeMultiplier;

                objectToMove.position = basePosition + new Vector3(shakeX, shakeY, 0f);
            }
            else
            {
                objectToMove.position = basePosition;
            }

            yield return null;
        }

        objectToMove.position = to;
    }

    private IEnumerator PreShakeRoutine()
    {
        Vector3 originalPosition = objectToMove.position;
        float timer = 0f;

        while (timer < preShakeTime)
        {
            timer += Time.deltaTime;

            float randomX = Random.Range(-preShakePower, preShakePower);
            float randomY = Random.Range(-preShakePower, preShakePower);

            objectToMove.position = originalPosition + new Vector3(randomX, randomY, 0f);

            yield return null;
        }

        objectToMove.position = originalPosition;
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (collidersToControl == null)
            return;

        for (int i = 0; i < collidersToControl.Length; i++)
        {
            if (collidersToControl[i] == null)
                continue;

            collidersToControl[i].enabled = enabled;
        }
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

    private void StopAudio(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
    }
}