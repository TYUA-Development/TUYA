using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BSN;
public class SceneFadeManager : BSNSingleton<SceneFadeManager>
{
    protected override bool DontDestroy => true;

    public Image fadeImage;
    public float fadeDuration = 1f;

    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(CoLoadSceneWithFade(sceneName));
    }

    private IEnumerator CoLoadSceneWithFade(string sceneName)
    {
        // 검게 페이드 아웃
        yield return StartCoroutine(Fade(0f, 1f));

        // 씬 이동
        SceneManager.LoadScene(sceneName);

        // 한 프레임 대기
        yield return null;

        // 다시 밝게 페이드 인
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float time = 0f;

        Color color = fadeImage.color;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float t = time / fadeDuration;
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);

            fadeImage.color = color;

            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;
    }
}