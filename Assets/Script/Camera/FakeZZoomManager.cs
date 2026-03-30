using System;
using System.Collections.Generic;
using UnityEngine;

public class FakeZZoomManager : MonoBehaviour
{
    [Serializable]
    public class ZoomTarget
    {
        public Transform target;
        [Tooltip("클수록 더 많이 확대/축소됨")]
        public float distance = 1f;

        [HideInInspector] public Vector3 baseScale;
    }

    [Header("관리 대상")]
    [SerializeField] private List<ZoomTarget> targets = new List<ZoomTarget>();

    [Header("시간 설정")]
    [SerializeField] private float zoomInDuration = 5f;
    [SerializeField] private float zoomOutDuration = 5f;

    [Header("확대 강도")]
    [SerializeField] private float globalZoomStrength = 0.15f;

    [Header("곡선 (0~1 입력, 0~1 출력)")]
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float cycleTime;

    private void Awake()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].target == null)
                continue;

            targets[i].baseScale = targets[i].target.localScale;
        }
    }

    private void Update()
    {
        float oneCycle = zoomInDuration + zoomOutDuration;
        if (oneCycle <= 0.0001f)
            return;

        cycleTime += Time.deltaTime;
        float t = cycleTime % oneCycle;

        float normalized;

        if (t < zoomInDuration)
        {
            // 0 -> 1
            normalized = t / zoomInDuration;
        }
        else
        {
            // 1 -> 0
            float outTime = t - zoomInDuration;
            normalized = 1f - (outTime / zoomOutDuration);
        }

        float curveValue = zoomCurve.Evaluate(normalized);

        for (int i = 0; i < targets.Count; i++)
        {
            ZoomTarget item = targets[i];

            if (item.target == null)
                continue;

            float zoomAmount = item.distance * globalZoomStrength * curveValue;
            Vector3 targetScale = item.baseScale * (1f + zoomAmount);

            item.target.localScale = targetScale;
        }
    }

    public void ResetBaseScales()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].target == null)
                continue;

            targets[i].baseScale = targets[i].target.localScale;
        }
    }
}