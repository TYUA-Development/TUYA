using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SkyObject
{
    public GameObject skyObject;

    // 알파 감소 시작 위치
    public float startPos;

    // 알파가 0이 되는 위치
    public float endPos;
}

public class SkyManager : MonoBehaviour
{
    public Transform player;

    public List<SkyObject> skyObjects;

    void Update()
    {
        float playerX = player.position.x;

        foreach (SkyObject sky in skyObjects)
        {
            if (sky.skyObject == null)
                continue;

            SpriteRenderer renderer =
                sky.skyObject.GetComponent<SpriteRenderer>();

            if (renderer == null)
                continue;

            Color color = renderer.color;

            // 0 ~ 1 보간값 계산
            float t = Mathf.InverseLerp(
                sky.startPos,
                sky.endPos,
                playerX);

            // startPos에서는 1
            // endPos에서는 0
            color.a = 1.0f - t;

            renderer.color = color;
        }
    }
}