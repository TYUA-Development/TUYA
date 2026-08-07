using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class CameraMovement : MonoBehaviour
{
    public UnityEngine.Transform shakeCamera;
    public GameObject Charactor;

    [Tooltip("ī�޶� ���󰡴� ���� 0.1 ~ 0.5")]
    public float followSmoothTime = 0.2f;

    public bool isMovingEvent;
    private float cameraSpeedUp = 3.0f;
    private Vector3 velocity;
    [SerializeField] private float CameraPosY;
    private bool followPlayerY = false;

    [Header("Default Zoom")]
    [Tooltip("카메라의 기본(평상시) Field of View. MissionAreaCamera 등 줌을 임시로 바꾸는 스크립트들이 '원래 상태'로 되돌아갈 때 이 값을 기준으로 삼습니다. 0 이하로 두면 씬 시작 시 카메라의 실제 Field of View를 자동으로 캡처해서 씁니다.")]
    public float defaultFieldOfView = 0f;

    public static CameraMovement Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        isMovingEvent = false;

        if(Charactor == null)
        {
            Charactor = FindObjectOfType<PlayerController>().gameObject;
        }
    }

    private void Start()
    {
        CameraPosY = Charactor.transform.position.y + 15.13f;

        if (defaultFieldOfView <= 0f && Camera.main != null)
            defaultFieldOfView = Camera.main.fieldOfView;
    }

    public void SetFollowPlayerY(bool follow)
    {
        followPlayerY = follow;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (isMovingEvent)
        {
            return;
        }

        if (followPlayerY)
            CameraPosY = Charactor.transform.position.y + 15.13f;

        Vector3 targetPos = new Vector3(
            Charactor.transform.position.x,
            CameraPosY,
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            followSmoothTime
        );
    }

    public void SetCameraPosY(float y)
    {
        CameraPosY = y + 15.13f;
    }

    // CameraRig의 실제 Y 좌표를 직접 설정 (오프셋 없음)
    public void SetCameraRigY(float rigY)
    {
        CameraPosY = rigY;
    }

    public void MoveCameraFix(Vector3 targetPos)
    {
        transform.position = new Vector3(
            targetPos.x,
            targetPos.y,
            transform.position.z);
    }

    /// <summary>
    /// ī�޶� ��ǥ�� speed �ӵ��� �̵� �� ����
    /// </summary>
    /// <param name="targetPos"></param>
    /// <param name="speed"></param>
    public void MoveCameraFix(Vector3 targetPos, float speed)
    {
        transform.position = new Vector3(
            targetPos.x,
            targetPos.y,
            transform.position.z);
    }

    /// <summary>
    /// ī�޶� ��ǥ�� �����̵� �� ���ʰ� �ش� ��ġ ����.
    /// </summary>
    public void MoveCamera(Vector3 targetPos, float time)
    {
        StartCoroutine(CoMoveCamera(targetPos, time));
    }

    private IEnumerator CoMoveCamera(Vector3 targetPos, float time)
    {
        isMovingEvent = true;

        Vector3 originPos = transform.position;

        // ��ǥ ��ġ�� ��� �̵�
        transform.position = new Vector3(
            targetPos.x,
            targetPos.y,
            transform.position.z);

        // ���� �ð� ���
        yield return new WaitForSeconds(time);

        transform.position = originPos;

        isMovingEvent = false;
    }

    /// <summary>
    /// ī�޶� ��ǥ�� speed�� �ӵ��� �̵� �� ���ʰ� �ش� ��ġ ���߰� speed �ӵ��� ���ƿ�.
    /// </summary>
    public void MoveCamera(Vector3 targetPos, float time, float speed, bool acceleration = false)
    {
        StartCoroutine(CoMoveCamera(targetPos, time, speed * cameraSpeedUp));
    }

    private IEnumerator CoMoveCamera(Vector3 targetPos, float time, float speed, bool acceleration = false)
    {
        isMovingEvent = true;

        Vector3 originalPos = transform.position;

        // z�� ����
        targetPos.z = transform.position.z;

        float currentSpeed = 0f;

        // ��ǥ ��ġ���� �̵�
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

        // ���� �ð� ���
        yield return new WaitForSeconds(time);

        currentSpeed = 0f;

        // ���� ��ġ�� ����
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
    /// ī�޶� ��ǥ�� waitTime �� ���Ŀ� speed�� �ӵ��� �̵� �� ���ʰ� �ش� ��ġ ���߰� speed �ӵ��� ���ƿ�.
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

        // z�� ����
        targetPos.z = transform.position.z;

        float currentSpeed = 0f;

        // ��ǥ ��ġ���� �̵�
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

        // ���� �ð� ���
        yield return new WaitForSeconds(time);

        currentSpeed = 0f;

        // ���� ��ġ�� ����
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
    /// ī�޶� ������� ��鸰��.
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

            // ���� ��鸲
            if (horizon)
            {
                x = UnityEngine.Random.Range(-power, power);
            }

            // ���� ��鸲
            if (vertical)
            {
                y = UnityEngine.Random.Range(-power, power);
            }

            shakeCamera.localPosition =
                originPos + new Vector3(x, y, 0f);

            yield return null;
        }

        // ���� ��ġ ����
        shakeCamera.localPosition = originPos;
    }
}
