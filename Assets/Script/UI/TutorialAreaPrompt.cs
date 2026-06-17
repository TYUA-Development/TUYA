using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialAreaPrompt : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";

    [Tooltip("체크하면 이 Area에서는 딱 한 번만 나옵니다.")]
    public bool showOnlyOnce = true;

    [Tooltip("중복 감지 방지 시간")]
    public float duplicateBlockTime = 1.0f;

    public bool showDebugLog = true;

    [Header("UI References")]
    public GameObject promptRoot;
    public CanvasGroup promptCanvasGroup;
    public TextMeshProUGUI promptText;
    public RectTransform promptRect;

    [Header("Text")]
    [TextArea(2, 5)]
    public string tutorialMessage =
        "우클릭 길게 누르기 : 조준\n좌클릭 놓기 : 활쏘기";

    [Header("Timing")]
    public float fadeInTime = 0.5f;
    public float stayTime = 3.0f;
    public float fadeOutTime = 0.7f;

    [Header("Motion")]
    public bool useMotion = true;
    public float moveDistance = 18f;

    [Header("Hide Option")]
    [Tooltip("꺼두는 걸 추천. UI 오브젝트를 끄지 않고 투명하게만 숨깁니다.")]
    public bool disableRootWhenHidden = false;

    [Header("State")]
    public bool hasShown;
    public bool isShowing;

    private Coroutine showCoroutine;
    private Vector2 originalPosition;
    private bool originalPositionCached;

    private float lastShowTime = -999f;
    private GameObject currentPlayerObject;

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
        if (showDebugLog)
            Debug.Log("[TutorialAreaPrompt] Trigger Enter: " + other.name + " / Tag: " + other.tag);

        TryShow(other.gameObject);
    }

    private void TryShow(GameObject otherObject)
    {
        if (otherObject == null)
            return;

        if (!IsPlayerObject(otherObject))
            return;

        if (showOnlyOnce && hasShown)
        {
            if (showDebugLog)
                Debug.Log("[TutorialAreaPrompt] 이미 한 번 보여줬으므로 무시");

            return;
        }

        if (isShowing)
        {
            if (showDebugLog)
                Debug.Log("[TutorialAreaPrompt] 이미 표시 중이라 무시");

            return;
        }

        if (Time.time - lastShowTime < duplicateBlockTime)
        {
            if (showDebugLog)
                Debug.Log("[TutorialAreaPrompt] 중복 감지 방지 시간이라 무시");

            return;
        }

        currentPlayerObject = otherObject;
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

        if (showDebugLog)
            Debug.Log("[TutorialAreaPrompt] 플레이어가 아님: " + otherObject.name + " / Tag: " + otherObject.tag);

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

        promptText.text = tutorialMessage;

        Color textColor = promptText.color;
        textColor.a = 1f;
        promptText.color = textColor;
        promptText.enabled = true;
        promptText.gameObject.SetActive(true);

        promptCanvasGroup.alpha = 0f;
        promptCanvasGroup.interactable = false;
        promptCanvasGroup.blocksRaycasts = false;

        CacheOriginalPosition();

        if (promptRect != null)
        {
            promptRect.localScale = Vector3.one;
            promptRect.SetAsLastSibling();

            if (useMotion)
                promptRect.anchoredPosition = originalPosition + new Vector2(0f, -moveDistance);
            else
                promptRect.anchoredPosition = originalPosition;
        }

        if (showDebugLog)
            Debug.Log("[TutorialAreaPrompt] UI 표시 시작");

        yield return StartCoroutine(FadeAndMove(0f, 1f, fadeInTime, true));

        if (stayTime > 0f)
            yield return new WaitForSeconds(stayTime);

        yield return StartCoroutine(FadeAndMove(1f, 0f, fadeOutTime, false));

        HideInstant();

        isShowing = false;
        showCoroutine = null;
        currentPlayerObject = null;

        if (showDebugLog)
            Debug.Log("[TutorialAreaPrompt] UI 표시 종료");
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