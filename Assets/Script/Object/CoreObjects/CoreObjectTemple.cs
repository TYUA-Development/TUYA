using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TemplePiece
{
    public GameObject piece;

    [Tooltip("이 조각이 도착할 Y 위치입니다. localPosition.y 기준입니다.")]
    public float targetPosY;

    [Tooltip("이 조각이 올라가는 데 걸리는 시간")]
    public float time;

    [Tooltip("1~100")]
    public float slowPower;

    [HideInInspector] public PerlinNoise noise;

    [Tooltip("전체 신전 상승이 시작된 뒤, 이 조각이 추가로 기다리는 시간")]
    public float delayTime;
}

public class CoreObjectTemple : MonoBehaviour, IArrowHit, ICoreEvent
{
    [Header("Temple Pieces")]
    public List<TemplePiece> pieces;

    [Header("Activation")]
    public bool activateTemple;

    [Tooltip("코어를 맞춘 뒤, 신전 조각들이 올라가기 전까지 기다리는 시간")]
    public float templeStartDelay = 1.0f;

    [Header("Temple Rise Audio")]
    [Tooltip("신전이 움직이기 시작할 때 나는 소리")]
    public AudioSource templeRiseStartAudio;

    [Tooltip("신전이 올라가는 동안 계속 깔리는 낮은 진동음 / 부유음")]
    public AudioSource templeRiseLoopAudio;

    [Tooltip("신전 상승 중 돌가루, 마찰, 부스러기 소리")]
    public AudioSource templeDebrisAudio;

    [Tooltip("신전이 다 올라왔을 때 나는 완료음")]
    public AudioSource templeCompleteAudio;

    [Tooltip("신전 상승 사운드를 사용할지")]
    public bool useTempleRiseAudio = true;

    [Header("Player Ride Settings")]
    public int includePlayerIndex = -1;
    public PlayerController controller;

    [Header("Core Rise")]
    public bool coreRiseUp;
    public float risePosY;
    public float riseSpeed = 2f;

    private Coroutine coreEventCoroutine;
    private Coroutine templeAudioCoroutine;

    public void OnCoreEvent()
    {
        if (activateTemple)
            return;

        activateTemple = true;

        if (coreEventCoroutine != null)
            StopCoroutine(coreEventCoroutine);

        coreEventCoroutine = StartCoroutine(CoreEventRoutine());
    }

    private IEnumerator CoreEventRoutine()
    {
        // 코어 자체 상승은 바로 시작
        if (coreRiseUp)
            StartCoroutine(RisingCore());

        // 코어 맞은 뒤 신전이 바로 움직이지 않도록 전체 딜레이
        if (templeStartDelay > 0f)
            yield return new WaitForSeconds(templeStartDelay);

        // 신전 상승 사운드 시작
        if (useTempleRiseAudio)
        {
            float totalRiseDuration = GetTempleTotalRiseDuration();

            if (templeAudioCoroutine != null)
                StopCoroutine(templeAudioCoroutine);

            templeAudioCoroutine = StartCoroutine(TempleRiseAudioRoutine(totalRiseDuration));
        }

        int index = 0;

        foreach (TemplePiece piece in pieces)
        {
            if (piece == null || piece.piece == null)
            {
                index++;
                continue;
            }

            if (index == includePlayerIndex)
            {
                StartCoroutine(RisingTempleAndPlayer(piece));
            }
            else
            {
                StartCoroutine(RisingTemple(piece));
            }

            index++;
        }

        coreEventCoroutine = null;
    }

    public void OnHit()
    {
        if (!activateTemple)
        {
            OnCoreEvent();
        }
    }

    void Start()
    {
        activateTemple = false;

        if (templeRiseLoopAudio != null)
        {
            templeRiseLoopAudio.loop = true;
            templeRiseLoopAudio.Stop();
        }

        if (templeDebrisAudio != null)
        {
            templeDebrisAudio.loop = true;
            templeDebrisAudio.Stop();
        }

        foreach (TemplePiece piece in pieces)
        {
            if (piece == null || piece.piece == null)
                continue;

            piece.noise = piece.piece.GetComponent<PerlinNoise>();

            if (piece.noise != null)
                piece.noise.Play();
        }
    }

