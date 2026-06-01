using UnityEngine;

public class UpperBodyArrowEventRelay : MonoBehaviour
{
    public PlayerController player;

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<PlayerController>();
    }

    public void ShowHeldArrow()
    {
        if (player != null)
            player.ShowHeldArrow();
    }

    public void HideHeldArrow()
    {
        if (player != null)
            player.HideHeldArrow();
    }
}