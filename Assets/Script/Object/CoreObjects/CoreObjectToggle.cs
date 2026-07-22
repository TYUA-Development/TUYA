using System.Collections.Generic;
using UnityEngine;

public class CoreObjectToggle : MonoBehaviour
{
    [Header("Core Objects")]
    [Tooltip("어떤 코어를 맞춰도 동일하게 동작합니다. 여러 번 맞출 수 있는지 여부는 각 CoreActivationController의 activateOnlyOnce 값을 따릅니다.")]
    public List<CoreActivationController> coreObjects;

    [Header("Toggle Targets")]
    [Tooltip("코어가 활성화될 때마다 각 오브젝트의 활성 상태를 개별적으로 반전시킨다 (켜져있으면 끄고, 꺼져있으면 켠다)")]
    public List<GameObject> targetObjects;

    void Start()
    {
        foreach (var core in coreObjects)
        {
            if (core != null)
                core.onActivated += HandleCoreActivated;
        }
    }

    void OnDestroy()
    {
        foreach (var core in coreObjects)
        {
            if (core != null)
                core.onActivated -= HandleCoreActivated;
        }
    }

    private void HandleCoreActivated()
    {
        foreach (var obj in targetObjects)
        {
            if (obj == null)
                continue;

            obj.SetActive(!obj.activeSelf);
        }
    }
}
