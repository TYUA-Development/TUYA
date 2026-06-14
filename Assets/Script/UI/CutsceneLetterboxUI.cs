using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneLetterboxUI : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas targetCanvas;

    [Tooltip("Canvas가 없으면 자동으로 새 Canvas를 만듭니다.")]
    public bool createCanvasAutomatically = true;

    [Tooltip("다른 UI보다 위에 보이게 하는 값")]
    public int sortingOrder = 500;

    [Header("Bars")]
    public RectTransform topBar;
    public RectTransform bottomBar;

    [Tooltip("검정바 높이")]
    public float barHeight = 90f;

    [Tooltip("검정바 색상")]
    public Color barColor = Color.black;

    [Header("Animation")]
    public float defaultShowTime = 0.45f;
    public float defaultHideTime = 0.45f;

    [Header("State")]
    public bool isShowing;

    private Coroutine barCoroutine;

    private void Awake()
    {
        SetupCanvas();
        SetupBars();
        SetBarHeight(0f);
    }

    private void SetupCanvas()
    {
        if (targetCanvas != null)
            return;

        if (!createCanvasAutomatically)
            return;

        GameObject canvasObject = new GameObject("CutsceneLetterboxCanvas");
        targetCanvas = canvasObject.AddComponent<Canvas>();
        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        targetCanvas.sortingOrder = sortingOrder;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        DontDestroyOnLoad(canvasObject);
    }

    private void SetupBars()
    {
        if (targetCanvas == null)
            return;

        if (topBar == null)
            topBar = CreateBar("Letterbox_Top", true);

        if (bottomBar == null)
            bottomBar = CreateBar("Letterbox_Bottom", false);
    }

    private RectTransform CreateBar(string objectName, bool isTop)
    {
        GameObject barObject = new GameObject(objectName);
        barObject.transform.SetParent(targetCanvas.transform, false);

        RectTransform rect = barObject.AddComponent<RectTransform>();
        Image image = barObject.AddComponent<Image>();
        image.color = barColor;
        image.raycastTarget = false;

        if (isTop)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
        }

        rect.sizeDelta = new Vector2(0f, 0f);

        return rect;
    }

    public void ShowBars()
    {
        ShowBars(defaultShowTime);
    }

    public void ShowBars(float duration)
    {
        isShowing = true;

        if (barCoroutine != null)
            StopCoroutine(barCoroutine);

        barCoroutine = StartCoroutine(AnimateBars(GetCurrentBarHeight(), barHeight, duration));
    }

    public void HideBars()
    {
        HideBars(defaultHideTime);
    }

    public void HideBars(float duration)
    {
        isShowing = false;

        if (barCoroutine != null)
            StopCoroutine(barCoroutine);

        barCoroutine = StartCoroutine(AnimateBars(GetCurrentBarHeight(), 0f, duration));
    }

    private IEnumerator AnimateBars(float fromHeight, float toHeight, float duration)
    {
        if (duration <= 0f)
        {
            SetBarHeight(toHeight);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float height = Mathf.Lerp(fromHeight, toHeight, smoothT);

            SetBarHeight(height);

            yield return null;
        }

        SetBarHeight(toHeight);
        barCoroutine = null;
    }

    private void SetBarHeight(float height)
    {
        if (topBar != null)
            topBar.sizeDelta = new Vector2(0f, height);

        if (bottomBar != null)
            bottomBar.sizeDelta = new Vector2(0f, height);
    }

    private float GetCurrentBarHeight()
    {
        if (topBar == null)
            return 0f;

        return topBar.sizeDelta.y;
    }
}