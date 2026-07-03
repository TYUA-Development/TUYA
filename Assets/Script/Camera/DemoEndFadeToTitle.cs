using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DemoEndFadeToTitle : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";
    public bool activateOnlyOnce = true;

    [Header("Player Control")]
    [Tooltip("기존 호환용 옵션입니다. 켜져 있어도 입력 잠금은 암전 완료 후 적용됩니다.")]
    public bool lockPlayerInputImmediately = false;

    [Tooltip("화면이 완전히 어두워진 뒤 플레이어 입력을 잠급니다.")]
    public bool lockPlayerInputAfterDarkFade = true;

    [Tooltip("입력 잠금 시 플레이어 속도를 0으로 만듭니다.")]
    public bool stopPlayerVelocityWhenLocked = true;

    public float playerLockTime = 9999f;

    [Header("Existing UI References")]
    [Tooltip("기존 검은 화면용 Image. 없으면 런타임 크레딧 UI가 자체 배경을 생성합니다.")]
    public Image fadeImage;

    [Tooltip("기존 감사 문구 TMP Text. 크레딧 모드에서는 숨깁니다.")]
    public TMP_Text messageText;

    [Tooltip("기존 ESC 안내 문구 TMP Text. 크레딧 모드에서는 숨깁니다.")]
    public TMP_Text escHintText;

    [Header("Credits Text")]
    [TextArea(15, 30)]
    public string creditsText =
        "TUYA\n\n" +
        "CREATORS\n\n" +
        "DIRECTOR · GAME DESIGN · ART\n\n" +
        "OH SEUNG-HYUN\n" +
        "오승현\n\n" +
        "LEAD PROGRAMMER\n\n" +
        "JANG JIN-HO\n" +
        "장진호\n\n" +
        "SPECIAL THANKS\n\n" +
        "CONNECT\n" +
        "EXP\n" +
        "C.Y.S\n\n" +
        "And You\n\n" +
        "MUSIC & AUDIO\n\n" +
        "Generated with Suno AI\n\n" +
        "Sound Effects from Freesound\n\n" +
        "FONT\n\n" +
        "Pretendard\n\n" +
        "Licensed under SIL OFL\n\n" +
        "THANK YOU FOR PLAYING\n\n" +
        "T U Y A\n\n" +
        "2026";

    public string escHint = "ESC를 누르면 타이틀로 돌아갑니다";

    [Header("Credits Font")]
    [Tooltip("Pretendard TMP Font Asset을 연결하세요. 예: Assets/TextMesh Pro/Fonts/PretendardVariable SDF.asset")]
    public TMP_FontAsset pretendardFontAsset;

    [Header("Canvas")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [Range(0f, 1f)] public float canvasMatchWidthOrHeight = 0.5f;
    public int creditsCanvasSortingOrder = 5000;

    [Header("Background")]
    [SerializeField] private Color fadeColor = new Color(16f / 255f, 11f / 255f, 34f / 255f, 1f);
    [HideInInspector] public Color backgroundColor = new Color(16f / 255f, 11f / 255f, 34f / 255f, 1f);
    [Range(0f, 1f)] public float targetDarkAlpha = 1f;
    public float startDelay = 0.2f;
    public float darkFadeTime = 3.16f;

    [Header("Scroll")]
    public float scrollSpeed = 42f;
    public float creditsStartY = -80f;
    public float creditsEndPadding = 700f;
    public float creditsFadeInTime = 1.5f;
    public float lineSpacing = 8f;
    public float sectionSpacing = 36f;
    public float paragraphSpacing = 22f;
    public float creditsWidth = 1400f;

    [Header("Credits Compact Layout")]
    public float compactLineSpacing = 4f;
    public float compactNameSpacing = 2f;
    public float compactRoleSpacing = 20f;
    public float compactBodySpacing = 28f;
    public float compactGroupSpacing = 74f;
    public float compactTitleSpacing = 120f;
    public float compactFinalSpacing = 86f;

    [Header("Font Sizes")]
    public float mainTitleFontSize = 60f;
    public float sectionTitleFontSize = 30f;
    public float roleFontSize = 20f;
    public float englishNameFontSize = 22f;
    public float koreanNameFontSize = 18f;
    public float bodyFontSize = 20f;
    public float thankYouFontSize = 32f;
    public float finalTitleFontSize = 20f;
    public float yearFontSize = 20f;

    [Header("ESC Hint")]
    public float escHintAppearDelay = 30f;
    [HideInInspector] public float escHintDelay = 30f;
    [HideInInspector] public float escHintDelayAfterCreditsStart = 15f;
    public float escHintFadeTime = 1f;
    public float escHintFontSize = 18f;
    [Range(0f, 1f)] public float escHintIdleAlpha = 0.2f;
    [HideInInspector] [Range(0f, 1f)] public float escHintNormalAlpha = 0.2f;
    [Range(0f, 1f)] public float escHintHoverAlpha = 1f;
    public float escHintHoverFadeTime = 0.12f;
    public Vector2 escHintAnchoredPosition = new Vector2(-56f, 42f);

    [Header("Title Scene")]
    public string titleSceneName = "TitleScene";

    [Tooltip("ESC를 누른 뒤 완전 검정으로 짧게 페이드하고 타이틀로 이동합니다.")]
    public bool fadeToFullBlackBeforeTitle = true;

    public float titleFadeTime = 4.2f;
    [SerializeField] private Color titleFadeColor = new Color(16f / 255f, 11f / 255f, 34f / 255f, 1f);
    [HideInInspector] public Color titleTransitionColor = new Color(16f / 255f, 11f / 255f, 34f / 255f, 1f);
    public float titleSceneRevealTime = 3.0f;
    [Range(0f, 1f)] public float titleTransitionCreditsAlpha = 0f;
    public bool fadeOutAllAudioBeforeTitle = true;

    [Header("Audio Optional")]
    public AudioSource endStartAudio;
    public AudioSource messageAudio;
    public AudioSource[] titleTransitionAudioSources;

    [Header("Time")]
    public bool useUnscaledTime = false;

    [Header("State")]
    public bool hasActivated;
    public bool isEnding;
    public bool canPressEsc;
    public bool playerLocked;

    private Canvas creditsCanvas;
    private Image runtimeBackgroundImage;
    private Image titleTransitionImage;
    private RectTransform creditsRoot;
    private CanvasGroup creditsCanvasGroup;
    private TMP_Text runtimeEscHintText;
    private RectTransform runtimeEscHintRect;
    private readonly List<TMP_Text> runtimeCreditTexts = new List<TMP_Text>();

    private Coroutine endCoroutine;
    private Coroutine titleCoroutine;
    private Coroutine scrollCoroutine;
    private Coroutine escHintCoroutine;

    private PlayerController cachedPlayerController;
    private Rigidbody2D cachedPlayerRigidbody;

    private void Awake()
    {
        TryAssignPretendardFontAsset();
        SetupInitialUI();
        CreateRuntimeCreditsUI();
    }

    private void Update()
    {
        if (!canPressEsc)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            RequestGoToTitle();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        if (activateOnlyOnce && hasActivated)
            return;

        hasActivated = true;

        CachePlayerReferences(collision);

        if (endCoroutine != null)
            StopCoroutine(endCoroutine);

        endCoroutine = StartCoroutine(EndSequenceRoutine());
    }

    private void CachePlayerReferences(Collider2D collision)
    {
        cachedPlayerController = collision.GetComponentInParent<PlayerController>();
        cachedPlayerRigidbody = collision.GetComponentInParent<Rigidbody2D>();
    }

    private void SetupInitialUI()
    {
        SetImageAlpha(fadeImage, 0f);
        SetTextAlpha(messageText, 0f);
        SetTextAlpha(escHintText, 0f);
    }

    private void CreateRuntimeCreditsUI()
    {
        if (creditsCanvas != null)
            return;

        GameObject canvasObject = new GameObject("RuntimeEndingCreditsCanvas");
        creditsCanvas = canvasObject.AddComponent<Canvas>();
        creditsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        creditsCanvas.sortingOrder = creditsCanvasSortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = canvasMatchWidthOrHeight;

        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystemExists();

        runtimeBackgroundImage = CreateImage("Background", creditsCanvas.transform, fadeColor);
        RectTransform backgroundRect = runtimeBackgroundImage.rectTransform;
        StretchToFill(backgroundRect);
        SetImageAlpha(runtimeBackgroundImage, 0f);

        GameObject creditsRootObject = new GameObject("CreditsRoot");
        creditsRootObject.transform.SetParent(creditsCanvas.transform, false);
        creditsRoot = creditsRootObject.AddComponent<RectTransform>();
        creditsRoot.anchorMin = new Vector2(0.5f, 0f);
        creditsRoot.anchorMax = new Vector2(0.5f, 0f);
        creditsRoot.pivot = new Vector2(0.5f, 1f);
        creditsRoot.sizeDelta = new Vector2(creditsWidth, 4000f);
        creditsRoot.anchoredPosition = new Vector2(0f, creditsStartY);

        creditsCanvasGroup = creditsRootObject.AddComponent<CanvasGroup>();
        creditsCanvasGroup.alpha = 0f;
        creditsCanvasGroup.blocksRaycasts = false;

        BuildCreditsTextObjects();
        CreateEscHintText();
        CreateTitleTransitionOverlay();

        creditsCanvas.gameObject.SetActive(false);
    }

    private void BuildCreditsTextObjects()
    {
        runtimeCreditTexts.Clear();

        string[] lines = creditsText.Replace("\r\n", "\n").Split('\n');
        float y = 0f;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            CreditLineStyle style = GetLineStyle(line);
            TMP_Text text = CreateText("CreditLine_" + i, creditsRoot, line, style.fontSize);
            text.fontStyle = style.fontStyle;
            text.rectTransform.anchoredPosition = new Vector2(0f, y);

            runtimeCreditTexts.Add(text);

            y -= style.height + compactLineSpacing + GetCompactSpacingAfter(line);
        }

        creditsRoot.sizeDelta = new Vector2(creditsWidth, Mathf.Abs(y) + creditsEndPadding);
    }

    private void CreateEscHintText()
    {
        runtimeEscHintText = CreateText("EscHint", creditsCanvas.transform, escHint, escHintFontSize);
        runtimeEscHintText.alignment = TextAlignmentOptions.Right;
        runtimeEscHintText.raycastTarget = true;
        SetTextAlpha(runtimeEscHintText, 0f);

        runtimeEscHintRect = runtimeEscHintText.rectTransform;
        runtimeEscHintRect.anchorMin = new Vector2(1f, 0f);
        runtimeEscHintRect.anchorMax = new Vector2(1f, 0f);
        runtimeEscHintRect.pivot = new Vector2(1f, 0f);
        runtimeEscHintRect.sizeDelta = new Vector2(560f, 42f);
        runtimeEscHintRect.anchoredPosition = escHintAnchoredPosition;

        EndingEscHintHover hover = runtimeEscHintText.gameObject.AddComponent<EndingEscHintHover>();
        hover.Initialize(this);
    }

    private void CreateTitleTransitionOverlay()
    {
        titleTransitionImage = CreateImage("TitleTransitionOverlay", creditsCanvas.transform, titleFadeColor);
        RectTransform transitionRect = titleTransitionImage.rectTransform;
        StretchToFill(transitionRect);
        SetImageAlpha(titleTransitionImage, 0f);
        titleTransitionImage.raycastTarget = false;
        titleTransitionImage.transform.SetAsLastSibling();
    }

    private IEnumerator EndSequenceRoutine()
    {
        isEnding = true;
        canPressEsc = false;
        playerLocked = false;

        HideExistingEndingTexts();
        EnsureRuntimeCreditsUIReady();

        creditsCanvas.gameObject.SetActive(true);
        creditsCanvas.sortingOrder = creditsCanvasSortingOrder;
        ApplyFadeColorToBackground();
        creditsRoot.anchoredPosition = new Vector2(0f, creditsStartY);
        creditsCanvasGroup.alpha = 0f;
        SetTextAlpha(runtimeEscHintText, 0f);

        PlayAudio(endStartAudio);

        if (startDelay > 0f)
            yield return Wait(startDelay);

        yield return StartCoroutine(FadeImageAlpha(
            GetEndingFadeImage(),
            GetImageAlpha(GetEndingFadeImage()),
            targetDarkAlpha,
            darkFadeTime
        ));

        if (lockPlayerInputAfterDarkFade || lockPlayerInputImmediately)
            LockPlayerNow();

        PlayAudio(messageAudio);

        if (scrollCoroutine != null)
            StopCoroutine(scrollCoroutine);

        if (escHintCoroutine != null)
            StopCoroutine(escHintCoroutine);

        scrollCoroutine = StartCoroutine(ScrollCreditsRoutine());
        escHintCoroutine = StartCoroutine(ShowEscHintRoutine());

        endCoroutine = null;
    }

    private void EnsureRuntimeCreditsUIReady()
    {
        if (creditsCanvas == null)
            CreateRuntimeCreditsUI();
    }

    private void HideExistingEndingTexts()
    {
        SetTextAlpha(messageText, 0f);
        SetTextAlpha(escHintText, 0f);
    }

    private IEnumerator ScrollCreditsRoutine()
    {
        yield return StartCoroutine(FadeCanvasGroupAlpha(creditsCanvasGroup, 0f, 1f, creditsFadeInTime));

        float targetY = referenceResolution.y + creditsRoot.sizeDelta.y + creditsEndPadding;

        while (creditsRoot.anchoredPosition.y < targetY)
        {
            Vector2 position = creditsRoot.anchoredPosition;
            position.y += scrollSpeed * DeltaTime();
            creditsRoot.anchoredPosition = position;

            yield return null;
        }

        creditsRoot.anchoredPosition = new Vector2(creditsRoot.anchoredPosition.x, targetY);
        scrollCoroutine = null;
    }

    private IEnumerator ShowEscHintRoutine()
    {
        if (escHintAppearDelay > 0f)
            yield return Wait(escHintAppearDelay);

        canPressEsc = true;

        yield return StartCoroutine(FadeTextAlpha(
            runtimeEscHintText,
            GetTextAlpha(runtimeEscHintText),
            escHintIdleAlpha,
            escHintFadeTime
        ));

        escHintCoroutine = null;
    }

    private void LockPlayerNow()
    {
        if (playerLocked)
            return;

        playerLocked = true;

        if (cachedPlayerController != null)
            cachedPlayerController.LockPlayerInput(playerLockTime);

        if (stopPlayerVelocityWhenLocked && cachedPlayerRigidbody != null)
        {
            cachedPlayerRigidbody.velocity = Vector2.zero;
            cachedPlayerRigidbody.angularVelocity = 0f;
        }
    }

    private IEnumerator GoToTitleRoutine()
    {
        if (escHintCoroutine != null)
            StopCoroutine(escHintCoroutine);

        if (fadeToFullBlackBeforeTitle)
        {
            yield return StartCoroutine(FadeOutToTitleRoutine());
            yield break;
        }

        SceneManager.LoadScene(titleSceneName);
    }

    private void RequestGoToTitle()
    {
        if (!canPressEsc || titleCoroutine != null)
            return;

        canPressEsc = false;
        titleCoroutine = StartCoroutine(GoToTitleRoutine());
    }

    private IEnumerator FadeOutToTitleRoutine()
    {
        EnsureRuntimeCreditsUIReady();

        Image persistentTransitionImage = CreatePersistentTitleTransitionImage();
        SetImageAlpha(persistentTransitionImage, 0f);

        AudioSource[] sources = GetTitleTransitionAudioSources();
        float[] startVolumes = new float[sources.Length];

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null)
                startVolumes[i] = sources[i].volume;
        }

        float startEscHintAlpha = GetTextAlpha(runtimeEscHintText);
        float timer = 0f;

        while (timer < titleFadeTime)
        {
            timer += DeltaTime();
            float t = Smooth01(Mathf.Clamp01(timer / titleFadeTime));

            SetImageAlpha(persistentTransitionImage, Mathf.Lerp(0f, 1f, t));

            SetTextAlpha(runtimeEscHintText, Mathf.Lerp(startEscHintAlpha, 0f, t));

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null)
                    sources[i].volume = Mathf.Lerp(startVolumes[i], 0f, t);
            }

            yield return null;
        }

        SetImageAlpha(persistentTransitionImage, 1f);

        SetTextAlpha(runtimeEscHintText, 0f);

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null)
                sources[i].volume = 0f;
        }

        TitleTransitionSceneFader fader = persistentTransitionImage.GetComponentInParent<TitleTransitionSceneFader>();
        fader.Begin(titleSceneName, persistentTransitionImage, titleSceneRevealTime, useUnscaledTime);
    }

    private Image GetEndingFadeImage()
    {
        return runtimeBackgroundImage != null ? runtimeBackgroundImage : fadeImage;
    }

    private void ApplyFadeColorToBackground()
    {
        if (runtimeBackgroundImage == null)
            return;

        Color color = fadeColor;
        color.a = runtimeBackgroundImage.color.a;
        runtimeBackgroundImage.color = color;
    }

    private Image CreatePersistentTitleTransitionImage()
    {
        GameObject canvasObject = new GameObject("PersistentTitleTransitionCanvas");
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = creditsCanvasSortingOrder + 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = canvasMatchWidthOrHeight;

        canvasObject.AddComponent<TitleTransitionSceneFader>();

        Image image = CreateImage("TitleSceneRevealOverlay", canvasObject.transform, titleFadeColor);
        StretchToFill(image.rectTransform);
        image.raycastTarget = false;
        return image;
    }

    private Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private TMP_Text CreateText(string objectName, Transform parent, string value, float fontSize)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        if (pretendardFontAsset != null)
            text.font = pretendardFontAsset;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(creditsWidth, Mathf.Max(fontSize * 1.5f, 36f));

        return text;
    }

    private void TryAssignPretendardFontAsset()
    {
        if (pretendardFontAsset != null)
            return;

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("Pretendard t:TMP_FontAsset");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

            if (asset != null)
            {
                pretendardFontAsset = asset;
                return;
            }
        }
