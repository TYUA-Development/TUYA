using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBloomAreaTrigger : MonoBehaviour
{
    private class GlowSprite
    {
        public SpriteRenderer source;
        public SpriteRenderer glow;
    }

    [Header("Target")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool affectAllPlayerSprites = true;

    [Header("Bloom Look")]
    [ColorUsage(true, true)]
    [SerializeField] private Color bloomColor = new Color(0.25f, 0.8f, 2.4f, 0.65f);
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.55f;
    [SerializeField] private float glowScale = 1.05f;
    [SerializeField] private int sortingOrderOffset = 1;
    [SerializeField] private float fadeInTime = 1.5f;
    [SerializeField] private float fadeOutTime = 1.0f;

    [Header("Material")]
    [SerializeField] private Material glowMaterial;
    [SerializeField] private string fallbackShaderName = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;

    private readonly List<GlowSprite> glowSprites = new List<GlowSprite>();
    private Transform currentPlayer;
    private Material runtimeGlowMaterial;
    private Coroutine fadeRoutine;
    private float currentAlpha;

    private void Reset()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void LateUpdate()
    {
        SyncGlowSprites();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        currentPlayer = collision.transform;
        BuildGlowSprites(collision);
        StartFade(maxAlpha, fadeInTime);

        if (showDebugLog)
            Debug.Log("Player bloom glow started.");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag(playerTag))
            return;

        StartFade(0f, fadeOutTime);

        if (showDebugLog)
            Debug.Log("Player bloom glow fading out.");
    }

    private void OnDisable()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        ClearGlowSprites();
    }

    private void BuildGlowSprites(Collider2D collision)
    {
        ClearGlowSprites();

        List<SpriteRenderer> sources = FindTargetRenderers(collision);

        for (int i = 0; i < sources.Count; i++)
        {
            SpriteRenderer source = sources[i];

            if (source == null)
                continue;

            GameObject glowObject = new GameObject(source.gameObject.name + "_BloomGlow");
            glowObject.transform.SetParent(source.transform, false);
            glowObject.transform.localPosition = Vector3.zero;
            glowObject.transform.localRotation = Quaternion.identity;
            glowObject.transform.localScale = Vector3.one * glowScale;

            SpriteRenderer glowRenderer = glowObject.AddComponent<SpriteRenderer>();
            glowRenderer.sharedMaterial = GetGlowMaterial(source);
            glowRenderer.color = GetGlowColor(0f);

            glowSprites.Add(new GlowSprite
            {
                source = source,
                glow = glowRenderer
            });
        }

        SyncGlowSprites();
    }

    private List<SpriteRenderer> FindTargetRenderers(Collider2D collision)
    {
        List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        PlayerController playerController = collision.GetComponent<PlayerController>();

        if (playerController == null)
            playerController = collision.GetComponentInParent<PlayerController>();

        if (playerController != null && playerController.charactorSprite != null)
            AddRenderer(renderers, playerController.charactorSprite);

        if (affectAllPlayerSprites)
        {
            SpriteRenderer[] childRenderers = collision.GetComponentsInChildren<SpriteRenderer>(true);

            for (int i = 0; i < childRenderers.Length; i++)
                AddRenderer(renderers, childRenderers[i]);
        }

        if (renderers.Count == 0)
        {
            SpriteRenderer spriteRenderer = collision.GetComponentInParent<SpriteRenderer>();
            AddRenderer(renderers, spriteRenderer);
        }

        return renderers;
    }

    private void AddRenderer(List<SpriteRenderer> renderers, SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null || renderers.Contains(spriteRenderer))
            return;

        if (spriteRenderer.gameObject.name.EndsWith("_BloomGlow"))
            return;

        renderers.Add(spriteRenderer);
    }

    private Material GetGlowMaterial(SpriteRenderer source)
    {
        if (glowMaterial != null)
            return glowMaterial;

        if (runtimeGlowMaterial != null)
            return runtimeGlowMaterial;

        Shader shader = Shader.Find(fallbackShaderName);

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            runtimeGlowMaterial = new Material(shader);
            runtimeGlowMaterial.name = "Runtime_PlayerBloomGlow_Material";
            return runtimeGlowMaterial;
        }

        return source != null ? source.sharedMaterial : null;
    }

    private void SyncGlowSprites()
    {
        for (int i = glowSprites.Count - 1; i >= 0; i--)
        {
            GlowSprite entry = glowSprites[i];

            if (entry == null || entry.source == null || entry.glow == null)
            {
                glowSprites.RemoveAt(i);
                continue;
            }

            entry.glow.sprite = entry.source.sprite;
            entry.glow.flipX = entry.source.flipX;
            entry.glow.flipY = entry.source.flipY;
            entry.glow.drawMode = entry.source.drawMode;
            entry.glow.size = entry.source.size;
            entry.glow.sortingLayerID = entry.source.sortingLayerID;
            entry.glow.sortingOrder = entry.source.sortingOrder + sortingOrderOffset;
            entry.glow.maskInteraction = entry.source.maskInteraction;
            entry.glow.enabled = entry.source.enabled;
            entry.glow.color = GetGlowColor(currentAlpha);
        }
    }

    private void StartFade(float targetAlpha, float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        float startAlpha = currentAlpha;
        float finalAlpha = Mathf.Clamp01(targetAlpha);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            currentAlpha = Mathf.Lerp(startAlpha, finalAlpha, t);

            SyncGlowSprites();

            yield return null;
        }

        currentAlpha = finalAlpha;
        SyncGlowSprites();
        fadeRoutine = null;

        if (Mathf.Approximately(currentAlpha, 0f))
            ClearGlowSprites();
    }

    private Color GetGlowColor(float alpha)
    {
        return new Color(
            bloomColor.r,
            bloomColor.g,
            bloomColor.b,
            bloomColor.a * Mathf.Clamp01(alpha)
        );
    }

    private void ClearGlowSprites()
    {
        for (int i = 0; i < glowSprites.Count; i++)
        {
            GlowSprite entry = glowSprites[i];

            if (entry != null && entry.glow != null)
                Destroy(entry.glow.gameObject);
        }

        glowSprites.Clear();
        currentPlayer = null;
        currentAlpha = 0f;
    }
}
