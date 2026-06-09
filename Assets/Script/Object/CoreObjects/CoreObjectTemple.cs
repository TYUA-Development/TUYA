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

    public bool coreRiseUp;
    public float risePosY;
    public float riseSpeed;

    public void OnCoreEvent()
    {
        activateTemple = true;

        int index = 0;

        if (coreRiseUp)
            StartCoroutine(RisingCore());

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

    private IEnumerator RisingCore()
    {
        Vector3 targetPos = new Vector3(
        transform.position.x,
        risePosY,
        transform.position.z);

        while (Mathf.Abs(transform.position.y - risePosY) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                riseSpeed * Time.deltaTime);

            yield return null;
        }

        // ��Ȯ�� ��ġ ����
        transform.position = targetPos;
    }

    private IEnumerator RisingTemple(TemplePiece piece)
    {
        yield return new WaitForSeconds(piece.delayTime);

        Transform target = piece.piece.transform;

        // ���� ��ġ
        Vector3 startPos = target.localPosition;

        // ��ǥ ��ġ (Y�� ����)
        Vector3 targetPos = new Vector3(
            startPos.x,
            piece.targetPosY,
            startPos.z);

        float elapsed = 0f;

        while (elapsed < piece.time)
        {
            elapsed += Time.deltaTime;

            // ���൵ (0 ~ 1)
            float t = Mathf.Clamp01(elapsed / piece.time);

            // ���� ���� ���
            float power = Mathf.Lerp(1.5f, 8f, piece.slowPower / 100f);

            // Ease-Out �
            float curvedT = 1f - Mathf.Pow(1f - t, power);

            // localPosition ���� �̵�
            target.localPosition = Vector3.Lerp(
                startPos,
                targetPos + piece.noise.LerpNoise(),
                curvedT);

            yield return null;
        }

        // ��Ȯ�� ��ġ ����
        target.localPosition = targetPos;
    }

    private IEnumerator RisingTempleAndPlayer(TemplePiece piece)
    {
        float originGravity = controller.Rigidbody2D.gravityScale;
        controller.Rigidbody2D.gravityScale = 0;

        CameraMovement.Instance.SetFollowPlayerY(true);

        yield return new WaitForSeconds(piece.delayTime);

        Transform target = piece.piece.transform;

        // ���� ��ġ
        Vector3 startPos = target.localPosition;

        Vector3 prevWorldPos = target.position;

        // ��ǥ ��ġ (Y�� ����)
        Vector3 targetPos = new Vector3(
            startPos.x,
            piece.targetPosY,
            startPos.z);

        float elapsed = 0f;

        while (elapsed < piece.time)
        {
            elapsed += Time.deltaTime;

            // ���൵ (0 ~ 1)
            float t = Mathf.Clamp01(elapsed / piece.time);

            // ���� ���� ���
            float power = Mathf.Lerp(1.5f, 8f, piece.slowPower / 100f);

            // Ease-Out �
            float curvedT = 1f - Mathf.Pow(1f - t, power);

            // localPosition ���� �̵�
            target.localPosition = Vector3.Lerp(
                startPos,
                targetPos + piece.noise.LerpNoise(),
                curvedT);

            // ���� ���� ��ġ
            Vector3 currentWorldPos = target.position;

            // �̵��� ���
            Vector3 delta = currentWorldPos - prevWorldPos;

            // �÷��̾� ���� �̵�
            controller.transform.position += delta;

            // ���� ��ġ ����
            prevWorldPos = currentWorldPos;

            yield return null;
        }

        // ��Ȯ�� ��ġ ����
        target.localPosition = targetPos;
        controller.Rigidbody2D.gravityScale = originGravity;
        CameraMovement.Instance.SetFollowPlayerY(false);
    }
}
