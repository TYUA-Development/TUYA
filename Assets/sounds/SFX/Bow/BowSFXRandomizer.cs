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

    // Pull 전용 AudioSource. Shoot/Hit는 PlayOneShot으로 겹쳐 재생되어야 하지만(동시에 여러
    // 발이 맞는 경우 등), PlayOneShot으로 재생한 소리는 AudioSource.Stop()으로 중간에 끊을 수
    // 없다는 Unity의 제약이 있다. 조준을 즉시 취소했을 때 이미 재생 중인 Pull 사운드를
    // 확실히 끊을 수 있도록 Pull만 별도 AudioSource에서 Play()/Stop()으로 재생한다.
    private AudioSource pullAudioSource;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        pullAudioSource = gameObject.AddComponent<AudioSource>();
        pullAudioSource.playOnAwake = false;
        pullAudioSource.loop = false;

        if (audioSource != null)
        {
            pullAudioSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            pullAudioSource.spatialBlend = audioSource.spatialBlend;
        }
    }

    public void PlayPull()
    {
        if (pullAudioSource == null) return;
        if (pullSounds == null || pullSounds.Length == 0) return;

        pullAudioSource.clip = pullSounds[Random.Range(0, pullSounds.Length)];
        pullAudioSource.pitch = Random.Range(minPitch, maxPitch);
        pullAudioSource.volume = pullVolume;
        pullAudioSource.Play();
    }

    // 조준이 실제 효과(발사 등) 없이 즉시 취소되었을 때, 이미 재생 중일 수 있는 Pull
    // 사운드를 끊기 위한 용도.
    public void StopPull()
    {
        if (pullAudioSource == null) return;
        pullAudioSource.Stop();
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