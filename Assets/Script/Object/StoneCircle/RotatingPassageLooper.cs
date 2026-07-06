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
        public float lockPitchMultiplier = 1f;
    }

    [Header("Passage Parts")]
    public PassagePart[] parts;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip rotateStartClip;
    [Range(0f, 1f)] public float rotateStartVolume = 1f;
    public AudioClip lockClip;
    [Range(0f, 1f)] public float lockVolume = 1f;

    [Header("Lock Sound Variation")]
    public float lockPitchMin = 0.92f;
    public float lockPitchMax = 1.08f;
    [Range(0f, 1f)] public float lockVolumeMin = 0.85f;
    [Range(0f, 1f)] public float lockVolumeMax = 1f;

    [Header("Option")]
    public bool playOnStart = false;
    public bool restartIfAlreadyPlaying = false;

    [Header("Start Timing")]
    public float initialStartDelay = 5f;
    public float partStartInterval = 1f;
    public bool startPartsTogether = true;
    public bool usePartStartDelayOffsets = false;

    [Header("Rotation")]
    public float sequenceSpinSpeed = 20f;
    public bool usePerPartSpinSpeed = true;
    public float spinDirection = 1f;
    public float spinRampTime = 0f;

    [Header("Lock")]
    public float targetLockAngle = 45f;
    public BreakableFragmentPlatformEvent breakablePlatformEvent;
    public float targetLockAngleAfterBreak = -45f;
    public float lockAngleTolerance = 2f;
    public float requiredRotationsBeforeLock = 2f;
    public float lockInterval = 1f;
    public bool useSequentialLockOrder = false;
    public float lockSoundLeadTime = 1f;
    public bool playLockClipAgainOnLock = false;

    private bool isLooping = false;
    private Coroutine sequenceRoutine;
    private Coroutine[] partRoutines;
    private bool[] partLocked;
    private int nextLockIndex;
    private float nextLockAllowedTime;

    private void Awake()
    {
        EnsureAudioSource();
    }

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
        float delay = Mathf.Max(0f, initialStartDelay);

        if (!startPartsTogether)
            delay += Mathf.Max(0f, partStartInterval) * index;

        if (usePartStartDelayOffsets)
            delay += Mathf.Max(0f, part.startDelay);

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (!isLooping)
            yield break;

        PlayPartSound(part, rotateStartClip);

        float timer = 0f;
        float rotatedDegrees = 0f;
        float direction = GetSpinDirection();
        float speed = GetSpinSpeed(part);
        float requiredDegrees = Mathf.Max(0f, requiredRotationsBeforeLock) * 360f;
        bool lockSoundPlayed = false;

        while (isLooping)
        {
            timer += Time.deltaTime;

            float speedMultiplier = GetSpeedMultiplier(timer);
            float delta = direction * speed * speedMultiplier * Time.deltaTime;
            rotatedDegrees += Mathf.Abs(delta);

            Vector3 euler = part.partRoot.localEulerAngles;
            euler.z = Mathf.Repeat(euler.z + delta, 360f);
            part.partRoot.localEulerAngles = euler;

            if (!lockSoundPlayed && ShouldPlayLockSoundSoon(rotatedDegrees, euler.z, requiredDegrees, speed * speedMultiplier, direction))
            {
                PlayPartSound(part, lockClip);
                lockSoundPlayed = true;
            }

            if (CanLockPart(index, rotatedDegrees, euler.z, requiredDegrees))
            {
                LockPart(index, part, lockSoundPlayed);
                yield break;
            }

            yield return null;
        }
    }

    private bool CanLockPart(int index, float rotatedDegrees, float currentZ, float requiredDegrees)
    {
        if (useSequentialLockOrder)
        {
            if (index != nextLockIndex)
                return false;

            if (Time.time < nextLockAllowedTime)
                return false;
        }

        if (rotatedDegrees < requiredDegrees)
            return false;

        return IsNearAngle(currentZ, GetActiveTargetLockAngle(), lockAngleTolerance);
    }

    private void LockPart(int index, PassagePart part, bool lockSoundAlreadyPlayed)
    {
        partLocked[index] = true;
        partRoutines[index] = null;

        if (!lockSoundAlreadyPlayed || playLockClipAgainOnLock)
            PlayPartSound(part, lockClip);

        if (!useSequentialLockOrder)
            return;

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

    private bool ShouldPlayLockSoundSoon(float rotatedDegrees, float currentZ, float requiredDegrees, float currentSpeed, float direction)
    {
        if (lockSoundLeadTime <= 0f || lockClip == null)
            return false;

        float angularSpeed = Mathf.Abs(currentSpeed);

        if (angularSpeed <= 0.01f)
            return false;

        if (rotatedDegrees + angularSpeed * lockSoundLeadTime < requiredDegrees)
            return false;

        float distanceToTarget = GetForwardAngleDistance(currentZ, GetActiveTargetLockAngle(), direction);
        float timeToTarget = distanceToTarget / angularSpeed;

        return timeToTarget <= lockSoundLeadTime;
    }

    private float GetActiveTargetLockAngle()
    {
        if (breakablePlatformEvent != null && breakablePlatformEvent.hasActivated)
            return targetLockAngleAfterBreak;

        return targetLockAngle;
    }

    private float GetForwardAngleDistance(float currentZ, float targetZ, float direction)
    {
        if (direction < 0f)
            return Mathf.Repeat(currentZ - targetZ, 360f);

        return Mathf.Repeat(targetZ - currentZ, 360f);
    }

    private void PlayPartSound(PassagePart part, AudioClip clip)
    {
        if (part == null || !part.playSound || clip == null)
            return;

        EnsureAudioSource();

        if (audioSource == null)
            return;

        if (clip == lockClip)
        {
            audioSource.pitch = GetLockPitch(part);
            audioSource.PlayOneShot(clip, GetLockVolume() * Mathf.Clamp01(lockVolume));
            return;
        }

        audioSource.pitch = 1f;
        audioSource.PlayOneShot(clip, GetVolumeForClip(clip));
    }

    private float GetLockPitch(PassagePart part)
    {
        float min = Mathf.Min(lockPitchMin, lockPitchMax);
        float max = Mathf.Max(lockPitchMin, lockPitchMax);
        float pitch = Random.Range(min, max);

        if (part != null)
            pitch *= Mathf.Max(0.01f, part.lockPitchMultiplier);

        return pitch;
    }

    private float GetLockVolume()
    {
        float min = Mathf.Min(lockVolumeMin, lockVolumeMax);
        float max = Mathf.Max(lockVolumeMin, lockVolumeMax);
        return Random.Range(min, max);
    }

    private float GetVolumeForClip(AudioClip clip)
    {
        if (clip == rotateStartClip)
            return Mathf.Clamp01(rotateStartVolume);

        return 1f;
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    private bool IsNearAngle(float currentZ, float targetZ, float tolerance)
    {
        float delta = Mathf.Abs(Mathf.DeltaAngle(currentZ, targetZ));
        return delta <= Mathf.Max(0.01f, tolerance);
    }
}
