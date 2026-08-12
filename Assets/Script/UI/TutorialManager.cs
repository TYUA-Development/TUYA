using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Serializable]
    public struct TutorialEntry
    {
        [Tooltip("언어별 문구. Language enum의 값을 인덱스로 사용한다 - 0=Korean, 1=English, 2=Japanese, 3=ChineseSimplified, 4=ChineseTraditional. 특정 언어 칸이 비어있으면 0번(Korean)으로 대체 표시된다.")]
        public List<string> texts;

        [Tooltip("MissionArea 진입(또는 같은 MissionArea의 이전 튜토리얼 종료) 시점부터 이 튜토리얼이 표시되기까지 대기하는 시간(초)")]
        public float delay;

        public float displayDuration;
        public MissionAreaTutorialTrigger missionArea;
    }

    [Header("Shared UI")]
    public CanvasGroup promptCanvasGroup;
    public TextMeshProUGUI promptText;
    public RectTransform promptRect;

    [Header("Fonts By Language")]
    [Tooltip("언어별 폰트. Language enum 값을 인덱스로 사용 - 0=Korean, 1=English, 2=Japanese, 3=ChineseSimplified, 4=ChineseTraditional. 특정 언어 칸이 비어있으면 0번(Korean) 폰트로 대체되고, 리스트 전체가 비어있으면 promptText에 원래 지정된 폰트를 그대로 쓴다.")]
    public List<TMP_FontAsset> fontsByLanguage = new List<TMP_FontAsset>();

    [Header("Shared Fade Timing")]
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.7f;

    [Header("Shared Motion")]
    public bool useMotion = true;
    public float moveDistance = 18f;

    [Header("Tutorials")]
    public List<TutorialEntry> tutorials = new List<TutorialEntry>();

    public static TutorialManager Instance { get; private set; }

    private readonly HashSet<int> shownIndices = new HashSet<int>();
    private readonly Dictionary<MissionAreaTutorialTrigger, List<int>> triggerToIndices = new Dictionary<MissionAreaTutorialTrigger, List<int>>();

    private Vector2 originalPosition;
    private bool originalPositionCached;
    private Coroutine activeRoutine;

    private void Awake()
    {
        Instance = this;

        CacheOriginalPosition();
        HideInstant();

        for (int i = 0; i < tutorials.Count; i++)
        {
            MissionAreaTutorialTrigger trigger = tutorials[i].missionArea;

            if (trigger == null)
                continue;

            if (!triggerToIndices.TryGetValue(trigger, out List<int> indices))
            {
                indices = new List<int>();
                triggerToIndices.Add(trigger, indices);
            }

            // 리스트에 나열된 순서를 그대로 표시 순서로 쓴다 - 같은 MissionArea를 참조하는
            // 항목이 여럿이면 이 순서대로 하나씩 이어서 표시된다.
            indices.Add(i);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void NotifyAreaEntered(MissionAreaTutorialTrigger trigger)
    {
        if (trigger == null || !triggerToIndices.TryGetValue(trigger, out List<int> indices))
            return;

        List<int> toQueue = null;

        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];

            if (shownIndices.Contains(index))
                continue;

            // 표시 중인 다른 튜토리얼이 있어도 이 영역은 "본 것"으로 처리한다 - 진입 자체를
            // 무시하는 게 아니라, 나중에 다시 들어와도 재표시되지 않게 하기 위함.
            shownIndices.Add(index);

            if (toQueue == null)
                toQueue = new List<int>();

            toQueue.Add(index);
        }

        if (toQueue == null)
            return;

        if (activeRoutine != null)
            return;

        activeRoutine = StartCoroutine(ShowQueueRoutine(toQueue));
    }

    private IEnumerator ShowQueueRoutine(List<int> indices)
    {
        for (int i = 0; i < indices.Count; i++)
        {
            TutorialEntry entry = tutorials[indices[i]];

            if (entry.delay > 0f)
                yield return new WaitForSeconds(entry.delay);

            yield return StartCoroutine(ShowSingleTutorial(entry));
        }

        activeRoutine = null;
    }

    private IEnumerator ShowSingleTutorial(TutorialEntry entry)
    {
        if (!CheckUIReferences())
            yield break;

        CacheOriginalPosition();

        promptText.text = GetLocalizedText(entry);

        TMP_FontAsset localizedFont = GetLocalizedFont();
        if (localizedFont != null)
            promptText.font = localizedFont;

        promptCanvasGroup.alpha = 0f;
        promptCanvasGroup.interactable = false;
        promptCanvasGroup.blocksRaycasts = false;

        if (promptRect != null)
        {
            promptRect.localScale = Vector3.one;
            promptRect.SetAsLastSibling();
            promptRect.anchoredPosition = useMotion
                ? originalPosition + new Vector2(0f, -moveDistance)
                : originalPosition;
        }

        yield return StartCoroutine(FadeAndMove(0f, 1f, fadeInTime, true));

        if (entry.displayDuration > 0f)
            yield return new WaitForSeconds(entry.displayDuration);

        yield return StartCoroutine(FadeAndMove(1f, 0f, fadeOutTime, false));
    }

    private IEnumerator FadeAndMove(float fromAlpha, float toAlpha, float duration, bool moveIn)
    {
        if (duration <= 0f)
        {
            SetAlpha(toAlpha);
            yield break;
        }

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

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / duration));

            SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));

            if (promptRect != null && useMotion)
                promptRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            yield return null;
        }

        SetAlpha(toAlpha);

        if (promptRect != null && moveIn)
            promptRect.anchoredPosition = originalPosition;
    }

    private string GetLocalizedText(TutorialEntry entry)
    {
        if (entry.texts == null || entry.texts.Count == 0)
            return string.Empty;

        int index = SettingsManager.Instance != null ? (int)SettingsManager.Instance.CurrentLanguage : 0;

        if (index < 0 || index >= entry.texts.Count || string.IsNullOrEmpty(entry.texts[index]))
            index = 0; // 해당 언어 번역이 비어있으면 0번(Korean)으로 대체

        index = Mathf.Clamp(index, 0, entry.texts.Count - 1);

        return entry.texts[index];
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
    }

    private void CacheOriginalPosition()
    {
        if (promptRect == null || originalPositionCached)
            return;

        originalPosition = promptRect.anchoredPosition;
        originalPositionCached = true;
    }

    private bool CheckUIReferences()
    {
        bool result = true;

        if (promptCanvasGroup == null)
        {
            Debug.LogWarning("[TutorialManager] Prompt Canvas Group이 비어있습니다.");
            result = false;
        }

        if (promptText == null)
        {
            Debug.LogWarning("[TutorialManager] Prompt Text가 비어있습니다.");
            result = false;
        }

        return result;
    }
}
