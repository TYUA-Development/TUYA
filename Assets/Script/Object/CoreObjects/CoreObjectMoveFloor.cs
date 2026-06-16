using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CoreObjectMoveFloorInfo
{
    public GameObject Floor;
    public Vector3 nextPos;
    [HideInInspector] public Vector3 prevPos;
}

[System.Serializable]
public struct PropellerInfo
{
    public GameObject propeller;
    public float rotateSpeed;
    [HideInInspector] public float currentSpeed;
}

[System.Serializable]
public struct WindObjectInfo
{
    public Object_Wind windObject;
    public Vector3 targetPos;
    [HideInInspector] public Vector3 prevPos;
}

public class CoreObjectMoveFloor : MonoBehaviour, ICoreEvent, IArrowHit
{
    [Header("Core Objects")]
    [Tooltip("어떤 코어를 맞춰도 동일하게 동작합니다.")]
    public List<CoreActivationController> coreObjects;

    public List<CoreObjectMoveFloorInfo> floors;
    public float moveSpeed;

    [Header("Wind Objects")]
    public List<WindObjectInfo> winds;
    [Tooltip("Wind가 targetPos까지 이동하는 데 걸리는 시간(초)")]
    public float windObjectSpeed = 1.0f;

    [Header("Propellers")]
    public List<PropellerInfo> propellers;
    [Tooltip("0에서 최대 속도까지 가속/감속에 걸리는 시간(초)")]
    public float propellerRampTime = 1.0f;

    private int isMoving;
    private bool propellersSpinning = false;
    private Coroutine propellerRampCoroutine;

    void Start()
    {
        isMoving = floors.Count;

        foreach (var core in coreObjects)
        {
            if (core != null)
                core.onActivated += OnCoreEvent;
        }

        for (int i = 0; i < winds.Count; i++)
        {
            WindObjectInfo info = winds[i];
            if (info.windObject != null)
                info.prevPos = info.windObject.transform.localPosition;
            winds[i] = info;
        }

        for (int i = 0; i < propellers.Count; i++)
        {
            PropellerInfo info = propellers[i];
            info.currentSpeed = 0f;
            propellers[i] = info;
        }
    }

    void OnDestroy()
    {
        foreach (var core in coreObjects)
        {
            if (core != null)
                core.onActivated -= OnCoreEvent;
        }
    }

    void Update()
    {
        for (int i = 0; i < propellers.Count; i++)
        {
            if (propellers[i].propeller != null && propellers[i].currentSpeed != 0f)
                propellers[i].propeller.transform.Rotate(0f, 0f, propellers[i].currentSpeed * Time.deltaTime);
        }
    }

    public void OnCoreEvent()
    {
        if (isMoving != floors.Count) return;

        isMoving = 0;

        for (int i = 0; i < winds.Count; i++)
            StartCoroutine(MoveWind(i));

        propellersSpinning = !propellersSpinning;

        if (propellerRampCoroutine != null)
            StopCoroutine(propellerRampCoroutine);

        propellerRampCoroutine = StartCoroutine(RampPropellers(propellersSpinning));

        for (int i = 0; i < floors.Count; i++)
        {
            CoreObjectMoveFloorInfo floorInfo = floors[i];
            floorInfo.prevPos = floorInfo.Floor.transform.localPosition;
            floors[i] = floorInfo;
            StartCoroutine(MoveFloor(i));
        }
    }

    public void OnHit()
    {
        OnCoreEvent();
    }

    private IEnumerator MoveFloor(int index)
    {
        CoreObjectMoveFloorInfo info = floors[index];
        Transform floor = info.Floor.transform;

        while (Vector3.Distance(floor.localPosition, info.nextPos) > 0.01f)
        {
            floor.localPosition = Vector3.MoveTowards(
                floor.localPosition,
                info.nextPos,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        floor.localPosition = info.nextPos;

        Vector3 temp = info.nextPos;
        info.nextPos = info.prevPos;
        info.prevPos = temp;

        floors[index] = info;
        isMoving += 1;
    }

    private IEnumerator MoveWind(int index)
    {
        WindObjectInfo info = winds[index];
        if (info.windObject == null) yield break;

        Transform windTransform = info.windObject.transform;
        Vector3 startPos = windTransform.localPosition;
        float elapsed = 0f;
        float duration = windObjectSpeed > 0f ? windObjectSpeed : 0.001f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            windTransform.localPosition = Vector3.Lerp(startPos, info.targetPos, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        windTransform.localPosition = info.targetPos;

        Vector3 temp = info.targetPos;
        info.targetPos = info.prevPos;
        info.prevPos = temp;

        winds[index] = info;
    }

    private IEnumerator RampPropellers(bool spinUp)
    {
        float elapsed = 0f;

        float[] startSpeeds = new float[propellers.Count];
        for (int i = 0; i < propellers.Count; i++)
            startSpeeds[i] = propellers[i].currentSpeed;

        while (elapsed < propellerRampTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / propellerRampTime);

            for (int i = 0; i < propellers.Count; i++)
            {
                PropellerInfo info = propellers[i];
                float targetSpeed = spinUp ? info.rotateSpeed : 0f;
                info.currentSpeed = Mathf.Lerp(startSpeeds[i], targetSpeed, t);
                propellers[i] = info;
            }

            yield return null;
        }

        for (int i = 0; i < propellers.Count; i++)
        {
            PropellerInfo info = propellers[i];
            info.currentSpeed = spinUp ? info.rotateSpeed : 0f;
            propellers[i] = info;
        }

        propellerRampCoroutine = null;
    }
}
