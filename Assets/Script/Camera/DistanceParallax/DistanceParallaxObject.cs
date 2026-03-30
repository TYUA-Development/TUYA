using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DistanceParallaxObject : MonoBehaviour
{
    [Header("멀수록 변화가 적음")]
    [SerializeField] private float depth = 5f;

    [Header("점점 멀어질 때 줄어드는 크기 강도")]
    [SerializeField] private float scaleStrength = 0.08f;

    [Header("점점 멀어질 때 기준점 쪽으로 당겨지는 위치 강도")]
    [SerializeField] private float positionStrength = 0.5f;

    [Header("거리 효과의 기준점 (보통 카메라 또는 화면 중심 역할 오브젝트)")]
    [SerializeField] private Transform origin;

    private Vector3 startPosition;
    private Vector3 startScale;
    private bool isRegistered;

    private void Awake()
    {
        startPosition = transform.position;
        startScale = transform.localScale;
    }

    private void Start()
    {
        if (origin == null && Camera.main != null)
            origin = Camera.main.transform;
    }

    private void OnBecameVisible()
    {
        if (DistanceParallaxManager.Instance != null && !isRegistered)
        {
            DistanceParallaxManager.Instance.Register(this);
            isRegistered = true;
        }
    }

    private void OnBecameInvisible()
    {
        if (DistanceParallaxManager.Instance != null && isRegistered)
        {
            DistanceParallaxManager.Instance.Unregister(this);
            isRegistered = false;
        }
    }

    private void OnDisable()
    {
        if (DistanceParallaxManager.Instance != null && isRegistered)
        {
            DistanceParallaxManager.Instance.Unregister(this);
            isRegistered = false;
        }
    }

    public void ApplyDistanceEffect(float distanceAmount, float globalStrength)
    {
        if (origin == null)
            return;

        // depth가 클수록 변화량 감소
        float factor = globalStrength / Mathf.Max(depth, 0.0001f);

        // origin 방향으로 당겨지게
        Vector3 toOrigin = (origin.position - startPosition).normalized;

        // distanceAmount가 커질수록 멀어지는 연출
        // => 크기 감소 + origin 방향으로 이동
        Vector3 targetPosition = startPosition + toOrigin * (distanceAmount * positionStrength * factor);

        float scaleOffset = distanceAmount * scaleStrength * factor;
        float finalScaleMultiplier = Mathf.Max(0.05f, 1f - scaleOffset);

        Vector3 targetScale = startScale * finalScaleMultiplier;

        transform.position = targetPosition;

        transform.localScale = targetScale;
    }
}