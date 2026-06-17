using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DemoEndFadeToTitle : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";
    public bool activateOnlyOnce = true;

    [Header("Player Control")]
    [Tooltip("화면이 충분히 어두워진 뒤 플레이어 입력을 잠급니다.")]
    public bool lockPlayerInputAfterDarkFade = true;

    [Tooltip("입력 잠금 시 플레이어 속도를 0으로 만듭니다.")]
    public bool stopPlayerVelocityWhenLocked = true;

    public float playerLockTime = 9999f;

    [Header("UI References")]
    [Tooltip("검은 화면용 Image. 전체 화면을 덮는 검은 Panel의 Image를 넣으세요.")]
    public Image fadeImage;

    [Tooltip("감사 문구 TMP Text")]
    public TMP_Text messageText;

    [Tooltip("ESC 안내 문구 TMP Text")]
    public TMP_Text escHintText;

    [Header("Text")]
    [TextArea]
    public string message = "지금까지 데모를 플레이해주셔서 감사합니다.";

    public string escHint = "ESC를 누르면 타이틀로 돌아갑니다.";

    [Header("Fade")]
    [Range(0f, 1f)]
    public float targetDarkAlpha = 0.88f;

    [Tooltip("구역 진입 후 어두워지기 시작하기 전 대기")]
    public float startDelay = 0.2f;

    [Tooltip("화면이 어두워지는 시간")]
    public float darkFadeTime = 2.2f;

    [Tooltip("화면이 어두워진 뒤 문구가 나오기 전 대기")]
    public float messageDelay = 0.5f;

    public float messageFadeTime = 1.4f;

    [Tooltip("감사 문구가 나온 뒤 ESC 안내가 나오기 전 대기")]
    public float escHintDelay = 1.0f;

    public float escHintFadeTime = 1.0f;

    [Header("Title Scene")]
    public string titleSceneName = "Title";

    [Tooltip("ESC를 누른 뒤 완전 검정으로 짧게 페이드하고 타이틀로 이동")]
    public bool fadeToFullBlackBeforeTitle = true;

    public float titleFadeTime = 0.6f;

    [Header("Audio Optional")]
    public AudioSource endStartAudio;
    public AudioSource messageAudio;

    [Header("Time")]
    public bool useUnscaledTime = false;

    [Header("State")]
    public bool hasActivated;
    public bool isEnding;
    public bool canPressEsc;
    public bool playerLocked;

    private Coroutine endCoroutine;
    private Coroutine titleCoroutine;

    private PlayerController cachedPlayerController;
    private Rigidbody2D cachedPlayerRigidbody;

    private void Awake()
    {
        SetupInitialUI();
    }

    private void Update()
    {
        if (!canPressEsc)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            canPressEsc = false;

            if (titleCoroutine != null)
                StopCoroutine(titleCoroutine);

            titleCoroutine = StartCoroutine(GoToTitleRoutine());
        }
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

        if (messageText != null)
        {
            messageText.text = message;
            SetTextAlpha(messageText, 0f);
        }

        if (escHintText != null)
        {
            escHintText.text = escHint;
            SetTextAlpha(escHintText, 0f);
        }
    }

    private IEnumerator EndSequenceRoutine()
    {
        isEnding = true;
        canPressEsc = false;
        playerLocked = false;

        PlayAudio(endStartAudio);

        if (startDelay > 0f)
            yield return Wait(startDelay);

        // 1. 먼저 화면만 자연스럽게 어두워짐
        yield return StartCoroutine(FadeImageAlpha(
            fadeImage,
            GetImageAlpha(fadeImage),
            targetDarkAlpha,
            darkFadeTime
        ));

        // 2. 화면이 어두워진 뒤 플레이어 입력 잠금
        if (lockPlayerInputAfterDarkFade)
            LockPlayerNow();

        // 3. 그 다음 문구 등장
        if (messageDelay > 0f)
            yield return Wait(messageDelay);

        if (messageText != null)
            messageText.text = message;

        PlayAudio(messageAudio);

        yield return StartCoroutine(FadeTextAlpha(
            messageText,
            GetTextAlpha(messageText),
            1f,
            messageFadeTime
        ));

        if (escHintDelay > 0f)
            yield return Wait(escHintDelay);

        if (escHintText != null)
            escHintText.text = escHint;

        yield return StartCoroutine(FadeTextAlpha(
            escHintText,
            GetTextAlpha(escHintText),
            1f,
            escHintFadeTime
        ));

        canPressEsc = true;
        endCoroutine = null;
    }

    private void LockPlayerNow()
    {
        if (playerLocked)
            return;

        playerLocked = true;

        if (cachedPlayerController != null)
        {
            cachedPlayerController.LockPlayerInput(playerLockTime);
        }

        if (stopPlayerVelocityWhenLocked && cachedPlayerRigidbody != null)
        {
            cachedPlayerRigidbody.velocity = Vector2.zero;
            cachedPlayerRigidbody.angularVelocity = 0f;
        }
    }

    private IEnumerator GoToTitleRoutine()
    {
        if (fadeToFullBlackBeforeTitle)
        {
            yield return StartCoroutine(FadeImageAlpha(
                fadeImage,
                GetImageAlpha(fadeImage),
                1f,
                titleFadeTime
            ));
        }

        SceneManager.LoadScene(titleSceneName);
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

            float t = Mathf.Clamp01(timer / duration);
            t = Smooth01(t);

            float alpha = Mathf.Lerp(from, to, t);
            SetImageAlpha(image, alpha);

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

            float t = Mathf.Clamp01(timer / duration);
            t = Smooth01(t);

            float alpha = Mathf.Lerp(from, to, t);
            SetTextAlpha(text, alpha);

            yield return null;
        }

        SetTextAlpha(text, to);
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
}