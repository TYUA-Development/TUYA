using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialAreaPrompt : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";
    public bool showOnlyOnce = true;
    public float duplicateBlockTime = 1.0f;
    public bool showDebugLog = true;

    [Header("UI References")]
    public GameObject promptRoot;
    public CanvasGroup promptCanvasGroup;
    public TextMeshProUGUI promptText;
    public RectTransform promptRect;

    [Header("First Text")]
    [TextArea(2, 5)]
    public string tutorialMessage =
        "우클릭으로 조준하고 좌클릭으로 화살을 놓습니다.";

    [Header("Follow Up Text")]
    public bool useFollowUpPrompt = false;

    [TextArea(2, 5)]
    public string followUpMessage =
        "코어에 화살을 맞춰 길을 여세요.";

    public float followUpDelay = 0.2f;
    public float followUpFadeInTime = 0.6f;
    public float followUpStayTime = 999f;
    public float followUpFadeOutTime = 0.6f;

    [Header("Core Hint")]
    public CoreActivationController coreActivationController;
    public bool showCoreHintOnFollowUp = true;
    public bool waitUntilCoreActivated = true;

    [Header("Timing")]
    public float fadeInTime = 0.5f;
    public float stayTime = 3.0f;
    public float fadeOutTime = 0.7f;

    [Header("Motion")]
    public bool useMotion = true;
    public float moveDistance = 18f;

    [Header("Hide Option")]
    public bool disableRootWhenHidden = false;

    [Header("State")]
    public bool hasShown;
    public bool isShowing;

    private Coroutine showCoroutine;
    private Vector2 originalPosition;
    private bool originalPositionCached;

    private float lastShowTime = -999f;

    private void Awake()
    {
        AutoFindUIReferences();
        CacheOriginalPosition();
        ForcePrepareUI();
        HideInstant();

        if (showDebugLog)
            Debug.Log("[TutorialAreaPrompt] 준비 완료: " + gameObject.name);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryShow(other.gameObject);
    }

    private void TryShow(GameObject otherObject)
    {
        if (otherObject == null)
            return;

        if (!IsPlayerObject(otherObject))
            return;

        if (showOnlyOnce && hasShown)
            return;

        if (isShowing)
            return;

        if (Time.time - lastShowTime < duplicateBlockTime)
            return;

        ShowPrompt();
    }

    private bool IsPlayerObject(GameObject otherObject)
    {
        if (otherObject.CompareTag(playerTag))
            return true;

        Transform parent = otherObject.transform.parent;

        while (parent != null)
        {
            if (parent.CompareTag(playerTag))
                return true;

            parent = parent.parent;
        }

        return false;
    }

    public void ShowPrompt()
    {
        if (showOnlyOnce && hasShown)
            return;

        hasShown = true;
        isShowing = true;
        lastShowTime = Time.time;

        if (showCoroutine != null)
            StopCoroutine(showCoroutine);

        showCoroutine = StartCoroutine(ShowRoutine());
    }

    [ContextMenu("테스트로 튜토리얼 표시")]
    public void TestShowPrompt()
    {
        hasShown = false;
        isShowing = false;
        ShowPrompt();
    }

    private IEnumerator ShowRoutine()
    {
        AutoFindUIReferences();

        if (!CheckUIReferences())
        {
            isShowing = false;
            yield break;
        }

        ForcePrepareUI();
        CacheOriginalPosition();

        yield return StartCoroutine(ShowSingleMessage(
            tutorialMessage,
            fadeInTime,
            stayTime,
            fadeOutTime,
            false
        ));

        if (useFollowUpPrompt)
        {
            if (followUpDelay > 0f)
                yield return new WaitForSeconds(followUpDelay);

            if (showCoreHintOnFollowUp && coreActivationController != null)
                coreActivationController.ShowCoreHintRing();

            yield return StartCoroutine(ShowSingleMessage(
                followUpMessage,
                followUpFadeInTime,
                followUpStayTime,
                followUpFadeOutTime,
                waitUntilCoreActivated
            ));
        }

        HideInstant();

        isShowing = false;
        showCoroutine = null;
    }

    private IEnumerator ShowSingleMessage(string message, float inTime, float holdTime, float outTime, bool waitForCore)
    {
        ForcePrepareUI();

        promptText.text = message;

        Color textColor = promptText.color;
        textColor.a = 1f;
        promptText.color = textColor;
        promptText.enabled = true;
        promptText.gameObject.SetActive(true);

        promptCanvasGroup.alpha = 0f;
        promptCanvasGroup.interactable = false;
        promptCanvasGroup.blocksRaycasts = false;

        if (promptRect != null)
        {
            promptRect.localScale = Vector3.one;
            promptRect.SetAsLastSibling();

            if (useMotion)
                promptRect.anchoredPosition = originalPosition + new Vector2(0f, -moveDistance);
            else
                promptRect.anchoredPosition = originalPosition;
        }

        yield return StartCoroutine(FadeAndMove(0f, 1f, inTime, true));

        if (waitForCore && coreActivationController != null)
        {
            while (!coreActivationController.isActivated)
                yield return null;
        }
        else
        {
            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);
        }

        yield return StartCoroutine(FadeAndMove(1f, 0f, outTime, false));
    }

    private IEnumerator FadeAndMove(float fromAlpha, float toAlpha, float duration, bool moveIn)
    {
        if (duration <= 0f)
        {
            SetAlpha(toAlpha);
            yield break;
        }

        float timer = 0f;

        Vector2 startPos = originalPosition;
        Vector2 endPos = originalPosition;

        if (promptRect != null && useMotion)
        {
            if (moveIn)
            {
                startPos = originalPosition + new Vector2(0f, -moveDistance);
                endPos = originalPosition;
            }
            else
            {
                startPos = originalPosition;
                endPos = originalPosition + new Vector2(0f, moveDistance * 0.35f);
            }
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, smoothT));

            if (promptRect != null && useMotion)
                promptRect.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);

            yield return null;
        }

        SetAlpha(toAlpha);

        if (promptRect != null && moveIn)
            promptRect.anchoredPosition = originalPosition;
    }

    private void SetAlpha(float alpha)
    {
        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = alpha;
    }

    private void HideInstant()
    {
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.interactable = false;
            promptCanvasGroup.blocksRaycasts = false;
        }

        if (promptRect != null && originalPositionCached)
            promptRect.anchoredPosition = originalPosition;

        if (promptRoot != null && disableRootWhenHidden)
            promptRoot.SetActive(false);
    }

    private void ForcePrepareUI()
    {
        if (promptRoot != null)
            promptRoot.SetActive(true);

        if (promptRect != null)
        {
            promptRect.gameObject.SetActive(true);
            promptRect.localScale = Vector3.one;
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.gameObject.SetActive(true);
            promptCanvasGroup.interactable = false;
            promptCanvasGroup.blocksRaycasts = false;
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.enabled = true;
        }

        Canvas parentCanvas = null;

        if (promptRoot != null)
            parentCanvas = promptRoot.GetComponentInParent<Canvas>(true);

        if (parentCanvas != null)
            parentCanvas.gameObject.SetActive(true);
    }

    private void CacheOriginalPosition()
    {
        if (promptRect == null)
            return;

        if (originalPositionCached)
            return;

        originalPosition = promptRect.anchoredPosition;
        originalPositionCached = true;
    }

    private bool CheckUIReferences()
    {
        bool result = true;

        if (promptRoot == null)
        {
            Debug.LogWarning("[TutorialAreaPrompt] Prompt Root가 비어있습니다.");
            result = false;
        }

        if (promptCanvasGroup == null)
        {
            Debug.LogWarning("[TutorialAreaPrompt] Prompt Canvas Group이 비어있습니다.");
            result = false;
        }

        if (promptText == null)
        {
            Debug.LogWarning("[TutorialAreaPrompt] Prompt Text가 비어있습니다.");
            result = false;
        }

        if (promptRect == null)
        {
            Debug.LogWarning("[TutorialAreaPrompt] Prompt Rect가 비어있습니다.");
            result = false;
        }

        return result;
    }

    private void AutoFindUIReferences()
    {
        if (promptRoot == null)
            return;

        if (promptCanvasGroup == null)
            promptCanvasGroup = promptRoot.GetComponent<CanvasGroup>();

        if (promptRect == null)
            promptRect = promptRoot.GetComponent<RectTransform>();

        if (promptText == null)
            promptText = promptRoot.GetComponentInChildren<TextMeshProUGUI>(true);
    }
}