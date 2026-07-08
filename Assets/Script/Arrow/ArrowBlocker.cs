using UnityEngine;

// 다른 것엔 영향 없이 화살만 막아서 박히게 하는 컴포넌트.
// Arrow.cs가 IArrowHit을 감지해 기존 Stick() 로직으로 그대로 박아준다.
public class ArrowBlocker : MonoBehaviour, IArrowHit
{
    public void OnHit()
    {
    }
}
