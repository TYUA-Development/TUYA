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

        [Header("Angles")]
        public float angleA = 45f;
        public float angleB = 0f;
        public float angleC = -45f;
        public float angleD = 0f;

        [Header("Timing")]
        public float startDelay = 0f;
        public float rotateTime = 0.9f;
        public float holdTime = 0.9f;

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

    private bool isLooping = false;
    private Coroutine[] loopRoutines;

    private void Start()
    {
        if (playOnStart)
        {
            StartLoop();
        }
    }

    public void StartLoop()
    {
        if (isLooping)
            return;

        isLooping = true;

        if (parts == null || parts.Length == 0)
            return;

        loopRoutines = new Coroutine[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] != null && parts[i].partRoot != null)
            {
                loopRoutines[i] = StartCoroutine(PartLoop(parts[i]));
            }
        }
    }

    public void StopLoop()
    {
        isLooping = false;

        if (loopRoutines == null)
            return;

        for (int i = 0; i < loopRoutines.Length; i++)
        {
            if (loopRoutines[i] != null)
            {
                StopCoroutine(loopRoutines[i]);
                loopRoutines[i] = null;
            }
        }
    }

    private IEnumerator PartLoop(PassagePart part)
    {
        yield return new WaitForSeconds(part.startDelay);

        float[] angles =
        {
            part.angleA,
            part.angleB,
            part.angleC,
            part.angleD
        };

        int index = 0;

        while (isLooping)
        {
            float targetAngle = angles[index];

            yield return StartCoroutine(RotatePartToAngle(part, targetAngle));

            if (part.playSound && lockClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(lockClip);
            }

            yield return new WaitForSeconds(part.holdTime);

            index++;

            if (index >= angles.Length)
                index = 0;
        }
    }

    private IEnumerator RotatePartToAngle(PassagePart part, float targetZ)
    {
        if (part == null || part.partRoot == null)
            yield break;

        if (part.playSound && rotateStartClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(rotateStartClip);
        }

        float timer = 0f;

        float startZ = part.partRoot.localEulerAngles.z;

        if (startZ > 180f)
            startZ -= 360f;

        while (timer < part.rotateTime)
        {
            timer += Time.deltaTime;

            float t = timer / part.rotateTime;
            t = Mathf.Clamp01(t);
            t = Mathf.SmoothStep(0f, 1f, t);

            float z = Mathf.LerpAngle(startZ, targetZ, t);

            Vector3 euler = part.partRoot.localEulerAngles;
            euler.z = z;
            part.partRoot.localEulerAngles = euler;

            yield return null;
        }

        Vector3 finalEuler = part.partRoot.localEulerAngles;
        finalEuler.z = targetZ;
        part.partRoot.localEulerAngles = finalEuler;
    }
}