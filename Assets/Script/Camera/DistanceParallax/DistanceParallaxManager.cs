using System.Collections.Generic;
using UnityEngine;

public class DistanceParallaxManager : MonoBehaviour
{
    public static DistanceParallaxManager Instance { get; private set; }

    [Header("전후 이동 기준 오브젝트")]
    [SerializeField] private Transform target;

    [Header("이 축 방향 이동을 전후 이동으로 간주")]
    [SerializeField] private ForwardAxis forwardAxis = ForwardAxis.Z;

    [Header("전체 거리 효과 배율")]
    [SerializeField] private float globalStrength = 1f;

    [Header("기준 시작값을 현재 위치로 자동 설정")]
    [SerializeField] private bool useCurrentPositionAsStart = true;

    private readonly List<DistanceParallaxObject> activeObjects = new List<DistanceParallaxObject>();

    private float startForwardValue;
    private float previousForwardValue;

    public enum ForwardAxis
    {
        X,
        Y,
        Z
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("[DistanceParallaxManager] target이 설정되지 않았습니다.");
            enabled = false;
            return;
        }

        float current = GetAxisValue(target.position);

        if (useCurrentPositionAsStart)
        {
            startForwardValue = current;
            previousForwardValue = current;
        }
    }

    public void Register(DistanceParallaxObject obj)
    {
        if (obj == null)
            return;

        if (!activeObjects.Contains(obj))
            activeObjects.Add(obj);
    }

    public void Unregister(DistanceParallaxObject obj)
    {
        if (obj == null)
            return;

        activeObjects.Remove(obj);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float currentForwardValue = GetAxisValue(target.position);

        // 시작 지점 기준 누적 거리
        float distanceAmount = currentForwardValue - startForwardValue;

        // 뒤로 가면 음수가 될 수 있으니, 멀어지는 효과만 쓰고 싶으면 clamp
        distanceAmount = Mathf.Max(0f, distanceAmount);

        // 값 변화가 거의 없으면 굳이 순회 안 함
        if (Mathf.Abs(currentForwardValue - previousForwardValue) < 0.0001f)
            return;

        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            DistanceParallaxObject obj = activeObjects[i];

            if (obj == null)
            {
                activeObjects.RemoveAt(i);
                continue;
            }

            obj.ApplyDistanceEffect(distanceAmount, globalStrength);
        }

        previousForwardValue = currentForwardValue;
    }

    private float GetAxisValue(Vector3 position)
    {
        switch (forwardAxis)
        {
            case ForwardAxis.X:
                return position.x;
            case ForwardAxis.Y:
                return position.y;
            default:
                return position.z;
        }
    }

    public void ResetDistanceOrigin()
    {
        if (target == null)
            return;

        float current = GetAxisValue(target.position);
        startForwardValue = current;
        previousForwardValue = current;
    }
}