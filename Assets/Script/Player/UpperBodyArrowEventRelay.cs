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

    public void ShowUpperBody()
    {
        if (player != null)
            player.ShowUpperBody();
    }

    public void HideUpperBody()
    {
        if (player != null)
            player.HideUpperBody();
    }

    public void FinishAttackAnimation()
    {
        if (player != null)
            player.FinishAttackAnimation();
    }
}
