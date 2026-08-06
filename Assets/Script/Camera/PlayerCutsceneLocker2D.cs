using UnityEngine;

public class PlayerCutsceneLocker2D : MonoBehaviour
{
    [Header("Target")]
    public PlayerController playerController;
    public Rigidbody2D playerRigidbody;

    [Header("Lock Settings")]
    [Tooltip("üũ�ϸ� ���� �� PlayerController�� ��� ���� �Է�/�̵��� ������ �����ϴ�.")]
    public bool disablePlayerControllerDuringLock = true;

    [Tooltip("üũ�ϸ� Rigidbody2D�� ������ �����մϴ�.")]
    public bool freezeRigidbodyDuringLock = true;

    [Header("State")]
    public bool isLocked;

    private RigidbodyConstraints2D originalConstraints;
    private float originalGravityScale;
    private bool originalPlayerControllerEnabled;
    private bool hasCached;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody2D>();

        CacheOriginalValues();
    }

    private void CacheOriginalValues()
    {
        if (hasCached)
            return;

        if (playerRigidbody != null)
        {
            originalConstraints = playerRigidbody.constraints;
            originalGravityScale = playerRigidbody.gravityScale;
        }

        if (playerController != null)
        {
            originalPlayerControllerEnabled = playerController.enabled;
        }

        hasCached = true;
    }

    public void LockNow()
    {
        CacheOriginalValues();

        isLocked = true;

        if (playerController != null && playerController.currentState != playerController.idleState)
            playerController.OnIdle();

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;

            if (freezeRigidbodyDuringLock)
            {
                playerRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
                playerRigidbody.gravityScale = 0f;
            }
        }

        if (playerController != null && disablePlayerControllerDuringLock)
        {
            playerController.enabled = false;
        }
    }

    public void UnlockNow()
    {
        if (!isLocked)
            return;

        isLocked = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;

            if (freezeRigidbodyDuringLock)
            {
                playerRigidbody.constraints = originalConstraints;
                playerRigidbody.gravityScale = originalGravityScale;
            }
        }

        if (playerController != null && disablePlayerControllerDuringLock)
        {
            playerController.enabled = originalPlayerControllerEnabled;
        }
    }
}