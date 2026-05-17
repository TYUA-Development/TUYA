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

public class CoreObjectMoveFloor : MonoBehaviour, ICoreEvent, IArrowHit
{
    public List<CoreObjectMoveFloorInfo> floors;
    public float moveSpeed;

    private int isMoving;
    private Vector3 targetPos;

    // Start is called before the first frame update
    void Start()
    {
        isMoving = floors.Count;
    }

    public void OnCoreEvent()
    {
        if (isMoving == floors.Count)
        {
            isMoving = 0;
            for (int i = 0; i < floors.Count; i++)
            {
                CoreObjectMoveFloorInfo floorInfo = floors[i];

                floorInfo.prevPos = floorInfo.Floor.transform.localPosition;
                floors[i] = floorInfo;
                StartCoroutine(MoveFloor(i));
            }
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
