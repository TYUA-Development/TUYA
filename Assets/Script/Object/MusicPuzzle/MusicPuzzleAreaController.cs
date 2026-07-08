using System.Collections;
using UnityEngine;

public class MusicPuzzleAreaController : MonoBehaviour
{
    [System.Serializable]
    public class PathMoveTarget
    {
        public Transform target;
        public Transform moveToPoint;
        public Vector3 localOffset;
        public bool useMoveToPoint = true;
    }

    [Header("Puzzle")]
    [SerializeField] private MusicPuzzleCoreBridge questionCore;
    [SerializeField] private MusicPuzzleCoreBridge answerCore;
    [SerializeField] private HangingMusicPuzzleNoteObject[] noteObjects = new HangingMusicPuzzleNoteObject[4];
    [SerializeField] private int[] expectedNoteIndexes = new int[4] { 0, 1, 2, 3 };

    [Header("Notes")]
    [SerializeField] private AudioSource noteAudioSource;
    [SerializeField] private AudioClip[] noteClips = new AudioClip[4];
    [SerializeField] private float[] noteVolumes = new float[4] { 1f, 1f, 1f, 1f };
    [SerializeField] private float questionNoteInterval = 0.55f;
    [SerializeField] private float submissionNoteInterval = 0.45f;
    [SerializeField] private float successNoteInterval = 0.6f;
    [SerializeField] private float successNoteVolumeMultiplier = 1.2f;

    [Header("Core Audio")]
    [SerializeField] private AudioSource puzzleAudioSource;
    [SerializeField] private AudioClip questionCoreLightClip;
    [Range(0f, 1f)]
    [SerializeField] private float questionCoreLightVolume = 1f;
    [SerializeField] private AudioClip successResonanceClip;
    [Range(0f, 1f)]
    [SerializeField] private float successResonanceVolume = 1f;
    [SerializeField] private AudioClip failClip;
    [Range(0f, 1f)]
    [SerializeField] private float failVolume = 1f;
    [SerializeField] private AudioClip pathOpenClip;
    [Range(0f, 1f)]
    [SerializeField] private float pathOpenVolume = 1f;
    [SerializeField] private AudioClip guideClip;
    [Range(0f, 1f)]
    [SerializeField] private float guideVolume = 1f;

    [Header("Delays")]
    [SerializeField] private float questionMelodyDelay = 0.3f;
    [SerializeField] private float answerMelodyDelay = 0.3f;
    [SerializeField] private float successSequenceDelay = 0.35f;
    [SerializeField] private float successResonanceDelay = 0.25f;
    [SerializeField] private float failSoundDelay = 0.2f;

    [Header("Path Open - Move")]
    [SerializeField] private PathMoveTarget[] pathMoveTargets;
    [SerializeField] private float pathOpenDuration = 2f;
    [SerializeField] private AnimationCurve pathOpenEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Path Open - Enable/Disable")]
    [SerializeField] private GameObject[] objectsToDeactivate;
    [SerializeField] private Collider2D[] collidersToDisable;
    [SerializeField] private GameObject[] objectsToActivate;

    [Header("Success Guide")]
    [SerializeField] private LineRenderer guideLineRenderer;
    [SerializeField] private ParticleSystem[] guideParticles;
    [SerializeField] private Transform guideStartPoint;
    [SerializeField] private Transform guideEndPoint;
    [SerializeField] private float guideTravelTime = 2.5f;
    [SerializeField] private bool keepGuideAfterSuccess = true;
    [SerializeField] private float guideHideDelay = 4f;

    [Header("State")]
    [SerializeField] private bool puzzleStarted;
    [SerializeField] private bool puzzleSolved;

    private bool sequenceRunning;
    private bool hasPlayedIntro;
    private Coroutine startRoutine;
    private Coroutine submitRoutine;

    private void Awake()
    {
        BindNoteObjects();
    }

    private void Reset()
    {
        noteAudioSource = GetComponent<AudioSource>();
        puzzleAudioSource = GetComponent<AudioSource>();
    }

    private void OnValidate()
    {
        EnsureLength(ref noteObjects, 4);
        EnsureLength(ref expectedNoteIndexes, 4);
        EnsureLength(ref noteClips, 4);
        EnsureLength(ref noteVolumes, 4);

        for (int i = 0; i < expectedNoteIndexes.Length; i++)
            expectedNoteIndexes[i] = Mathf.Clamp(expectedNoteIndexes[i], 0, 3);

        for (int i = 0; i < noteVolumes.Length; i++)
            noteVolumes[i] = Mathf.Clamp01(noteVolumes[i]);

        BindNoteObjects();
    }

    public bool IsPuzzleSolved => puzzleSolved;

    public void StartMusicPuzzle()
    {
        BeginPuzzleFromAreaEntry();
    }

    public void ActivatePuzzle()
    {
        BeginPuzzleFromAreaEntry();
    }

    public void BeginPuzzleFromArea()
    {
        BeginPuzzleFromAreaEntry();
    }

