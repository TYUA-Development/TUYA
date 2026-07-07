using UnityEngine;

public class MusicPuzzlePropellerHitProxy : MonoBehaviour, IArrowHit
{
    [SerializeField] private HangingMusicPuzzleNoteObject owner;

    public void SetOwner(HangingMusicPuzzleNoteObject noteObject)
    {
        owner = noteObject;
    }

    public void OnHit()
    {
        if (owner == null)
            owner = GetComponentInParent<HangingMusicPuzzleNoteObject>();

        if (owner != null)
            owner.HandlePropellerHit(gameObject);
    }
}
