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

    private SpriteRenderer[] fadeRenderers;
    private Collider2D[] fadeColliders;
    private Coroutine toggleCoroutine;

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
}
