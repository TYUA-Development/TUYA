using UnityEngine;

// BoxObject가 이 컴포넌트를 가진 오브젝트와 한 번이라도 충돌하면, 그 순간부터
// 접촉이 끊긴 뒤에도 계속(영구히) 화살에 맞아도 넉백이 가해지지 않는다.
// IBoxKnockbackFree(닿아있는 동안만 면제)와 달리 "한 번 닿으면 영구 적용"이라는
// 차이가 있어 별도 마커 컴포넌트로 분리했다.
public class BoxKnockBackDown : MonoBehaviour
{
}
