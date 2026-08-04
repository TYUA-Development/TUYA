using System.Collections;
using UnityEngine;

public class EnableObject : MonoBehaviour
{
    [Header("Activate")]
    [Tooltip("켜질 때 재생할 파티클")]
    public ParticleSystem activateParticle;

    [Tooltip("켜질 때 재생할 오디오")]
    public AudioSource activateAudio;

    [Tooltip("켜질 때 알파값을 0에서 1로 서서히 올리는 시간(초). 0이면 즉시 표시된다.")]
    public float activateFadeDuration = 0f;

    [Header("Deactivate")]
    [Tooltip("꺼질 때 재생할 파티클")]
    public ParticleSystem deactivateParticle;

    [Tooltip("꺼질 때 재생할 오디오")]
    public AudioSource deactivateAudio;

    [Tooltip("꺼짐 이펙트가 재생될 시간(초). 이 시간 동안 알파값을 1에서 0으로 서서히 내리고, 끝나면 오브젝트를 실제로 비활성화한다.")]
    public float deactivateDelay = 0f;

    private SpriteRenderer[] fadeRenderers;
    private Coroutine toggleCoroutine;

    void Awake()
    {
        fadeRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void Toggle()
    {
        if (toggleCoroutine != null)
            StopCoroutine(toggleCoroutine);

        if (gameObject.activeSelf)
        {
            toggleCoroutine = StartCoroutine(DeactivateRoutine());
        }
        else
        {
            // 비활성 상태인 GameObject에서는 코루틴을 시작할 수 없으므로
            // (Unity가 "Coroutine couldn't be started because the game object is inactive!" 에러를 냄)
            // SetActive(true)를 코루틴 밖에서 동기적으로 먼저 호출해야 한다.
            gameObject.SetActive(true);
            SetRenderersAlpha(0f);
            PlayParticle(activateParticle);
            PlayAudio(activateAudio);
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
        PlayAudio(deactivateAudio);

        yield return FadeAlpha(0f, deactivateDelay);

        gameObject.SetActive(false);
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

    private void PlayParticle(ParticleSystem particle)
    {
        if (particle == null)
            return;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play();
    }

    private void PlayAudio(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
        audioSource.Play();
    }
}
