using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InGameTitleReturnButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private InGameSettingsMenuController owner;
    private Graphic[] graphics;
    private float normalAlpha = 0.2f;
    private float hoverAlpha = 1f;
    private float fadeTime = 0.12f;
    private Coroutine fadeCoroutine;

    public void Initialize(InGameSettingsMenuController owner, float normalAlpha, float hoverAlpha, float fadeTime)
    {
        this.owner = owner;
        this.normalAlpha = normalAlpha;
        this.hoverAlpha = hoverAlpha;
        this.fadeTime = fadeTime;

        CacheGraphics();
        SetAlpha(this.normalAlpha);
    }

    private void Awake()
    {
        CacheGraphics();
    }

    private void OnEnable()
    {
        SetAlpha(normalAlpha);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        FadeTo(hoverAlpha);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        FadeTo(normalAlpha);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null)
            owner.ReturnToTitle();
    }

    private void CacheGraphics()
    {
        graphics = GetComponentsInChildren<Graphic>(true);
    }

    private void FadeTo(float targetAlpha)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = GetCurrentAlpha();

        if (fadeTime <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeTime);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
        fadeCoroutine = null;
    }

    private float GetCurrentAlpha()
    {
        if (graphics == null || graphics.Length == 0 || graphics[0] == null)
            return normalAlpha;

        return graphics[0].color.a;
    }

    private void SetAlpha(float alpha)
    {
        if (graphics == null || graphics.Length == 0)
            CacheGraphics();

        if (graphics == null)
            return;

        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
                continue;

            Color color = graphic.color;
            color.a = alpha;

            if (graphic is Image)
                color.a = 0f;

            if (graphic is TMP_Text)
                color.a = alpha;

            graphic.color = color;
        }
    }
}
