// BoxObject가 이 인터페이스를 구현한 컴포넌트를 가진 오브젝트와 접촉하고 있는 동안에는,
// 화살에 맞아도(IArrowKnockbackReceiver.OnArrowKnockback) 넉백력이 가해지지 않는다.
// 별도 메서드 없이 "이 오브젝트에 닿아있으면 넉백 면제"라는 표시로만 쓰이는 마커 인터페이스.
public interface IBoxKnockbackFree
{
}
