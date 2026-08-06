using System.Collections;
using UnityEngine;

public class EnableObject : MonoBehaviour
{
    [Header("Activate")]
    [Tooltip("켜질 때 재생할 파티클")]
    public ParticleSystem activateParticle;

    [Tooltip("켜질 때 AudioAssist로 재생할 효과음.")]
    public AudioAssist activate_Object;

    [Tooltip("켜질 때 알파값을 0에서 1로 서서히 올리는 시간(초). 0이면 즉시 표시된다.")]
    public float activateFadeDuration = 0f;

    [Header("Deactivate")]
    [Tooltip("꺼질 때 재생할 파티클")]
    public ParticleSystem deactivateParticle;

    [Tooltip("꺼질 때 AudioAssist로 재생할 효과음.")]
    public AudioAssist deactivate_Object;

    [Tooltip("꺼짐 이펙트가 재생될 시간(초). 이 시간 동안 알파값을 1에서 0으로 서서히 내리고, 끝나면 콜라이더를 비활성화해 상호작용을 막는다. GameObject 자체는 비활성화하지 않는다 - 그러면 재생 중인 AudioAssist/파티클이 즉시 끊기기 때문.")]
    public float deactivateDelay = 0f;

    [Header("Glow")]
    [Tooltip("켜지거나 꺼질 때, RopeRegenerator의 로프 재생성 연출과 동일하게 흰색으로 번쩍인 뒤 서서히 원래 모습으로 돌아오는 효과를 같이 재생할지.")]
    public bool useGlowFlash = true;

    [Tooltip("흰색 발광이 원래 모습으로 페이드되는 시간(초).")]
    public float glowFadeDuration = 1f;

    [Tooltip("발광 색상.")]
    public Color glowColor = Color.white;

    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    private SpriteRenderer[] fadeRenderers;
    private Collider2D[] fadeColliders;
    private Material[] originalMaterials;
    private Material flashMaterial;
    private Coroutine toggleCoroutine;
    private Coroutine glowCoroutine;

    // gameObject.activeSelf는 더 이상 켜짐/꺼짐 상태를 나타내지 않는다(꺼진 상태에서도 GameObject는
    // 계속 활성 상태로 남아 AudioAssist/파티클이 끊기지 않게 한다) - 그래서 상태를 이 필드로 직접
    // 추적한다. Awake만으로는 부족하다 - GameObject가 씬에 처음부터 비활성화된 채로 배치됐다면
    // Awake가 아예 실행되지 않으므로, 최초 조회 시점(Object_Wind.BaseWindPower와 동일한 패턴)에
    // gameObject.activeSelf로 지연 캡처하는 걸 폴백으로 둔다.
    private bool? isOnState;

    private bool IsOn
    {
        get
        {
            if (!isOnState.HasValue)
                isOnState = gameObject.activeSelf;

            return isOnState.Value;
        }
        set { isOnState = value; }
    }

    void Awake()
    {
        fadeRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        fadeColliders = GetComponentsInChildren<Collider2D>(true);

        if (useGlowFlash)
        {
            Shader flashShader = Shader.Find("Custom/SpriteFlash");
            if (flashShader != null)
            {
                flashMaterial = new Material(flashShader);

                // 진짜 원본 머티리얼은 여기서 딱 한 번만 캡처해둔다. GlowFlashRoutine이 매번
                // 재생 직전에 renderer.sharedMaterial을 읽어 "원본"으로 잡으면, 번쩍임이 끝나기
                // 전에(예: 짧은 시간 안에 Toggle이 다시 호출돼) 코루틴이 중간에 멈추는 경우
                // 그 시점엔 이미 flashMaterial로 바뀌어 있어 flashMaterial 자신을 원본으로
                // 잘못 캡처하게 된다 - 이후 영원히 흰색에서 못 돌아오는 버그가 생긴다.
                originalMaterials = new Material[fadeRenderers.Length];
                for (int i = 0; i < fadeRenderers.Length; i++)
                    originalMaterials[i] = fadeRenderers[i] != null ? fadeRenderers[i].sharedMaterial : null;
            }
        }
    }

    public void Toggle()
    {
        if (toggleCoroutine != null)
            StopCoroutine(toggleCoroutine);

        if (IsOn)
        {
            IsOn = false;
            toggleCoroutine = StartCoroutine(DeactivateRoutine());
        }
        else
        {
            IsOn = true;

            // 씬에 처음부터 비활성화된 채로 배치된 오브젝트를 최초로 켤 때만 필요하다
            // (비활성 상태인 GameObject에서는 코루틴을 시작할 수 없으므로, SetActive(true)를
            // 코루틴 밖에서 동기적으로 먼저 호출해야 한다). 이후로는 GameObject를 다시
            // 비활성화하지 않으므로 이 분기를 다시 타지 않는다.
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            SetCollidersEnabled(true);
            SetRenderersAlpha(0f);
            PlayParticle(activateParticle);
            PlayAudioAssist(activate_Object);
            PlayGlowFlash(activateFadeDuration);
            toggleCoroutine = StartCoroutine(ActivateRoutine());
        }
    }

