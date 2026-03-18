using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ParallaxImage : MonoBehaviour
{
    [Header("클수록 멀리 있는 물체(1 ~ 1000)")]
    [SerializeField] private float parallaxStrength = 5f;

    [Header("축 선택")]
    [SerializeField] private bool moveX = true;
    [SerializeField] private bool moveY = false;

    private Vector3 startPosition;
    private bool isRegistered;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void OnBecameVisible()
    {
        if (ParallaxManager.Instance != null && !isRegistered)
        {
            ParallaxManager.Instance.Register(this);
            isRegistered = true;
        }
    }

    private void OnBecameInvisible()
    {
        if (ParallaxManager.Instance != null && isRegistered)
        {
            ParallaxManager.Instance.Unregister(this);
            isRegistered = false;
        }
    }

    private void OnDisable()
    {
        if (ParallaxManager.Instance != null && isRegistered)
        {
            ParallaxManager.Instance.Unregister(this);
            isRegistered = false;
        }
    }

    public void ApplyParallaxFromStart(Vector3 totalCameraDelta, float globalStrength)
    {
        float factor = parallaxStrength / globalStrength;

        Vector3 targetPosition = startPosition;

        if (moveX)
            targetPosition.x += totalCameraDelta.x * factor;

        if (moveY)
            targetPosition.y += totalCameraDelta.y * factor;

        transform.position = targetPosition;
    }
}