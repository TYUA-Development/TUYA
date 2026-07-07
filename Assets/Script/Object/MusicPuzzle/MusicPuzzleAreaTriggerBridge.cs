using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class MusicPuzzleAreaTriggerBridge : MonoBehaviour
{
    public enum PuzzleStartMethod
    {
        StartMusicPuzzle,
        ActivatePuzzle,
        BeginPuzzleFromArea
    }

    [Header("Target")]
    [SerializeField] private MusicPuzzleAreaController puzzleController;
    [SerializeField] private PuzzleStartMethod startMethod = PuzzleStartMethod.StartMusicPuzzle;

    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool startOnPlayerEnter = true;
    [SerializeField] private bool startOnlyOnce = true;
    [SerializeField] private float startDelay = 0f;

    [Header("Extra Event")]
    [SerializeField] private UnityEvent onPuzzleStart;

    [Header("State")]
    [SerializeField] private bool hasStarted;

    private Coroutine startCoroutine;

    public void TriggerPuzzleStart()
    {
        if (startOnlyOnce && hasStarted)
            return;

        if (startCoroutine != null)
            StopCoroutine(startCoroutine);

        startCoroutine = StartCoroutine(StartRoutine());
    }

    public void ResetStartedState()
    {
        hasStarted = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!startOnPlayerEnter)
            return;

        if (!collision.CompareTag(playerTag))
            return;

        TriggerPuzzleStart();
    }

    private IEnumerator StartRoutine()
    {
        hasStarted = true;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (puzzleController == null)
        {
            Debug.LogWarning($"{nameof(MusicPuzzleAreaTriggerBridge)} on {name} has no puzzle controller.");
            startCoroutine = null;
            yield break;
        }

        switch (startMethod)
        {
            case PuzzleStartMethod.ActivatePuzzle:
                puzzleController.ActivatePuzzle();
                break;
            case PuzzleStartMethod.BeginPuzzleFromArea:
                puzzleController.BeginPuzzleFromArea();
                break;
            default:
                puzzleController.StartMusicPuzzle();
                break;
        }

        onPuzzleStart?.Invoke();
        startCoroutine = null;
    }
}