    public void NotifyQuestionCoreActivated(MusicPuzzleCoreBridge source)
    {
        BeginPuzzleInternal();
    }

    public void SubmitAnswerFromCore(MusicPuzzleCoreBridge source)
    {
        if (!puzzleStarted || puzzleSolved || sequenceRunning)
            return;

        if (submitRoutine != null)
            StopCoroutine(submitRoutine);

        submitRoutine = StartCoroutine(SubmitRoutine());
    }

    public void PlayNoteByIndex(int noteIndex, float volumeMultiplier = 1f, float delay = 0f)
    {
        StartCoroutine(PlayNoteDelayed(noteIndex, volumeMultiplier, delay));
    }

    public int[] GetCurrentNoteIndexes()
    {
        int[] indexes = new int[noteObjects.Length];

        for (int i = 0; i < noteObjects.Length; i++)
            indexes[i] = noteObjects[i] != null ? noteObjects[i].CurrentNoteIndex : -1;

        return indexes;
    }

    private void BeginPuzzleFromAreaEntry()
    {
        if (hasPlayedIntro)
            return;

        hasPlayedIntro = true;
        BeginPuzzleInternal();
    }

    private void BeginPuzzleInternal()
    {
        BindNoteObjects();

        if (puzzleSolved)
            return;

        if (sequenceRunning)
            return;

        puzzleStarted = true;

        if (startRoutine != null)
            StopCoroutine(startRoutine);

        startRoutine = StartCoroutine(StartPuzzleRoutine());
    }

    private IEnumerator StartPuzzleRoutine()
    {
        sequenceRunning = true;

        if (questionCore != null)
            questionCore.ActivateCoreVisuals();

        PlayPuzzleAudio(questionCoreLightClip, questionCoreLightVolume);

        if (questionMelodyDelay > 0f)
            yield return new WaitForSeconds(questionMelodyDelay);

        yield return StartCoroutine(PlayNoteSequence(expectedNoteIndexes, questionNoteInterval, 1f));

        sequenceRunning = false;
        startRoutine = null;
    }

    private IEnumerator SubmitRoutine()
    {
        sequenceRunning = true;

        if (answerCore != null)
            answerCore.ActivateCoreVisuals();

        if (answerMelodyDelay > 0f)
            yield return new WaitForSeconds(answerMelodyDelay);

        int[] currentIndexes = GetCurrentNoteIndexes();
        yield return StartCoroutine(PlayNoteSequence(currentIndexes, submissionNoteInterval, 1f));

        if (MatchesExpected(currentIndexes))
            yield return StartCoroutine(SuccessRoutine());
        else
            yield return StartCoroutine(FailRoutine());

        sequenceRunning = false;
        submitRoutine = null;
    }

    private IEnumerator SuccessRoutine()
    {
        puzzleSolved = true;

        if (questionCore != null)
            questionCore.SetExternalActivationLocked(true);
        if (answerCore != null)
            answerCore.SetExternalActivationLocked(true);

        if (successSequenceDelay > 0f)
            yield return new WaitForSeconds(successSequenceDelay);

        yield return StartCoroutine(PlayNoteSequence(expectedNoteIndexes, successNoteInterval, successNoteVolumeMultiplier));

        if (successResonanceDelay > 0f)
            yield return new WaitForSeconds(successResonanceDelay);

        PlayPuzzleAudio(successResonanceClip, successResonanceVolume);

        yield return StartCoroutine(OpenPathRoutine());
        StartCoroutine(GuideRoutine());
    }

    private IEnumerator FailRoutine()
    {
        if (failSoundDelay > 0f)
            yield return new WaitForSeconds(failSoundDelay);

        PlayPuzzleAudio(failClip, failVolume);
    }

    private IEnumerator OpenPathRoutine()
    {
        PlayPuzzleAudio(pathOpenClip, pathOpenVolume);
        SetObjectsActive(objectsToActivate, true);
        SetObjectsActive(objectsToDeactivate, false);
        SetCollidersEnabled(collidersToDisable, false);

        if (pathMoveTargets == null || pathMoveTargets.Length == 0)
            yield break;

        Vector3[] startPositions = new Vector3[pathMoveTargets.Length];
        Vector3[] endPositions = new Vector3[pathMoveTargets.Length];

        for (int i = 0; i < pathMoveTargets.Length; i++)
        {
            PathMoveTarget moveTarget = pathMoveTargets[i];
            if (moveTarget == null || moveTarget.target == null)
                continue;

            startPositions[i] = moveTarget.target.position;
            endPositions[i] = moveTarget.useMoveToPoint && moveTarget.moveToPoint != null
                ? moveTarget.moveToPoint.position
                : moveTarget.target.TransformPoint(moveTarget.localOffset);
        }

        if (pathOpenDuration <= 0f)
        {
            ApplyPathPositions(endPositions);
            yield break;
        }

        float timer = 0f;

        while (timer < pathOpenDuration)
        {
            timer += Time.deltaTime;
            float normalized = Mathf.Clamp01(timer / pathOpenDuration);
            float eased = pathOpenEase != null ? pathOpenEase.Evaluate(normalized) : normalized;

            for (int i = 0; i < pathMoveTargets.Length; i++)
            {
                if (pathMoveTargets[i] == null || pathMoveTargets[i].target == null)
                    continue;

                pathMoveTargets[i].target.position = Vector3.LerpUnclamped(startPositions[i], endPositions[i], eased);
            }

            yield return null;
        }

        ApplyPathPositions(endPositions);
    }

