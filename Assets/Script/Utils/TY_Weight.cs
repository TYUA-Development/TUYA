using UnityEngine;

public class TY_Weight : MonoBehaviour
{
    [Tooltip("이 오브젝트의 무게 값입니다. PressureCorePlatform이 이 값을 읽어 총 무게를 계산합니다.")]
    public float weight = 1f;
}
