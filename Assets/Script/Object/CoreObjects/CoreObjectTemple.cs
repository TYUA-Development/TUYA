using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEngine;

[System.Serializable]
public class TemplePiece
{
    public GameObject piece;
    public float targetPosY;
    public float time;
    [Tooltip("1~100")]
    public float slowPower;
    [HideInInspector] public PerlinNoise noise;

    public float delayTime;
}
public class CoreObjectTemple : MonoBehaviour, IArrowHit, ICoreEvent
{
    public List<TemplePiece> pieces;
    public bool activateTemple;

    public int includePlayerIndex = -1;
    public PlayerController controller;

    public void OnCoreEvent()
    {
        activateTemple = true;

        int index = 0;

        foreach (TemplePiece piece in pieces)
        { 
            if(index++ == includePlayerIndex)
            {
                StartCoroutine(RisingTempleAndPlayer(piece));

            }
            else
            {
                StartCoroutine(RisingTemple(piece));
            }
        }
    }

    public void OnHit()
    {
        if(!activateTemple)
        {
            OnCoreEvent();

        }

    }

    // Start is called before the first frame update
    void Start()
    {
        activateTemple = false;

        foreach (TemplePiece piece in pieces)
        {
            piece.noise = piece.piece.GetComponent<PerlinNoise>();
            piece.noise.Play();
        }
    }

    private IEnumerator RisingTemple(TemplePiece piece)
    {
        yield return new WaitForSeconds(piece.delayTime);

        Transform target = piece.piece.transform;

        // 시작 위치
        Vector3 startPos = target.localPosition;

        // 목표 위치 (Y만 변경)
        Vector3 targetPos = new Vector3(
            startPos.x,
            piece.targetPosY,
            startPos.z);

        float elapsed = 0f;

        while (elapsed < piece.time)
        {
            elapsed += Time.deltaTime;

            // 진행도 (0 ~ 1)
            float t = Mathf.Clamp01(elapsed / piece.time);

            // 감속 강도 계산
            float power = Mathf.Lerp(1.5f, 8f, piece.slowPower / 100f);

            // Ease-Out 곡선
            float curvedT = 1f - Mathf.Pow(1f - t, power);

            // localPosition 기준 이동
            target.localPosition = Vector3.Lerp(
                startPos,
                targetPos + piece.noise.LerpNoise(),
                curvedT);

            yield return null;
        }

        // 정확한 위치 보정
        target.localPosition = targetPos;
    }

    private IEnumerator RisingTempleAndPlayer(TemplePiece piece)
    {
        float originGravity = controller.Rigidbody2D.gravityScale;
        controller.Rigidbody2D.gravityScale = 0;

        yield return new WaitForSeconds(piece.delayTime);

        Transform target = piece.piece.transform;

        // 시작 위치
        Vector3 startPos = target.localPosition;

        Vector3 prevWorldPos = target.position;

        // 목표 위치 (Y만 변경)
        Vector3 targetPos = new Vector3(
            startPos.x,
            piece.targetPosY,
            startPos.z);

        float elapsed = 0f;

        while (elapsed < piece.time)
        {
            elapsed += Time.deltaTime;

            // 진행도 (0 ~ 1)
            float t = Mathf.Clamp01(elapsed / piece.time);

            // 감속 강도 계산
            float power = Mathf.Lerp(1.5f, 8f, piece.slowPower / 100f);

            // Ease-Out 곡선
            float curvedT = 1f - Mathf.Pow(1f - t, power);

            // localPosition 기준 이동
            target.localPosition = Vector3.Lerp(
                startPos,
                targetPos + piece.noise.LerpNoise(),
                curvedT);

            // 현재 월드 위치
            Vector3 currentWorldPos = target.position;

            // 이동량 계산
            Vector3 delta = currentWorldPos - prevWorldPos;

            // 플레이어 같이 이동
            controller.transform.position += delta;

            // 현재 위치 저장
            prevWorldPos = currentWorldPos;

            yield return null;
        }

        // 정확한 위치 보정
        target.localPosition = targetPos;
        controller.Rigidbody2D.gravityScale = originGravity;
    }
}
