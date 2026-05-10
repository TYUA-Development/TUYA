using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class CameraMovement : MonoBehaviour
{
    public GameObject Charactor;
    public float cameraHeight;
    private float timer;
    private bool isMovingEvent;
    private float cameraSpeedUp = 3.0f;

    public static CameraMovement Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        isMovingEvent = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isMovingEvent)
        {
            return;
        }
        Vector3 pos = new Vector3(Charactor.transform.position.x, cameraHeight, transform.position.z);
        transform.position = pos;
    }

    /// <summary>
    /// 카메라가 목표로 순간이동 후 몇초간 해당 위치 비춤.
    /// </summary>
    public void MoveCamera(Vector3 targetPos, float time)
    {
        StartCoroutine(CoMoveCamera(targetPos, time));
    }

    private IEnumerator CoMoveCamera(Vector3 targetPos, float time)
    {
        isMovingEvent = true;

        Vector3 originPos = transform.position;

        // 목표 위치로 즉시 이동
        transform.position = new Vector3(
            targetPos.x,
            targetPos.y,
            transform.position.z);

        // 지정 시간 대기
        yield return new WaitForSeconds(time);

        transform.position = originPos;

        isMovingEvent = false;
    }

    /// <summary>
    /// 카메라가 목표로 speed의 속도로 이동 후 몇초간 해당 위치 비추고 speed 속도로 돌아옴.
    /// </summary>
    public void MoveCamera(Vector3 targetPos, float time, float speed)
    {
        StartCoroutine(CoMoveCamera(targetPos, time, speed * cameraSpeedUp));
    }

    private IEnumerator CoMoveCamera(Vector3 targetPos, float time, float speed)
    {
        isMovingEvent = true;

        Vector3 originalPos = transform.position;

        // z축 유지
        targetPos.z = transform.position.z;

        // 목표 위치까지 이동
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                speed * Time.deltaTime);

            yield return null;
        }

        // 지정 시간 대기
        yield return new WaitForSeconds(time);

        // 원래 위치로 복귀
        while (Vector3.Distance(transform.position, originalPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                originalPos,
                speed * Time.deltaTime);

            yield return null;
        }

        isMovingEvent = false;
    }
}
