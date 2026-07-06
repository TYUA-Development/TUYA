using UnityEngine;

public class BacklightAreaTrigger : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";

    [Header("Silhouette Settings")]
    [Range(0f, 1f)]
    public float maxSilhouetteAmount = 1f;

    [Tooltip("완전 검정 대신 약간 보랏빛/푸른빛 실루엣도 가능")]
    public Color silhouetteColorInArea = Color.black;

    [Header("X Distance Fade")]
    [Tooltip("콜라이더 좌우 끝에서 이 거리만큼 이동하면서 점점 검정이 됩니다.")]
    public float fadeDistanceX = 6f;

    [Tooltip("콜라이더 중앙부에서 검정이 유지됩니다.")]
    public bool keepBlackInMiddle = true;

    [Tooltip("X 위치 기준으로만 계산합니다.")]
    public bool useOnlyX = true;

    [Header("Additional Silhouette Targets")]
    [Tooltip("플레이어와 같은 방식으로 함께 어두워질 스프라이트 그룹입니다. 각 오브젝트에 PlayerSilhouetteController를 붙여서 넣으세요.")]
    public PlayerSilhouetteController[] additionalSilhouetteTargets;

    [Tooltip("추가 타겟도 플레이어 위치 기준 fade 값을 그대로 따라갑니다.")]
    public bool applyPlayerAmountToAdditionalTargets = true;

    [Header("Debug")]
    public bool showDebugLog = false;

    private Collider2D areaCollider;
    private PlayerSilhouetteController currentSilhouette;

    private void Awake()
    {
        areaCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        currentSilhouette = FindSilhouetteController(collision);

        if (currentSilhouette == null)
            return;

        currentSilhouette.SetSilhouetteColor(silhouetteColorInArea);

        float amount = CalculateAmount(collision.transform.position);
        currentSilhouette.SetSilhouetteInstant(amount);
        ApplyAdditionalSilhouettes(amount);

        if (showDebugLog)
            Debug.Log("Backlight Enter Amount : " + amount);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        if (currentSilhouette == null)
            currentSilhouette = FindSilhouetteController(collision);

        if (currentSilhouette == null)
            return;

        float amount = CalculateAmount(collision.transform.position);
        currentSilhouette.SetSilhouetteColor(silhouetteColorInArea);
        currentSilhouette.SetSilhouetteInstant(amount);
        ApplyAdditionalSilhouettes(amount);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        PlayerSilhouetteController silhouette = FindSilhouetteController(collision);

        if (silhouette != null)
            silhouette.SetSilhouetteInstant(0f);

        ApplyAdditionalSilhouettes(0f);

        if (currentSilhouette == silhouette)
            currentSilhouette = null;

        if (showDebugLog)
            Debug.Log("Backlight Exit");
    }

    private float CalculateAmount(Vector3 playerPosition)
    {
        if (areaCollider == null)
            areaCollider = GetComponent<Collider2D>();

        if (areaCollider == null)
            return 0f;

        Bounds bounds = areaCollider.bounds;

        float left = bounds.min.x;
        float right = bounds.max.x;
        float x = playerPosition.x;

        float safeFadeDistance = Mathf.Max(0.01f, fadeDistanceX);

        float leftFade = Mathf.InverseLerp(left, left + safeFadeDistance, x);
        float rightFade = Mathf.InverseLerp(right, right - safeFadeDistance, x);

        leftFade = Mathf.Clamp01(leftFade);
        rightFade = Mathf.Clamp01(rightFade);

        float amount;

        if (keepBlackInMiddle)
        {
            amount = Mathf.Min(leftFade, rightFade);
        }
        else
        {
            float center = bounds.center.x;
            float halfWidth = bounds.extents.x;
            float distanceFromCenter = Mathf.Abs(x - center);
            amount = 1f - Mathf.Clamp01(distanceFromCenter / halfWidth);
        }

        return Mathf.Clamp01(amount * maxSilhouetteAmount);
    }

    private void ApplyAdditionalSilhouettes(float amount)
    {
        if (!applyPlayerAmountToAdditionalTargets)
            return;

        if (additionalSilhouetteTargets == null)
            return;

        for (int i = 0; i < additionalSilhouetteTargets.Length; i++)
        {
            PlayerSilhouetteController silhouette = additionalSilhouetteTargets[i];

            if (silhouette == null)
                continue;

            silhouette.SetSilhouetteColor(silhouetteColorInArea);
            silhouette.SetSilhouetteInstant(amount);
        }
    }

    private PlayerSilhouetteController FindSilhouetteController(Collider2D collision)
    {
        PlayerSilhouetteController silhouette;

        silhouette = collision.GetComponent<PlayerSilhouetteController>();
        if (silhouette != null)
            return silhouette;

        silhouette = collision.GetComponentInParent<PlayerSilhouetteController>();
        if (silhouette != null)
            return silhouette;

        silhouette = collision.GetComponentInChildren<PlayerSilhouetteController>();
        if (silhouette != null)
            return silhouette;

        return null;
    }
}
