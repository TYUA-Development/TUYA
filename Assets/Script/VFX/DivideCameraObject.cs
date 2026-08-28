using UnityEngine;

// IDivideCamera 마커 인터페이스를 구현하는 기본 컴포넌트.
// ShockWaveController의 camera1OnlyObjects/camera2OnlyObjects 리스트에 넣을
// 오브젝트에 붙이면 된다. 별도 로직 없이 "이 오브젝트는 특정 카메라 전용으로
// 취급해도 된다"는 표시로만 쓰인다.
public class DivideCameraObject : MonoBehaviour, IDivideCamera
{
}
