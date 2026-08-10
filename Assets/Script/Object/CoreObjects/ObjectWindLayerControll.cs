using System.Collections.Generic;
using UnityEngine;

public class ObjectWindLayerControll : MonoBehaviour
{
    [Tooltip("코어가 활성화될 때마다 Toggle Layer를 IgnoredLayer에 넣거나 뺄 대상 Object_Wind 목록")]
    public List<Object_Wind> targetWinds = new List<Object_Wind>();

    [Tooltip("토글할 레이어. 대상 Object_Wind의 IgnoredLayer에 이미 포함돼 있으면 제거하고, 없으면 추가한다.")]
    public LayerMask toggleLayer;

    public void Toggle()
    {
        foreach (Object_Wind wind in targetWinds)
        {
            if (wind == null)
                continue;

            wind.ignoredLayer ^= toggleLayer;
        }
    }
}
