using System.Collections;
using System.Collections.Generic;
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

    [Header("Fonts By Language")]
    [Tooltip("언어별 폰트. Language enum 값을 인덱스로 사용 - 0=Korean, 1=English, 2=Japanese, 3=ChineseSimplified, 4=ChineseTraditional. 특정 언어 칸이 비어있으면 0번(Korean) 폰트로 대체되고, 리스트 전체가 비어있으면 promptText에 원래 지정된 폰트를 그대로 쓴다.")]
    public List<TMP_FontAsset> fontsByLanguage = new List<TMP_FontAsset>();

    [Header("First Text")]
    [Tooltip("언어별 문구. Language enum 값을 인덱스로 사용 - 0=Korean, 1=English, 2=Japanese, 3=ChineseSimplified, 4=ChineseTraditional. 특정 언어 칸이 비어있으면 0번(Korean)으로 대체 표시된다.")]
    public List<string> tutorialMessage = new List<string>();

    [Header("Follow Up Text")]
    public bool useFollowUpPrompt = false;

    [Tooltip("언어별 문구. Language enum 값을 인덱스로 사용 - 0=Korean, 1=English, 2=Japanese, 3=ChineseSimplified, 4=ChineseTraditional. 특정 언어 칸이 비어있으면 0번(Korean)으로 대체 표시된다.")]
    public List<string> followUpMessage = new List<string>();

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
    public float aimShootTutorialExtraStayTime = 2.0f;
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
            Debug.Log("[TutorialAreaPrompt] �غ� �Ϸ�: " + gameObject.name);
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

    [ContextMenu("�׽�Ʈ�� Ʃ�丮�� ǥ��")]
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
            GetLocalizedMessage(tutorialMessage),
            fadeInTime,
            GetFirstMessageStayTime(),
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
                GetLocalizedMessage(followUpMessage),
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

        TMP_FontAsset localizedFont = GetLocalizedFont();
        if (localizedFont != null)
            promptText.font = localizedFont;

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

    private float GetFirstMessageStayTime()
    {
        // 언어와 무관하게 항상 0번(Korean) 원문 기준으로 판별한다 - 표시 언어가 바뀌어도
        // 이 감지 로직 자체는 흔들리지 않아야 하기 때문.
        string koreanText = tutorialMessage != null && tutorialMessage.Count > 0 ? tutorialMessage[0] : string.Empty;

        if (IsAimShootTutorialMessage(koreanText))
            return stayTime + aimShootTutorialExtraStayTime;

        return stayTime;
    }

    private bool IsAimShootTutorialMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return false;

        return message.Contains("\uC6B0\uD074\uB9AD\uC73C\uB85C \uC870\uC900") && message.Contains("\uC88C\uD074\uB9AD\uC73C\uB85C \uD654\uC0B4");
    }

    private string GetLocalizedMessage(List<string> messages)
    {
        if (messages == null || messages.Count == 0)
            return string.Empty;

        int index = SettingsManager.Instance != null ? (int)SettingsManager.Instance.CurrentLanguage : 0;

        if (index < 0 || index >= messages.Count || string.IsNullOrEmpty(messages[index]))
            index = 0; // 해당 언어 번역이 비어있으면 0번(Korean)으로 대체

        index = Mathf.Clamp(index, 0, messages.Count - 1);

        return messages[index];
    }

    private TMP_FontAsset GetLocalizedFont()
    {
        if (fontsByLanguage == null || fontsByLanguage.Count == 0)
            return null;

        int index = SettingsManager.Instance != null ? (int)SettingsManager.Instance.CurrentLanguage : 0;

        if (index < 0 || index >= fontsByLanguage.Count || fontsByLanguage[index] == null)
            index = 0; // 해당 언어 폰트가 비어있으면 0번(Korean) 폰트로 대체

        if (index < 0 || index >= fontsByLanguage.Count)
            return null;

        return fontsByLanguage[index];
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
            Debug.LogWarning("[TutorialAreaPrompt] Prompt Root�� ����ֽ��ϴ�.");
            result = false;
        }

        if (promptCanvasGroup == null)
        {
            Debug.LogWarning("[TutorialAreaPrompt] Prompt Canvas Group�� ����ֽ��ϴ�.");
            result = false;
        }

        if (promptText == null)
        {
            Debug.LogWarning("[TutorialAreaPrompt] Prompt Text�� ����ֽ��ϴ�.");
            result = false;
        }

        if (promptRect == null)
        {
            Debug.LogWarning("[TutorialAreaPrompt] Prompt Rect�� ����ֽ��ϴ�.");
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