using System.Collections;
using UnityEngine;

public class SceneFadeIn : MonoBehaviour
{
    public CanvasGroup fadeCanvasGroup;
    public float fadeInTime = 1.2f;

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null)
            yield break;

        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;

        float timer = 0f;

        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeInTime);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }
}