#endif
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("RuntimeEndingCreditsEventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void SetEscHintHoverState(bool isHovering)
    {
        if (runtimeEscHintText == null || !canPressEsc)
            return;

        float targetAlpha = isHovering ? escHintHoverAlpha : escHintIdleAlpha;

        if (escHintCoroutine != null)
            StopCoroutine(escHintCoroutine);

        escHintCoroutine = StartCoroutine(FadeTextAlpha(
            runtimeEscHintText,
            GetTextAlpha(runtimeEscHintText),
            targetAlpha,
            escHintHoverFadeTime
        ));
    }

    private AudioSource[] GetTitleTransitionAudioSources()
    {
        if (!fadeOutAllAudioBeforeTitle)
            return titleTransitionAudioSources != null ? titleTransitionAudioSources : new AudioSource[0];

        AudioSource[] allSources = FindObjectsOfType<AudioSource>();
        List<AudioSource> playingSources = new List<AudioSource>();

        for (int i = 0; i < allSources.Length; i++)
        {
            if (allSources[i] != null && allSources[i].isPlaying)
                playingSources.Add(allSources[i]);
        }

        if (titleTransitionAudioSources != null)
        {
            for (int i = 0; i < titleTransitionAudioSources.Length; i++)
            {
                AudioSource source = titleTransitionAudioSources[i];

                if (source != null && !playingSources.Contains(source))
                    playingSources.Add(source);
            }
        }

        return playingSources.ToArray();
    }

    private void StretchToFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private CreditLineStyle GetLineStyle(string line)
    {
        if (line == "TUYA")
            return new CreditLineStyle(mainTitleFontSize, FontStyles.Bold);

        if (line == "CREATORS" || line == "SPECIAL THANKS" || line == "MUSIC & AUDIO" || line == "FONT")
            return new CreditLineStyle(sectionTitleFontSize, FontStyles.Bold);

        if (line == "DIRECTOR · GAME DESIGN · ART" || line == "LEAD PROGRAMMER")
            return new CreditLineStyle(roleFontSize, FontStyles.UpperCase);

        if (line == "OH SEUNG-HYUN" || line == "JANG JIN-HO")
            return new CreditLineStyle(englishNameFontSize, FontStyles.Bold);

        if (line == "오승현" || line == "장진호")
            return new CreditLineStyle(koreanNameFontSize, FontStyles.Normal);

        if (line == "THANK YOU FOR PLAYING")
            return new CreditLineStyle(thankYouFontSize, FontStyles.Bold);

        if (line == "T U Y A")
            return new CreditLineStyle(finalTitleFontSize, FontStyles.Normal);

        if (line == "2026")
            return new CreditLineStyle(yearFontSize, FontStyles.Normal);

        return new CreditLineStyle(bodyFontSize, FontStyles.Normal);
    }

    private float GetExtraSpacingAfter(string line)
    {
        if (line == "TUYA" || line == "CREATORS" || line == "SPECIAL THANKS" || line == "MUSIC & AUDIO" || line == "FONT")
            return sectionSpacing;

        return 0f;
    }

    private float GetCompactSpacingAfter(string line)
    {
        if (line == "TUYA")
            return compactTitleSpacing;

        if (line == "CREATORS" || line == "SPECIAL THANKS" || line == "MUSIC & AUDIO" || line == "FONT")
            return compactBodySpacing;

        if (line == "DIRECTOR · GAME DESIGN · ART" || line == "LEAD PROGRAMMER")
            return compactRoleSpacing;

        if (line == "OH SEUNG-HYUN" || line == "JANG JIN-HO")
            return compactNameSpacing;

        if (line == "오승현" || line == "장진호" || line == "And You" || line == "Sound Effects from Freesound" || line == "Licensed under SIL OFL")
            return compactGroupSpacing;

        if (line == "CONNECT" || line == "EXP" || line == "C.Y.S")
            return compactNameSpacing;

        if (line == "Generated with Suno AI" || line == "Pretendard")
            return compactBodySpacing;

        if (line == "THANK YOU FOR PLAYING")
            return compactFinalSpacing;

        if (line == "T U Y A")
            return compactBodySpacing;

        return 0f;
    }

    private IEnumerator FadeImageAlpha(Image image, float from, float to, float duration)
    {
        if (image == null)
            yield break;

        if (duration <= 0f)
        {
            SetImageAlpha(image, to);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += DeltaTime();
            float t = Smooth01(Mathf.Clamp01(timer / duration));
            SetImageAlpha(image, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetImageAlpha(image, to);
    }

    private IEnumerator FadeTextAlpha(TMP_Text text, float from, float to, float duration)
    {
        if (text == null)
            yield break;

        if (duration <= 0f)
        {
            SetTextAlpha(text, to);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += DeltaTime();
            float t = Smooth01(Mathf.Clamp01(timer / duration));
            SetTextAlpha(text, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetTextAlpha(text, to);
    }

    private IEnumerator FadeCanvasGroupAlpha(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += DeltaTime();
            float t = Smooth01(Mathf.Clamp01(timer / duration));
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private float GetImageAlpha(Image image)
    {
        if (image == null)
            return 0f;

        return image.color.a;
    }

    private void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
            return;

        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private float GetTextAlpha(TMP_Text text)
    {
        if (text == null)
            return 0f;

        return text.color.a;
    }

    private void PlayAudio(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
        audioSource.Play();
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private IEnumerator Wait(float seconds)
    {
        if (useUnscaledTime)
        {
            float timer = 0f;

            while (timer < seconds)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    private float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private class EndingEscHintHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private DemoEndFadeToTitle owner;

        public void Initialize(DemoEndFadeToTitle owner)
        {
            this.owner = owner;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner != null)
                owner.SetEscHintHoverState(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (owner != null)
                owner.SetEscHintHoverState(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner != null)
                owner.RequestGoToTitle();
        }
    }

    private class TitleTransitionSceneFader : MonoBehaviour
    {
        public void Begin(string sceneName, Image image, float revealTime, bool useUnscaledTime)
        {
            StartCoroutine(LoadSceneAndReveal(sceneName, image, revealTime, useUnscaledTime));
        }

        private IEnumerator LoadSceneAndReveal(string sceneName, Image image, float revealTime, bool useUnscaledTime)
        {
            SceneManager.LoadScene(sceneName);
            yield return null;

            if (image == null || revealTime <= 0f)
            {
                Destroy(gameObject);
                yield break;
            }

            float timer = 0f;

            while (timer < revealTime)
            {
                timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(timer / revealTime);
                t = t * t * (3f - 2f * t);

                Color color = image.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                image.color = color;

                yield return null;
            }

            Destroy(gameObject);
        }
    }

    private struct CreditLineStyle
    {
        public readonly float fontSize;
        public readonly FontStyles fontStyle;
        public readonly float height;

        public CreditLineStyle(float fontSize, FontStyles fontStyle)
        {
            this.fontSize = fontSize;
            this.fontStyle = fontStyle;
            height = Mathf.Max(fontSize * 1.5f, 36f);
        }
    }
}
