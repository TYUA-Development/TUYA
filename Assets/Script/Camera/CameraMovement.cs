using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class CameraMovement : MonoBehaviour
{
    public UnityEngine.Transform shakeCamera;
    public GameObject Charactor;
    public float cameraHeight;
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
        Vector3 pos = new Vector3(Charactor.transform.position.x, Charactor.transform.position.y+15.13f, transform.position.z);
        transform.position = pos;
    }

    public void SetCameraHeight(float height)
    {
        cameraHeight = height;
    }


    public void MoveCamera(Vector3 targetPos)
    {
        transform.position = new Vector3(
            targetPos.x,
            targetPos.y,
            transform.position.z);
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
    public void MoveCamera(Vector3 targetPos, float time, float speed, bool acceleration = false)
    {
        StartCoroutine(CoMoveCamera(targetPos, time, speed * cameraSpeedUp));
    }

    private IEnumerator CoMoveCamera(Vector3 targetPos, float time, float speed, bool acceleration = false)
    {
        isMovingEvent = true;

        Vector3 originalPos = transform.position;

        // z축 유지
        targetPos.z = transform.position.z;

        float currentSpeed = 0f;

        // 목표 위치까지 이동
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            if (acceleration)
            {
                currentSpeed += speed * Time.deltaTime;
            }
            else
            {
                currentSpeed = speed;
            }
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                currentSpeed * Time.deltaTime);

            yield return null;
        }

        // 지정 시간 대기
        yield return new WaitForSeconds(time);

        currentSpeed = 0f;

        // 원래 위치로 복귀
        while (Vector3.Distance(transform.position, originalPos) > 0.01f)
        {
            if (acceleration)
            {
                currentSpeed += speed * Time.deltaTime;
            }
            else
            {
                currentSpeed = speed;
            }
            transform.position = Vector3.MoveTowards(
                transform.position,
                originalPos,
                currentSpeed * Time.deltaTime);

            yield return null;
        }

        isMovingEvent = false;
    }

    /// <summary>
    /// 카메라가 목표로 waitTime 초 이후에 speed의 속도로 이동 후 몇초간 해당 위치 비추고 speed 속도로 돌아옴.
    /// </summary>
    public void MoveCamera(Vector3 targetPos, float time, float waitTime, float speed, bool acceleration = false)
    {
        StartCoroutine(CoMoveCamera(targetPos, time, waitTime, speed * cameraSpeedUp));
    }

    private IEnumerator CoMoveCamera(Vector3 targetPos, float time, float waitTime, float speed, bool acceleration = false)
    {
        yield return new WaitForSeconds(waitTime);

        isMovingEvent = true;

        Vector3 originalPos = transform.position;

        // z축 유지
        targetPos.z = transform.position.z;

        float currentSpeed = 0f;

        // 목표 위치까지 이동
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            if (acceleration)
            {
                currentSpeed += speed * Time.deltaTime;
            }
            else
            {
                currentSpeed = speed;
            }
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                currentSpeed * Time.deltaTime);

            yield return null;
        }

        // 지정 시간 대기
        yield return new WaitForSeconds(time);

        currentSpeed = 0f;

        // 원래 위치로 복귀
        while (Vector3.Distance(transform.position, originalPos) > 0.01f)
        {
            if (acceleration)
            {
                currentSpeed += speed * Time.deltaTime;
            }
            else
            {
                currentSpeed = speed;
            }
            transform.position = Vector3.MoveTowards(
                transform.position,
                originalPos,
                currentSpeed * Time.deltaTime);

            yield return null;
        }

        isMovingEvent = false;
    }

    /// <summary>
    /// 카메라가 노이즈로 흔들린다.
    /// </summary>
    public void MoveCameraNoise(float power, float time, bool vertical = false, bool horizon = false)
    {
        StartCoroutine(CoMoveCameraNoise(power, time, vertical, horizon));
    }

    private IEnumerator CoMoveCameraNoise(float power, float time, bool vertical, bool horizon)
    {
        Vector3 originPos = shakeCamera.localPosition;

        float currentTime = 0f;

        while (currentTime < time)
        {
            currentTime += Time.deltaTime;

            float x = 0f;
            float y = 0f;

            // 가로 흔들림
            if (horizon)
            {
                x = UnityEngine.Random.Range(-power, power);
            }

            // 세로 흔들림
            if (vertical)
            {
                y = UnityEngine.Random.Range(-power, power);
            }

            shakeCamera.localPosition =
                originPos + new Vector3(x, y, 0f);

            yield return null;
        }

        // 원래 위치 복귀
        shakeCamera.localPosition = originPos;
    }
}
