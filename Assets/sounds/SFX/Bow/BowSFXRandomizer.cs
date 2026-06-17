using UnityEngine;

public class BowSFXRandomizer : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Pull")]
    public AudioClip[] pullSounds;

    [Header("Shoot")]
    public AudioClip[] shootSounds;

    [Header("Hit")]
    public AudioClip[] hitSounds;

    [Header("Volume")]
    public float pullVolume = 0.5f;
    public float shootVolume = 0.75f;
    public float hitVolume = 0.8f;

    [Header("Hit Distance")]
    [Tooltip("이 거리 이내에서 화살이 박히면 풀 볼륨. 이후 선형 감소.")]
    public float hitFullVolumeDistance = 5f;
    [Tooltip("이 거리 이상이면 소리 안 들림.")]
    public float hitSilentDistance = 25f;

    [Header("Pitch Random")]
    public float minPitch = 0.97f;
    public float maxPitch = 1.03f;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlayPull()
    {
        PlayRandom(pullSounds, pullVolume);
    }

    public void PlayShoot()
    {
        PlayRandom(shootSounds, shootVolume);
    }

    public void PlayHit(Vector3 hitWorldPosition)
    {
        float dist = Vector2.Distance(transform.position, hitWorldPosition);
        float t = Mathf.InverseLerp(hitFullVolumeDistance, hitSilentDistance, dist);
        float volume = Mathf.Lerp(hitVolume, 0f, t);
        if (volume <= 0f) return;
        PlayRandom(hitSounds, volume);
    }

    void PlayRandom(AudioClip[] clips, float volume)
    {
        if (audioSource == null) return;
        if (clips == null || clips.Length == 0) return;

        int index = Random.Range(0, clips.Length);

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clips[index], volume);
    }
}