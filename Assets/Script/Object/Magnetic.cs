using System.Collections.Generic;
using UnityEngine;

public class Magnetic : MonoBehaviour
{
    [Tooltip("이 오브젝트와 접촉 중일 때 함께 움직일 오브젝트 목록")]
    public List<GameObject> attachedObjects = new List<GameObject>();

    private Collider2D magneticCollider;
    private Vector3 prevPosition;
    private readonly Dictionary<GameObject, Collider2D> colliderCache = new Dictionary<GameObject, Collider2D>();

    private void Awake()
    {
        magneticCollider = GetComponent<Collider2D>();
        prevPosition = transform.position;
    }

    private void FixedUpdate()
    {
        Vector3 delta = transform.position - prevPosition;

        if (delta != Vector3.zero && magneticCollider != null)
        {
            foreach (GameObject obj in attachedObjects)
            {
                if (obj == null)
                    continue;

                Collider2D otherCollider = GetCachedCollider(obj);
                if (otherCollider == null || !magneticCollider.IsTouching(otherCollider))
                    continue;

                if (obj.TryGetComponent<Rigidbody2D>(out var rb))
                    rb.MovePosition((Vector2)obj.transform.position + (Vector2)delta);
                else
                    obj.transform.position += delta;
            }
        }

        prevPosition = transform.position;
    }

    private Collider2D GetCachedCollider(GameObject obj)
    {
        if (!colliderCache.TryGetValue(obj, out Collider2D col) || col == null)
        {
            col = obj.GetComponent<Collider2D>();
            colliderCache[obj] = col;
        }

        return col;
    }
}
