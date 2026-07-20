using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct StoneBridgeInfo
{
    public GameObject stoneBridge;
    public float targetYPos;
}

public class StoneBridge : MonoBehaviour, ICoreEvent, IArrowHit
{
    public List<StoneBridgeInfo> gameObjects;
    public Vector3 CameraPos;
    public float CameraSpeed;
    public float stoneMoveSpeed;
    private bool IsBridge;

    public bool coreRiseUp;
    public float risePosY;
    public float riseSpeed;

    private void Awake()
    {
        IsBridge = false;
    }

    public void OnCoreEvent(bool isPressed = true)
    {
        //CameraMovement.Instance.MoveCamera(CameraPos, 5.0f);
        CameraMovement.Instance.MoveCamera(CameraPos, 5.0f, 1.0f, CameraSpeed, true);
        CameraMovement.Instance.MoveCameraNoise(2.0f, 5.0f, false, true);

        if (coreRiseUp)
            StartCoroutine(RisingCore());

        foreach (StoneBridgeInfo info in gameObjects)
        {
            StartCoroutine(MoveBridge(info));
        }
    }

    public void OnHit()
    {
        if (!IsBridge)
        {
            OnCoreEvent();
            IsBridge = true;
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

    private IEnumerator MoveBridge(StoneBridgeInfo info)
    {
        yield return new WaitForSeconds(1.0F);

        Transform bridge = info.stoneBridge.transform;

        Vector3 targetPos = bridge.localPosition;
        targetPos.y = info.targetYPos;

        while (Mathf.Abs(bridge.localPosition.y - info.targetYPos) > 0.01f)
        {
            bridge.localPosition = Vector3.MoveTowards( bridge.localPosition, targetPos, stoneMoveSpeed * Time.deltaTime);

            yield return null;
        }

        bridge.localPosition = targetPos;
    }

}
