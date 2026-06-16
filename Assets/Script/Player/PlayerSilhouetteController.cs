using UnityEngine;

public class PlayerSilhouetteController : MonoBehaviour
{
    [Header("Target Renderers")]
    public SpriteRenderer[] targetRenderers;

    [Header("Silhouette")]
    [Range(0f, 1f)]
    public float silhouetteAmount = 0f;

    [Tooltip("0 = 원래 색, 1 = silhouetteColor 완전 적용")]
    public Color silhouetteColor = Color.black;

    [Tooltip("SetSilhouette을 사용할 때 색 변화 속도")]
    public float transitionSpeed = 4f;

    private Color[] originalColors;
    private float targetAmount = 0f;

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        originalColors = new Color[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
                originalColors[i] = targetRenderers[i].color;
        }
    }

    private void Update()
    {
        silhouetteAmount = Mathf.MoveTowards(
            silhouetteAmount,
            targetAmount,
            transitionSpeed * Time.deltaTime
        );

        ApplySilhouette();
    }

    private void ApplySilhouette()
    {
        if (targetRenderers == null || originalColors == null)
            return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null)
                continue;

            Color original = originalColors[i];

            Color target = silhouetteColor;
            target.a = original.a;

            targetRenderers[i].color = Color.Lerp(original, target, silhouetteAmount);
        }
    }

    public void SetSilhouette(bool enable)
    {
        targetAmount = enable ? 1f : 0f;
    }

    public void SetSilhouetteAmount(float amount)
    {
        targetAmount = Mathf.Clamp01(amount);
    }

    public void SetSilhouetteInstant(float amount)
    {
        silhouetteAmount = Mathf.Clamp01(amount);
        targetAmount = silhouetteAmount;
        ApplySilhouette();
    }

    public void SetSilhouetteColor(Color color)
    {
        silhouetteColor = color;
        ApplySilhouette();
    }

    public void SetTransitionSpeed(float speed)
    {
        transitionSpeed = Mathf.Max(0f, speed);
    }

    public void RefreshOriginalColors()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (originalColors == null || originalColors.Length != targetRenderers.Length)
            originalColors = new Color[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
                originalColors[i] = targetRenderers[i].color;
        }
    }
}