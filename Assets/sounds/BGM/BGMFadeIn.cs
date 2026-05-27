using UnityEngine;

public class BGMFadeIn : MonoBehaviour
{
    public AudioSource audioSource;
    public float targetVolume = 0.3f;
    public float fadeTime = 3f;

    private float timer = 0f;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.volume = 0f;
        audioSource.loop = true;
        audioSource.Play();
    }

    void Update()
    {
        if (timer < fadeTime)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, timer / fadeTime);
        }
    }
}