    private IEnumerator GuideRoutine()
    {
        PlayPuzzleAudio(guideClip, guideVolume);

        if (guideParticles != null)
        {
            for (int i = 0; i < guideParticles.Length; i++)
            {
                if (guideParticles[i] == null)
                    continue;

                guideParticles[i].gameObject.SetActive(true);
                guideParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                guideParticles[i].Play();
            }
        }

        if (guideLineRenderer != null && guideStartPoint != null && guideEndPoint != null)
        {
            guideLineRenderer.enabled = true;
            guideLineRenderer.positionCount = 2;

            float timer = 0f;
            Vector3 start = guideStartPoint.position;
            Vector3 end = guideEndPoint.position;

            while (timer < guideTravelTime)
            {
                timer += Time.deltaTime;
                float t = guideTravelTime <= 0f ? 1f : Mathf.Clamp01(timer / guideTravelTime);
                guideLineRenderer.SetPosition(0, start);
                guideLineRenderer.SetPosition(1, Vector3.Lerp(start, end, t));
                yield return null;
            }

            guideLineRenderer.SetPosition(0, start);
            guideLineRenderer.SetPosition(1, end);
        }

        if (keepGuideAfterSuccess)
            yield break;

        if (guideHideDelay > 0f)
            yield return new WaitForSeconds(guideHideDelay);

        if (guideLineRenderer != null)
            guideLineRenderer.enabled = false;

        if (guideParticles != null)
        {
            for (int i = 0; i < guideParticles.Length; i++)
            {
                if (guideParticles[i] == null)
                    continue;

                guideParticles[i].Stop();
                guideParticles[i].gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator PlayNoteSequence(int[] indexes, float interval, float volumeMultiplier)
    {
        if (indexes == null)
            yield break;

        for (int i = 0; i < indexes.Length; i++)
        {
            PlayNote(indexes[i], volumeMultiplier);

            if (interval > 0f && i < indexes.Length - 1)
                yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator PlayNoteDelayed(int noteIndex, float volumeMultiplier, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        PlayNote(noteIndex, volumeMultiplier);
    }

    private void PlayNote(int noteIndex, float volumeMultiplier)
    {
        if (noteAudioSource == null || noteClips == null)
            return;

        if (noteIndex < 0 || noteIndex >= noteClips.Length)
            return;

        AudioClip clip = noteClips[noteIndex];
        if (clip == null)
            return;

        float volume = GetNoteVolume(noteIndex) * Mathf.Max(0f, volumeMultiplier);
        noteAudioSource.PlayOneShot(clip, volume);
    }

    private float GetNoteVolume(int noteIndex)
    {
        if (noteVolumes == null || noteIndex < 0 || noteIndex >= noteVolumes.Length)
            return 1f;

        return Mathf.Clamp01(noteVolumes[noteIndex]);
    }

    private void PlayPuzzleAudio(AudioClip clip, float volume)
    {
        if (puzzleAudioSource == null || clip == null)
            return;

        puzzleAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private bool MatchesExpected(int[] currentIndexes)
    {
        if (currentIndexes == null || expectedNoteIndexes == null)
            return false;

        if (currentIndexes.Length != expectedNoteIndexes.Length)
            return false;

        for (int i = 0; i < expectedNoteIndexes.Length; i++)
        {
            if (currentIndexes[i] != expectedNoteIndexes[i])
                return false;
        }

        return true;
    }

    private void ApplyPathPositions(Vector3[] endPositions)
    {
        if (pathMoveTargets == null || endPositions == null)
            return;

        for (int i = 0; i < pathMoveTargets.Length && i < endPositions.Length; i++)
        {
            if (pathMoveTargets[i] != null && pathMoveTargets[i].target != null)
                pathMoveTargets[i].target.position = endPositions[i];
        }
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }

    private static void SetCollidersEnabled(Collider2D[] colliders, bool enabled)
    {
        if (colliders == null)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = enabled;
        }
    }

    private static void EnsureLength<T>(ref T[] array, int length)
    {
        if (array == null)
        {
            array = new T[length];
            return;
        }

        if (array.Length != length)
            System.Array.Resize(ref array, length);
    }

    private void BindNoteObjects()
    {
        if (noteObjects == null)
            return;

        for (int i = 0; i < noteObjects.Length; i++)
        {
            if (noteObjects[i] != null)
                noteObjects[i].SetPuzzleController(this);
        }
    }
}
