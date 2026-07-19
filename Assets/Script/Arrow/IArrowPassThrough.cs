using UnityEngine;

// IArrowHit과 달리 화살을 멈추거나 박히게 하지 않는다.
// Arrow.OnTriggerEnter2D가 이 인터페이스를 감지하면 hasHit을 세우지 않고 알림만 준 뒤 그대로 통과시킨다.
public interface IArrowPassThrough
{
    void OnArrowPass(Vector2 hitPoint, Vector2 direction);
}
