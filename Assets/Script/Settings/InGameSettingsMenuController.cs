using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InGameSettingsMenuController : MonoBehaviour
{
    private const string TitleSceneName = "TitleScene";
    private const string TitleReturnText = "\uD0C0\uC774\uD2C0\uB85C \uC774\uB3D9";
    public const string SettingsMenuPrefabPath = "Assets/Prefabs/UI/SettingsMenuPrefab.prefab";

    [Header("Prefab")]
    [SerializeField] private GameObject settingsMenuPrefab;

    [Header("Pause")]
    [SerializeField] private bool pauseWithTimeScale = true;

    [Header("Title Return Button")]
    [SerializeField] private Vector2 titleReturnButtonSize = new Vector2(260f, 54f);
    [SerializeField] private Vector2 titleReturnButtonOffset = new Vector2(-90f, 50f);
    [SerializeField, Range(0f, 1f)] private float titleReturnNormalAlpha = 0.2f;
    [SerializeField, Range(0f, 1f)] private float titleReturnHoverAlpha = 1f;
    [SerializeField] private float titleReturnHoverFadeTime = 0.12f;
    [SerializeField] private float titleReturnFontSize = 18f;

    [Header("Title Transition")]
    [SerializeField] private Color fadeColor = new Color(16f / 255f, 11f / 255f, 34f / 255f, 1f);
    [SerializeField] private float titleFadeOutTime = 2.2f;
    [SerializeField] private float titleAudioFadeOutTime = 2.0f;
    [SerializeField] private float titleSceneRevealTime = 2.6f;
    [SerializeField] private int menuSortingOrder = 10000;
    [SerializeField] private int transitionSortingOrder = 11000;

    public static bool Exists { get; private set; }

    private GameObject menuInstance;
    private SettingsUI settingsUI;
    private Canvas menuCanvas;
    private Image fadeImage;
    private bool isOpen;
    private bool isTransitioningToTitle;
    private float previousTimeScale = 1f;
    private PlayerController[] cachedPlayerControllers;
    private bool[] cachedPlayerControllerEnabled;

    private void Awake()
    {
        Exists = true;
        ResolveSettingsMenuPrefab();
    }

    private void OnDestroy()
    {
        if (Exists)
            Exists = false;

        RestoreTimeScale();
        UnlockPlayerInput();
    }

    private void Update()
    {
        if (isTransitioningToTitle)
            return;

        if (SettingsUI.ShouldBlockEscapeNavigation)
            return;

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (IsEndingSequenceActive())
            return;

        if (isOpen)
            BackOrCloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        if (isOpen || isTransitioningToTitle)
            return;

        if (!ResolveSettingsMenuPrefab())
        {
            Debug.LogWarning(
                "[InGameSettingsMenuController] SettingsMenuPrefab is not assigned and was not found at " +
                SettingsMenuPrefabPath +
                ". Assign the prefab in the Inspector or create it at that path."
            );
            return;
        }

        EnsureEventSystemExists();

        menuInstance = Instantiate(settingsMenuPrefab, transform, false);
        menuInstance.name = settingsMenuPrefab.name + "_InGame";
        menuInstance.SetActive(true);

        DisableTitleOnlyControllers(menuInstance);
        CachePrefabReferences();
        PreparePrefabForInGame();

        if (settingsUI != null)
            settingsUI.gameObject.SetActive(true);

        AddCloseButtonListener();
        AddTitleReturnButton();
        BuildFadeOverlay();

        isOpen = true;
        LockPlayerInput();
        PauseGame();
    }

    public bool ResolveSettingsMenuPrefab()
    {
        if (settingsMenuPrefab != null)
            return true;

#if UNITY_EDITOR
        settingsMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsMenuPrefabPath);
#endif

        return settingsMenuPrefab != null;
    }

    public void CloseMenu()
    {
        if (!isOpen)
            return;

        isOpen = false;
        RestoreTimeScale();
        UnlockPlayerInput();

        if (menuInstance != null)
            Destroy(menuInstance);

        menuInstance = null;
        settingsUI = null;
        menuCanvas = null;
        fadeImage = null;
    }

    public void BackOrCloseMenu()
    {
        if (!isOpen)
            return;

        if (IsSettingsSubPanelOpen())
        {
            ShowSettingsMainPanel();
            return;
        }

        CloseMenu();
    }

    public void ReturnToTitle()
    {
        if (isTransitioningToTitle)
            return;

        StartCoroutine(ReturnToTitleRoutine());
    }

    private IEnumerator ReturnToTitleRoutine()
    {
        isTransitioningToTitle = true;

        if (fadeImage == null)
            BuildFadeOverlay();

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.transform.SetAsLastSibling();
            SetImageAlpha(fadeImage, 0f);
        }

        DisableMenuInteraction();

        AudioListener.pause = false;

        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        float[] startVolumes = new float[audioSources.Length];

        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
                startVolumes[i] = audioSources[i].volume;
        }

        float timer = 0f;
        float duration = Mathf.Max(titleFadeOutTime, titleAudioFadeOutTime);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float fadeT = titleFadeOutTime <= 0f ? 1f : Mathf.Clamp01(timer / titleFadeOutTime);
            float audioT = titleAudioFadeOutTime <= 0f ? 1f : Mathf.Clamp01(timer / titleAudioFadeOutTime);

            SetImageAlpha(fadeImage, Mathf.SmoothStep(0f, 1f, fadeT));

            for (int i = 0; i < audioSources.Length; i++)
            {
                if (audioSources[i] != null)
                    audioSources[i].volume = Mathf.Lerp(startVolumes[i], 0f, audioT);
            }

            yield return null;
        }

        RestoreTimeScale();
        UnlockPlayerInput();
        CreatePersistentTitleRevealOverlay();
        SceneManager.LoadScene(TitleSceneName);
    }

    private void CachePrefabReferences()
    {
        settingsUI = menuInstance.GetComponentInChildren<SettingsUI>(true);
        menuCanvas = menuInstance.GetComponentInChildren<Canvas>(true);

        if (settingsUI == null)
            Debug.LogWarning("[InGameSettingsMenuController] SettingsUI was not found in the settings prefab.");

        if (menuCanvas == null)
            Debug.LogWarning("[InGameSettingsMenuController] Canvas was not found in the settings prefab.");
    }

    private void PreparePrefabForInGame()
    {
        DisablePrefabBrightnessOverlays();
        PrepareCanvasesForInGame();
        PrepareSettingsCanvasGroup();
    }

    private void DisablePrefabBrightnessOverlays()
    {
        if (menuInstance == null)
            return;

        Transform[] children = menuInstance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == "BrightnessOverlay")
                child.gameObject.SetActive(false);
        }
    }

    private void PrepareCanvasesForInGame()
    {
        if (menuInstance == null)
            return;

        Canvas[] canvases = menuInstance.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
                continue;

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = menuSortingOrder + i;
            canvas.worldCamera = null;
        }

        menuCanvas = ResolveMenuCanvas(canvases);
    }

    private Canvas ResolveMenuCanvas(Canvas[] canvases)
    {
        if (settingsUI != null)
        {
            Canvas settingsCanvas = settingsUI.GetComponentInParent<Canvas>(true);
            if (settingsCanvas != null && settingsCanvas.gameObject.activeInHierarchy)
                return settingsCanvas;

            if (settingsCanvas != null && settingsCanvas.gameObject.name != "BrightnessOverlay")
                return settingsCanvas;
        }

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.gameObject.name != "BrightnessOverlay")
                return canvas;
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }

    private void PrepareSettingsCanvasGroup()
    {
        if (settingsUI == null)
            return;

        CanvasGroup group = settingsUI.GetComponent<CanvasGroup>();
        if (group == null)
            group = settingsUI.gameObject.AddComponent<CanvasGroup>();

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void AddCloseButtonListener()
    {
        if (settingsUI == null || settingsUI.mainCloseButton == null)
            return;

        settingsUI.mainCloseButton.onClick.AddListener(CloseMenu);
    }

    private void AddTitleReturnButton()
    {
        Canvas targetCanvas = menuCanvas != null ? menuCanvas : menuInstance.GetComponentInParent<Canvas>();
        if (targetCanvas == null)
        {
            Debug.LogWarning("[InGameSettingsMenuController] Title return button was not created because no Canvas was found.");
            return;
        }

        GameObject buttonObject = settingsUI != null && settingsUI.mainCloseButton != null
            ? Instantiate(settingsUI.mainCloseButton.gameObject)
            : new GameObject("InGameTitleReturnButton");

        buttonObject.name = "InGameTitleReturnButton";
        buttonObject.transform.SetParent(targetCanvas.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect == null)
            rect = buttonObject.AddComponent<RectTransform>();

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = titleReturnButtonOffset;
        rect.sizeDelta = titleReturnButtonSize;

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            button = buttonObject.AddComponent<Button>();

        DisablePersistentButtonListeners(button);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ReturnToTitle);

        TextMeshProUGUI label = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            label = CreateTitleReturnText(buttonObject.transform);
            CopyFontFromPrefab(label);
        }

        label.text = TitleReturnText;
        label.fontSize = titleReturnFontSize;
        MakeButtonImagesTransparent(buttonObject);

        SettingsMenuButtonAlpha[] titleAlphaComponents = buttonObject.GetComponentsInChildren<SettingsMenuButtonAlpha>(true);
        for (int i = 0; i < titleAlphaComponents.Length; i++)
            Destroy(titleAlphaComponents[i]);

        InGameTitleReturnButton hover = buttonObject.AddComponent<InGameTitleReturnButton>();
        hover.Initialize(this, titleReturnNormalAlpha, titleReturnHoverAlpha, titleReturnHoverFadeTime);
    }

    private void DisablePersistentButtonListeners(Button button)
    {
        if (button == null)
            return;

        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
    }

    private void MakeButtonImagesTransparent(GameObject buttonObject)
    {
        if (buttonObject == null)
            return;

        Image[] images = buttonObject.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
                continue;

            Color color = image.color;
            color.a = 0f;
            image.color = color;
        }
    }

    private bool IsSettingsSubPanelOpen()
    {
        if (settingsUI == null)
            return false;

        return IsActive(settingsUI.audioPanel) ||
               IsActive(settingsUI.graphicsPanel) ||
               IsActive(settingsUI.controlPanel);
    }

    private void ShowSettingsMainPanel()
    {
        if (settingsUI == null)
            return;

        if (settingsUI.settingsMainPanel != null)
            settingsUI.settingsMainPanel.SetActive(true);

        if (settingsUI.audioPanel != null)
            settingsUI.audioPanel.SetActive(false);

        if (settingsUI.graphicsPanel != null)
            settingsUI.graphicsPanel.SetActive(false);

        if (settingsUI.controlPanel != null)
            settingsUI.controlPanel.SetActive(false);
    }

    private bool IsActive(GameObject target)
    {
        return target != null && target.activeSelf;
    }

    private TextMeshProUGUI CreateTitleReturnText(Transform parent)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = TitleReturnText;
        text.fontSize = titleReturnFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableWordWrapping = false;

        return text;
    }

    private void CopyFontFromPrefab(TextMeshProUGUI target)
    {
        if (target == null || menuInstance == null)
            return;

        TMP_Text[] texts = menuInstance.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text source = texts[i];
            if (source == null || source.font == null)
                continue;

            target.font = source.font;
            target.fontMaterial = source.fontMaterial;
            return;
        }
    }

    private void BuildFadeOverlay()
    {
        Canvas targetCanvas = menuCanvas != null ? menuCanvas : menuInstance != null ? menuInstance.GetComponentInChildren<Canvas>(true) : null;
        if (targetCanvas == null)
            return;

        GameObject overlayObject = new GameObject("InGameTitleFadeOverlay");
        overlayObject.transform.SetParent(targetCanvas.transform, false);

        RectTransform rect = overlayObject.AddComponent<RectTransform>();
        StretchToFill(rect);

        fadeImage = overlayObject.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = true;
        SetImageAlpha(fadeImage, 0f);
        fadeImage.gameObject.SetActive(false);

        Canvas overlayCanvas = overlayObject.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = transitionSortingOrder;
        overlayObject.AddComponent<GraphicRaycaster>();
    }

    private void DisableMenuInteraction()
    {
        if (menuInstance == null)
            return;

        CanvasGroup group = menuInstance.GetComponent<CanvasGroup>();
        if (group == null)
            group = menuInstance.AddComponent<CanvasGroup>();

        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void DisableTitleOnlyControllers(GameObject root)
    {
        TitleUIController[] titleControllers = root.GetComponentsInChildren<TitleUIController>(true);
        for (int i = 0; i < titleControllers.Length; i++)
            titleControllers[i].enabled = false;
    }

    private void PauseGame()
    {
        if (!pauseWithTimeScale)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    private void RestoreTimeScale()
    {
        if (!pauseWithTimeScale)
            return;

        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        AudioListener.pause = false;
    }

    private void LockPlayerInput()
    {
        cachedPlayerControllers = FindObjectsOfType<PlayerController>();
        cachedPlayerControllerEnabled = new bool[cachedPlayerControllers.Length];

        for (int i = 0; i < cachedPlayerControllers.Length; i++)
        {
            PlayerController controller = cachedPlayerControllers[i];
            if (controller == null)
                continue;

            cachedPlayerControllerEnabled[i] = controller.enabled;
            controller.enabled = false;

            Rigidbody2D body = controller.GetComponent<Rigidbody2D>();
            if (body != null)
                body.velocity = Vector2.zero;
        }
    }

    private void UnlockPlayerInput()
    {
        if (cachedPlayerControllers == null || cachedPlayerControllerEnabled == null)
            return;

        for (int i = 0; i < cachedPlayerControllers.Length; i++)
        {
            if (cachedPlayerControllers[i] != null)
                cachedPlayerControllers[i].enabled = cachedPlayerControllerEnabled[i];
        }

        cachedPlayerControllers = null;
        cachedPlayerControllerEnabled = null;
    }

    private bool IsEndingSequenceActive()
    {
        DemoEndFadeToTitle[] endings = FindObjectsOfType<DemoEndFadeToTitle>();
        for (int i = 0; i < endings.Length; i++)
        {
            DemoEndFadeToTitle ending = endings[i];
            if (ending != null && (ending.isEnding || ending.hasActivated))
                return true;
        }

        return false;
    }

    private void CreatePersistentTitleRevealOverlay()
    {
        GameObject canvasObject = new GameObject("PersistentInGameTitleRevealCanvas");
        DontDestroyOnLoad(canvasObject);

        Canvas revealCanvas = canvasObject.AddComponent<Canvas>();
        revealCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        revealCanvas.sortingOrder = transitionSortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image image = canvasObject.AddComponent<Image>();
        image.color = fadeColor;
        image.raycastTarget = false;

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        StretchToFill(rect);
        SetImageAlpha(image, 1f);

        InGameTitleSceneRevealFader fader = canvasObject.AddComponent<InGameTitleSceneRevealFader>();
        fader.Begin(image, titleSceneRevealTime);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void StretchToFill(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("RuntimeInGameSettingsEventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private class InGameTitleSceneRevealFader : MonoBehaviour
    {
        public void Begin(Image image, float duration)
        {
            StartCoroutine(FadeRoutine(image, duration));
        }

        private IEnumerator FadeRoutine(Image image, float duration)
        {
            yield return null;

            if (image == null || duration <= 0f)
            {
                Destroy(gameObject);
                yield break;
            }

            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);
                float smooth = t * t * (3f - 2f * t);

                Color color = image.color;
                color.a = Mathf.Lerp(1f, 0f, smooth);
                image.color = color;

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
