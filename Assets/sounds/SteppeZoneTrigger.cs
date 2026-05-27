using UnityEngine;

public class SteppeZoneTrigger : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource steppeBGM;
    public AudioSource steppeAmbience;

    [Header("Target Volumes")]
    public float bgmTargetVolume = 0.25f;
    public float ambienceTargetVolume = 0.6f;

    [Header("Fade Settings")]
    public float fadeInTime = 3f;
    public float fadeOutTime = 4f;

    private bool playerInside = false;

    // 0 = 완전 무음, 1 = 목표 볼륨 100%
    private float fadePercent = 0f;

    void Start()
    {
        Setup(steppeBGM);
        Setup(steppeAmbience);
    }

    void Update()
    {
        if (playerInside)
        {
            FadePercentIn();
        }
        else
        {
            FadePercentOut();
        }

        ApplyVolume();
    }

    void Setup(AudioSource source)
    {
        if (source == null) return;

        source.volume = 0f;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    void FadePercentIn()
    {
        if (steppeBGM != null && !steppeBGM.isPlaying)
            steppeBGM.Play();

        if (steppeAmbience != null && !steppeAmbience.isPlaying)
            steppeAmbience.Play();

        fadePercent += Time.deltaTime / fadeInTime;
        fadePercent = Mathf.Clamp01(fadePercent);
    }

    void FadePercentOut()
    {
        fadePercent -= Time.deltaTime / fadeOutTime;
        fadePercent = Mathf.Clamp01(fadePercent);

        if (fadePercent <= 0.001f)
        {
            StopIfPlaying(steppeBGM);
            StopIfPlaying(steppeAmbience);
        }
    }

    void ApplyVolume()
    {
        if (steppeBGM != null)
            steppeBGM.volume = bgmTargetVolume * fadePercent;

        if (steppeAmbience != null)
            steppeAmbience.volume = ambienceTargetVolume * fadePercent;
    }

    void StopIfPlaying(AudioSource source)
    {
        if (source == null) return;

        source.volume = 0f;

        if (source.isPlaying)
            source.Stop();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }
}