    private IEnumerator ActivateRoutine()
    {
        yield return FadeAlpha(1f, activateFadeDuration);
        toggleCoroutine = null;
    }

    private IEnumerator DeactivateRoutine()
    {
        PlayParticle(deactivateParticle);
        PlayAudioAssist(deactivate_Object);
        PlayGlowFlash(deactivateDelay);

        yield return FadeAlpha(0f, deactivateDelay);

        // GameObject 자체는 활성 상태로 유지한다 - 방금 재생을 시작한 deactivate_Object의
        // AudioAssist(또는 deactivateParticle)가 자식에 있다면, SetActive(false)는 재생 중인
        // AudioSource를 그 자리에서 즉시 끊어버린다. 대신 콜라이더만 꺼서 상호작용을 막는다.
        SetCollidersEnabled(false);
        toggleCoroutine = null;
    }

    private IEnumerator FadeAlpha(float targetAlpha, float duration)
    {
        if (fadeRenderers == null || fadeRenderers.Length == 0 || duration <= 0f)
        {
            SetRenderersAlpha(targetAlpha);
            yield break;
        }

        float[] startAlphas = new float[fadeRenderers.Length];
        for (int i = 0; i < fadeRenderers.Length; i++)
            startAlphas[i] = fadeRenderers[i] != null ? fadeRenderers[i].color.a : targetAlpha;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            for (int i = 0; i < fadeRenderers.Length; i++)
            {
                if (fadeRenderers[i] == null)
                    continue;

                Color color = fadeRenderers[i].color;
                color.a = Mathf.Lerp(startAlphas[i], targetAlpha, t);
                fadeRenderers[i].color = color;
            }

            yield return null;
        }

        SetRenderersAlpha(targetAlpha);
    }

    private void SetRenderersAlpha(float alpha)
    {
        if (fadeRenderers == null)
            return;

        foreach (var renderer in fadeRenderers)
        {
            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }

    private void SetCollidersEnabled(bool value)
    {
        if (fadeColliders == null)
            return;

        foreach (var collider in fadeColliders)
        {
            if (collider != null)
                collider.enabled = value;
        }
    }

    private void PlayParticle(ParticleSystem particle)
    {
        if (particle == null)
            return;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play();
    }

    private void PlayAudioAssist(AudioAssist audio)
    {
        if (audio != null)
            audio.Play();
    }

    // RopeRegenerator의 로프 재생성 발광 연출(흰색으로 번쩍인 뒤 서서히 원래 모습으로
    // 페이드)과 동일한 방식. 켜질 때/꺼질 때 둘 다에서 호출된다.
    //
    // syncDuration(activate면 activateFadeDuration, deactivate면 deactivateDelay)과
    // glowFadeDuration 중 더 긴 쪽을 실제 발광 지속시간으로 쓴다. 발광이 알파 페이드보다
    // 먼저 끝나버리면, 아직 반투명하게 페이드 중인(=거의 안 보이는) 동안 발광의 밝은
    // 구간이 대부분 지나가버려서 "살짝 밝아졌다가" 보이고, 알파 페이드가 끝나기도 전에
    // 머티리얼이 원본으로 뚝 끊기듯 스왑되어 "갑자기 원래 색으로" 보이는 문제가 있었다.
    // 발광이 알파 페이드보다 절대 먼저 끝나지 않게 해서 항상 페이드가 끝나는 시점과
    // 맞물려 자연스럽게 마무리되도록 한다.
    private void PlayGlowFlash(float syncDuration)
    {
        if (!useGlowFlash || flashMaterial == null || fadeRenderers == null || fadeRenderers.Length == 0)
            return;

        if (glowCoroutine != null)
            StopCoroutine(glowCoroutine);

        float duration = Mathf.Max(glowFadeDuration, syncDuration);
        glowCoroutine = StartCoroutine(GlowFlashRoutine(duration));
    }

    private IEnumerator GlowFlashRoutine(float duration)
    {
        for (int i = 0; i < fadeRenderers.Length; i++)
        {
            if (fadeRenderers[i] != null)
                fadeRenderers[i].sharedMaterial = flashMaterial;
        }

        flashMaterial.SetColor(FlashColorId, glowColor);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float amount = 1f - Mathf.Clamp01(elapsed / duration);
            flashMaterial.SetFloat(FlashAmountId, amount);
            yield return null;
        }

        for (int i = 0; i < fadeRenderers.Length; i++)
        {
            if (fadeRenderers[i] != null && originalMaterials[i] != null)
                fadeRenderers[i].sharedMaterial = originalMaterials[i];
        }

        glowCoroutine = null;
    }
}
