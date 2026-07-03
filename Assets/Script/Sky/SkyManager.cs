using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SkyObject
{
    public GameObject skyObject;

    // ���� ���� ���� ��ġ
    public float startPos;

    // ���İ� 0�� �Ǵ� ��ġ
    public float endPos;
}

public class SkyManager : MonoBehaviour
{
    public Transform player;

    public List<SkyObject> skyObjects;

    void Awake()
    {
        if(player == null)
        {
            player = FindObjectOfType<PlayerController>().gameObject.transform;
        }
    }

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

            // 0 ~ 1 ������ ���
            float t = Mathf.InverseLerp(
                sky.startPos,
                sky.endPos,
                playerX);

            // startPos������ 1
            // endPos������ 0
            color.a = 1.0f - t;

            renderer.color = color;
        }
    }
}