    private float GetTempleTotalRiseDuration()
    {
        float maxDuration = 0f;

        foreach (TemplePiece piece in pieces)
        {
            if (piece == null || piece.piece == null)
                continue;

            float duration = piece.delayTime + piece.time;

            if (duration > maxDuration)
                maxDuration = duration;
        }

        return maxDuration;
    }

    private IEnumerator TempleRiseAudioRoutine(float totalRiseDuration)
    {
        PlayAudio(templeRiseStartAudio);
        PlayAudio(templeRiseLoopAudio);
        PlayAudio(templeDebrisAudio);

        if (totalRiseDuration > 0f)
            yield return new WaitForSeconds(totalRiseDuration);

        StopAudio(templeRiseLoopAudio);
        StopAudio(templeDebrisAudio);

        PlayAudio(templeCompleteAudio);

        templeAudioCoroutine = null;
    }

    private IEnumerator RisingCore()
    {
        Vector3 targetPos = new Vector3(
            transform.position.x,
            risePosY,
            transform.position.z
        );

        while (Mathf.Abs(transform.position.y - risePosY) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                riseSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPos;
    }

    private IEnumerator RisingTemple(TemplePiece piece)
    {
        yield return new WaitForSeconds(piece.delayTime);

        Transform target = piece.piece.transform;

        Vector3 startPos = target.localPosition;

        Vector3 targetPos = new Vector3(
            startPos.x,
            piece.targetPosY,
            startPos.z
        );

        float elapsed = 0f;

        while (elapsed < piece.time)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / piece.time);

            float power = Mathf.Lerp(1.5f, 8f, piece.slowPower / 100f);

            float curvedT = 1f - Mathf.Pow(1f - t, power);

            Vector3 noiseOffset = Vector3.zero;

            if (piece.noise != null)
                noiseOffset = piece.noise.LerpNoise();

            target.localPosition = Vector3.Lerp(
                startPos,
                targetPos + noiseOffset,
                curvedT
            );

            yield return null;
        }

        target.localPosition = targetPos;
    }

    private IEnumerator RisingTempleAndPlayer(TemplePiece piece)
    {
        if (controller == null || controller.Rigidbody2D == null)
        {
            StartCoroutine(RisingTemple(piece));
            yield break;
        }

        float originGravity = controller.Rigidbody2D.gravityScale;
        controller.Rigidbody2D.gravityScale = 0f;

        if (CameraMovement.Instance != null)
            CameraMovement.Instance.SetFollowPlayerY(true);

        yield return new WaitForSeconds(piece.delayTime);

        Transform target = piece.piece.transform;

        Vector3 startPos = target.localPosition;
        Vector3 prevWorldPos = target.position;

        Vector3 targetPos = new Vector3(
            startPos.x,
            piece.targetPosY,
            startPos.z
        );

        float elapsed = 0f;

        while (elapsed < piece.time)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / piece.time);

            float power = Mathf.Lerp(1.5f, 8f, piece.slowPower / 100f);

            float curvedT = 1f - Mathf.Pow(1f - t, power);

            Vector3 noiseOffset = Vector3.zero;

            if (piece.noise != null)
                noiseOffset = piece.noise.LerpNoise();

            target.localPosition = Vector3.Lerp(
                startPos,
                targetPos + noiseOffset,
                curvedT
            );

            Vector3 currentWorldPos = target.position;
            Vector3 delta = currentWorldPos - prevWorldPos;

            controller.transform.position += delta;

            prevWorldPos = currentWorldPos;

            yield return null;
        }

        target.localPosition = targetPos;

        controller.Rigidbody2D.gravityScale = originGravity;

        if (CameraMovement.Instance != null)
            CameraMovement.Instance.SetFollowPlayerY(false);
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