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
}

public class CoreObjectMoveFloor : MonoBehaviour, ICoreEvent, IArrowHit
{
    public List<CoreObjectMoveFloorInfo> floors;
    public float moveSpeed;

    [Header("Wind Objects")]
    public List<GameObject> winds;

    [Header("Propellers")]
    public List<PropellerInfo> propellers;

    private int isMoving;
    private bool propellersActive = false;

    void Start()
    {
        isMoving = floors.Count;
    }

    void Update()
    {
        if (!propellersActive) return;

        for (int i = 0; i < propellers.Count; i++)
        {
            if (propellers[i].propeller != null)
                propellers[i].propeller.transform.Rotate(0f, 0f, propellers[i].rotateSpeed * Time.deltaTime);
        }
    }

    public void OnCoreEvent()
    {
        if (isMoving != floors.Count) return;

        isMoving = 0;

        foreach (GameObject wind in winds)
        {
            if (wind != null)
                wind.SetActive(!wind.activeSelf);
        }

        propellersActive = !propellersActive;

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
}
