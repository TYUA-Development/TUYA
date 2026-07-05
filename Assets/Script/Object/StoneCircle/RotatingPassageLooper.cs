using System.Collections;
using UnityEngine;

public class RotatingPassageLooper : MonoBehaviour
{
    [System.Serializable]
    public class PassagePart
    {
        [Header("Part")]
        public string partName;
        public Transform partRoot;

        [Header("Legacy Angles")]
        public float angleA = 45f;
        public float angleB = 0f;
        public float angleC = -45f;
        public float angleD = 0f;

        [Header("Legacy Timing")]
        public float startDelay = 0f;
        public float rotateTime = 0.9f;
        public float holdTime = 0.9f;

        [Header("Legacy Sequence Spin")]
        public float spinSpeed = 20f;
        public float freeSpinTime = 2.5f;
        public float lockToZeroTime = 0.75f;

        [Header("Sound")]
        public bool playSound = true;
    }

    [Header("Passage Parts")]
    public PassagePart[] parts;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip rotateStartClip;
    public AudioClip lockClip;

    [Header("Option")]
    public bool playOnStart = false;
    public bool restartIfAlreadyPlaying = false;

    [Header("Start Timing")]
    public float initialStartDelay = 5f;
    public float partStartInterval = 1f;
    public bool usePartStartDelayOffsets = false;

    [Header("Rotation")]
    public float sequenceSpinSpeed = 20f;
    public bool usePerPartSpinSpeed = false;
    public float spinDirection = 1f;
    public float spinRampTime = 0f;

    [Header("Lock")]
    public float targetLockAngle = 45f;
    public float lockAngleTolerance = 2f;
    public float requiredRotationsBeforeLock = 2f;
    public float lockInterval = 1f;

    private bool isLooping = false;
    private Coroutine sequenceRoutine;
    private Coroutine[] partRoutines;
    private bool[] partLocked;
    private int nextLockIndex;
    private float nextLockAllowedTime;

    private void Start()
    {
        if (playOnStart)
            StartLoop();
    }

    public void StartLoop()
    {
        if (isLooping)
        {
            if (!restartIfAlreadyPlaying)
                return;

            StopLoop();
        }

        if (parts == null || parts.Length == 0)
            return;

        isLooping = true;
        nextLockIndex = 0;
        nextLockAllowedTime = Time.time;
        partRoutines = new Coroutine[parts.Length];
        partLocked = new bool[parts.Length];

        sequenceRoutine = StartCoroutine(PassageSequence());
    }

    public void StopLoop()
    {
        isLooping = false;

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        StopAllPartRoutines();
    }

    private IEnumerator PassageSequence()
    {
        for (int i = 0; i < parts.Length; i++)
        {
            PassagePart part = parts[i];

            if (part == null || part.partRoot == null)
            {
                partLocked[i] = true;
                continue;
            }

            partRoutines[i] = StartCoroutine(RotatePartUntilLockWindow(i, part));
        }

        while (isLooping && !AreAllPartsLocked())
            yield return null;

        isLooping = false;
        sequenceRoutine = null;
    }

    private IEnumerator RotatePartUntilLockWindow(int index, PassagePart part)
    {
        float delay = Mathf.Max(0f, initialStartDelay) + Mathf.Max(0f, partStartInterval) * index;

        if (usePartStartDelayOffsets)
            delay += Mathf.Max(0f, part.startDelay);

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (!isLooping)
            yield break;

        if (part.playSound && rotateStartClip != null && audioSource != null)
            audioSource.PlayOneShot(rotateStartClip);

        float timer = 0f;
        float rotatedDegrees = 0f;
        float direction = GetSpinDirection();
        float speed = GetSpinSpeed(part);
        float requiredDegrees = Mathf.Max(0f, requiredRotationsBeforeLock) * 360f;

        while (isLooping)
        {
            timer += Time.deltaTime;

            float speedMultiplier = GetSpeedMultiplier(timer);
            float delta = direction * speed * speedMultiplier * Time.deltaTime;
            rotatedDegrees += Mathf.Abs(delta);

            Vector3 euler = part.partRoot.localEulerAngles;
            euler.z = Mathf.Repeat(euler.z + delta, 360f);
            part.partRoot.localEulerAngles = euler;

            if (CanLockPart(index, rotatedDegrees, euler.z, requiredDegrees))
            {
                LockPart(index, part);
                yield break;
            }

            yield return null;
        }
    }

    private bool CanLockPart(int index, float rotatedDegrees, float currentZ, float requiredDegrees)
    {
        if (index != nextLockIndex)
            return false;

        if (Time.time < nextLockAllowedTime)
            return false;

        if (rotatedDegrees < requiredDegrees)
            return false;

        return IsNearAngle(currentZ, targetLockAngle, lockAngleTolerance);
    }

    private void LockPart(int index, PassagePart part)
    {
        partLocked[index] = true;
        partRoutines[index] = null;

        if (part.playSound && lockClip != null && audioSource != null)
            audioSource.PlayOneShot(lockClip);

        nextLockIndex++;
        nextLockAllowedTime = Time.time + Mathf.Max(0f, lockInterval);

        while (nextLockIndex < parts.Length && IsPartMissing(nextLockIndex))
        {
            partLocked[nextLockIndex] = true;
            nextLockIndex++;
        }
    }

    private bool IsPartMissing(int index)
    {
        if (parts == null || index < 0 || index >= parts.Length)
            return true;

        return parts[index] == null || parts[index].partRoot == null;
    }

    private bool AreAllPartsLocked()
    {
        if (partLocked == null)
            return true;

        for (int i = 0; i < partLocked.Length; i++)
        {
            if (!partLocked[i])
                return false;
        }

        return true;
    }

    private void StopAllPartRoutines()
    {
        if (partRoutines == null)
            return;

        for (int i = 0; i < partRoutines.Length; i++)
        {
            if (partRoutines[i] != null)
            {
                StopCoroutine(partRoutines[i]);
                partRoutines[i] = null;
            }
        }
    }

    private float GetSpinDirection()
    {
        return spinDirection < 0f ? -1f : 1f;
    }

    private float GetSpinSpeed(PassagePart part)
    {
        if (usePerPartSpinSpeed && part != null)
            return Mathf.Abs(part.spinSpeed);

        return Mathf.Abs(sequenceSpinSpeed);
    }

    private float GetSpeedMultiplier(float timer)
    {
        if (spinRampTime <= 0f)
            return 1f;

        float t = Mathf.Clamp01(timer / spinRampTime);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    private bool IsNearAngle(float currentZ, float targetZ, float tolerance)
    {
        float delta = Mathf.Abs(Mathf.DeltaAngle(currentZ, targetZ));
        return delta <= Mathf.Max(0.01f, tolerance);
    }
}
