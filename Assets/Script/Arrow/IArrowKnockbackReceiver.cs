using UnityEngine;

// IArrowHit과 별개로, 화살에 맞는 순간 물리적인 넉백을 받고 싶은 오브젝트가 구현한다.
// Arrow.OnTriggerEnter2D가 IArrowHit 처리와 함께 이 인터페이스도 확인해 OnArrowKnockback을 호출한다.
public interface IArrowKnockbackReceiver
{
    void OnArrowKnockback(Vector2 hitPoint, Vector2 hitDirection);
}
