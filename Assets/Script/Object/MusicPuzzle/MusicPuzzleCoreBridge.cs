using System.Collections;
using UnityEngine;

public enum MusicPuzzleCoreRole
{
    QuestionCore,
    AnswerCore
}

public class MusicPuzzleCoreBridge : MonoBehaviour, IArrowHit
{
    [Header("Puzzle")]
    [SerializeField] private MusicPuzzleAreaController puzzleController;
    [SerializeField] private MusicPuzzleCoreRole coreRole = MusicPuzzleCoreRole.QuestionCore;

    [Header("Existing Core")]
    [SerializeField] private CoreActivationController existingCore;
    [SerializeField] private bool subscribeToExistingCore = true;
    [SerializeField] private bool fallbackArrowHitWhenNoExistingCore = true;

    [Header("Reuse Existing Core Effects")]
    [SerializeField] private GameObject[] effectObjectsToEnable;
    [SerializeField] private SpriteRenderer[] renderersToEnable;
    [SerializeField] private Behaviour[] behavioursToEnable;
    [SerializeField] private ParticleSystem[] particlesToPlay;
    [SerializeField] private Animator[] animatorsToTrigger;
    [SerializeField] private string animatorTriggerName = "Activate";
    [SerializeField] private bool keepEffectsEnabled = true;
    [SerializeField] private float autoDisableEffectsAfter = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activationClip;
    [Range(0f, 1f)]
    [SerializeField] private float activationVolume = 1f;

    private Coroutine disableEffectsCoroutine;

    public MusicPuzzleCoreRole CoreRole => coreRole;

    private void Reset()
    {
        existingCore = GetComponent<CoreActivationController>();
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (subscribeToExistingCore && existingCore != null)
            existingCore.onActivated += HandleExistingCoreActivated;
    }

    private void OnDisable()
    {
        if (subscribeToExistingCore && existingCore != null)
            existingCore.onActivated -= HandleExistingCoreActivated;
    }

    public void ActivateCoreVisuals()
    {
        EnableObjects(true);
        PlayParticles();
        TriggerAnimators();
        PlayAudio(activationClip, activationVolume);

        if (!keepEffectsEnabled && autoDisableEffectsAfter > 0f)
        {
            if (disableEffectsCoroutine != null)
                StopCoroutine(disableEffectsCoroutine);

            disableEffectsCoroutine = StartCoroutine(DisableEffectsAfterDelay(autoDisableEffectsAfter));
        }
    }

    public void SignalPuzzle()
    {
        if (puzzleController != null && puzzleController.IsPuzzleSolved)
            return;

        ActivateCoreVisuals();

        if (puzzleController == null)
        {
            Debug.LogWarning($"{nameof(MusicPuzzleCoreBridge)} on {name} has no puzzle controller.");
            return;
        }

        if (coreRole == MusicPuzzleCoreRole.QuestionCore)
            puzzleController.NotifyQuestionCoreActivated(this);
        else
            puzzleController.SubmitAnswerFromCore(this);
    }

    public void SetExternalActivationLocked(bool locked)
    {
        if (existingCore != null)
            existingCore.activationLocked = locked;
    }

    public void FadeInCoreGlow()
    {
        if (existingCore != null)
            existingCore.FadeInActivateGlow();
    }

    public void OnHit()
    {
        if (!fallbackArrowHitWhenNoExistingCore && existingCore != null)
            return;

        SignalPuzzle();
    }

    private void HandleExistingCoreActivated()
    {
        SignalPuzzle();
    }

    private IEnumerator DisableEffectsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EnableObjects(false);
        disableEffectsCoroutine = null;
    }

    private void EnableObjects(bool active)
    {
        SetActive(effectObjectsToEnable, active);
        SetRendererEnabled(renderersToEnable, active);
        SetBehaviourEnabled(behavioursToEnable, active);
    }

    private void PlayParticles()
    {
        if (particlesToPlay == null)
            return;

        for (int i = 0; i < particlesToPlay.Length; i++)
        {
            ParticleSystem particle = particlesToPlay[i];
            if (particle == null)
                continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play();
        }
    }

    private void TriggerAnimators()
    {
        if (animatorsToTrigger == null || string.IsNullOrEmpty(animatorTriggerName))
            return;

        for (int i = 0; i < animatorsToTrigger.Length; i++)
        {
            Animator animator = animatorsToTrigger[i];
            if (animator == null)
                continue;

            animator.ResetTrigger(animatorTriggerName);
            animator.SetTrigger(animatorTriggerName);
        }
    }

    private void PlayAudio(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }

    private static void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }

    private static void SetRendererEnabled(SpriteRenderer[] renderers, bool enabled)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = enabled;
        }
    }

    private static void SetBehaviourEnabled(Behaviour[] behaviours, bool enabled)
    {
        if (behaviours == null)
            return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                behaviours[i].enabled = enabled;
        }
    }
}
