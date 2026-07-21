using UnityEngine;

public class PressureTopRelay : MonoBehaviour
{
    private PressureCorePlatform platform;

    private void Awake()
    {
        platform = GetComponentInParent<PressureCorePlatform>();
    }

    private void OnCollisionEnter2D(Collision2D collision) => platform.HandleTopCollisionEnter(collision);
    private void OnCollisionStay2D(Collision2D collision) => platform.HandleTopCollisionStay(collision);
    private void OnCollisionExit2D(Collision2D collision) => platform.HandleTopCollisionExit(collision);
}
