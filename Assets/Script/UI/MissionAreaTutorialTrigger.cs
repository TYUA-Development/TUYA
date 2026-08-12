using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MissionAreaTutorialTrigger : MonoBehaviour
{
    public string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyAreaEntered(this);
    }
}
