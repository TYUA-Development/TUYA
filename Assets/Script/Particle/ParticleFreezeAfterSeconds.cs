using UnityEngine;

public class ParticleSimulationSoftStopper : MonoBehaviour
{
    [Header("Target")]
    public ParticleSystem targetParticle;

    [Header("Timing")]
    [Tooltip("몇 초 뒤 완전히 멈출지")]
    public float stopAfterSeconds = 10f;

    [Tooltip("멈추기 전 몇 초 동안 서서히 느려질지")]
    public float slowDownSeconds = 4f;

    [Header("Stop Settings")]
    [Tooltip("완전히 멈춘 뒤 Pause해서 화면에 그대로 남깁니다.")]
    public bool pauseAtEnd = true;

    [Tooltip("자식 파티클까지 같이 멈춥니다.")]
    public bool includeChildren = true;

    [Tooltip("완전히 0으로 가기 직전 최소 속도")]
    public float minimumSimulationSpeed = 0.01f;

    private bool isCounting;
    private bool isStopped;

    private float timer;
    private float originalSimulationSpeed = 1f;

    private void Awake()
    {
        if (targetParticle == null)
            targetParticle = GetComponent<ParticleSystem>();

        CacheOriginalSpeed();
    }

    private void OnEnable()
    {
        ResetStopper();
    }

    private void Update()
    {
        if (targetParticle == null)
            return;

        if (!isCounting && !isStopped && targetParticle.IsAlive(includeChildren))
        {
            StartCounting();
        }

        if (!isCounting)
            return;

        timer += Time.deltaTime;

        float slowStartTime = Mathf.Max(0f, stopAfterSeconds - slowDownSeconds);

        if (timer >= slowStartTime)
        {
            float t = Mathf.InverseLerp(slowStartTime, stopAfterSeconds, timer);
            t = Smooth01(t);

            float newSpeed = Mathf.Lerp(originalSimulationSpeed, minimumSimulationSpeed, t);
            SetSimulationSpeed(newSpeed);
        }

        if (timer >= stopAfterSeconds)
        {
            StopSoftly();
        }
    }

    private void StartCounting()
    {
        isCounting = true;
        isStopped = false;
        timer = 0f;

        CacheOriginalSpeed();
        SetSimulationSpeed(originalSimulationSpeed);
    }

    private void StopSoftly()
    {
        isCounting = false;
        isStopped = true;

        SetSimulationSpeed(minimumSimulationSpeed);

        if (pauseAtEnd && targetParticle != null)
        {
            targetParticle.Pause(includeChildren);
        }
    }

    public void ResetStopper()
    {
        isCounting = false;
        isStopped = false;
        timer = 0f;

        if (targetParticle != null)
        {
            SetSimulationSpeed(originalSimulationSpeed);
        }
    }

    private void CacheOriginalSpeed()
    {
        if (targetParticle == null)
            return;

        ParticleSystem.MainModule main = targetParticle.main;

        if (main.simulationSpeed > 0f)
            originalSimulationSpeed = main.simulationSpeed;
    }

    private void SetSimulationSpeed(float speed)
    {
        if (targetParticle == null)
            return;

        ParticleSystem.MainModule main = targetParticle.main;
        main.simulationSpeed = speed;

        if (!includeChildren)
            return;

        ParticleSystem[] children = targetParticle.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null)
                continue;

            ParticleSystem.MainModule childMain = children[i].main;
            childMain.simulationSpeed = speed;
        }
    }

    private float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}