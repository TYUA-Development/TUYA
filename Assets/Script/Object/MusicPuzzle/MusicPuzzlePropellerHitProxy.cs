using UnityEngine;

public class MusicPuzzlePropellerHitProxy : MonoBehaviour, IArrowHit
{
    [SerializeField] private HangingMusicPuzzleNoteObject owner;
    private Collider2D hitCollider;

    private void Awake()
    {
        hitCollider = GetComponent<Collider2D>();
    }

    public void SetOwner(HangingMusicPuzzleNoteObject noteObject)
    {
        owner = noteObject;
    }

    public void OnHit()
    {
        // Arrow.cs가 IArrowHit 판정용으로 호출한다. 실제 회전 처리는 OnTriggerEnter2D에서 방향 정보와 함께 수행한다.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Arrow arrow))
            return;

        if (owner == null)
            owner = GetComponentInParent<HangingMusicPuzzleNoteObject>();

        if (owner == null)
            return;

        if (hitCollider == null)
            hitCollider = GetComponent<Collider2D>();

        Vector2 hitPoint = hitCollider != null
            ? hitCollider.ClosestPoint(arrow.transform.position)
            : (Vector2)transform.position;

        owner.HandlePropellerHit(hitPoint, arrow.transform.right);
    }
}
