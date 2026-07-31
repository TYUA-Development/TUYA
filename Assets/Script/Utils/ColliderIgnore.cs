using UnityEngine;

// 이 오브젝트의 Collider2D가 Inspector에서 고른 레이어들과 충돌하지 않게 한다.
[RequireComponent(typeof(Collider2D))]
public class ColliderIgnore : MonoBehaviour
{
    [Tooltip("이 오브젝트의 Collider가 충돌하지 않을 레이어들")]
    [SerializeField] private LayerMask ignoreLayers;

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void Apply()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();

        foreach (Collider2D col in colliders)
            col.excludeLayers = ignoreLayers;
    }
}
