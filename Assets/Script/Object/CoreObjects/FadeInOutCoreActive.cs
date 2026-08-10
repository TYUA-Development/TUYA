using System.Collections;
using UnityEngine;

public class FadeInOutCoreActive : MonoBehaviour
{
    [Header("Letterbox")]
    public CutsceneLetterboxUI letterboxUI;
    public float fadeInTime = 0.45f;
    public float fadeOutTime = 0.45f;

    [Tooltip("FadeIn 후 자동으로 FadeOut까지 대기할 시간. 0 이하이면 자동으로 꺼지지 않고 FadeOut()을 직접 호출해줘야 합니다.")]
    public float holdTime = 1.5f;

    [Header("Core Activation Link")]
    [Tooltip("이 오브젝트와 연결된 CoreActivation. 연결된 CoreActivation이 활성화되면 자동으로 FadeIn이 실행됩니다.")]
    public CoreActivation coreActivation;

    private Coroutine holdCoroutine;

    private void Reset()
    {
        if (coreActivation == null)
            coreActivation = GetComponent<CoreActivation>();
    }

    private void OnEnable()
    {
        if (coreActivation != null)
            coreActivation.onActivated += HandleCoreActivationActivated;
    }

    private void OnDisable()
    {
        if (coreActivation != null)
            coreActivation.onActivated -= HandleCoreActivationActivated;
    }

    private void HandleCoreActivationActivated()
    {
        FadeIn();
    }

    public void FadeIn()
    {
        if (letterboxUI == null)
            return;

        if (holdCoroutine != null)
            StopCoroutine(holdCoroutine);

        letterboxUI.ShowBars(fadeInTime);

        if (holdTime > 0f)
            holdCoroutine = StartCoroutine(HoldThenFadeOut());
    }

    public void FadeOut()
    {
        if (letterboxUI == null)
            return;

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }

        letterboxUI.HideBars(fadeOutTime);
    }

    private IEnumerator HoldThenFadeOut()
    {
        yield return new WaitForSeconds(holdTime);

        letterboxUI.HideBars(fadeOutTime);
        holdCoroutine = null;
    }
}
