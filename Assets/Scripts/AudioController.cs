using UnityEngine;

public class AudioController : MonoBehaviour
{
    public AudioClip normalHitSfx;
    [Range(0f,1f)] public float normalHitVolume = 0.9f;
    [Range(0f,0.3f)] public float normalHitPitchJitter = 0.03f;
    public float normalHitPitch = 1f;

    public AudioClip specialSfx;
    [Range(0f,1f)] public float specialSfxVolume = 0.9f;
    [Range(0f,0.3f)] public float specialSfxPitchJitter = 0.04f;
    public float specialSfxPitch = 1f;

    AudioSource sfx;

    void Awake()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;
        sfx.loop = false;
    }

    public void PlaySfx(AudioClip clip, float vol, float basePitch, float jitter)
    {
        if (!clip || !sfx) return;
        float p = basePitch + Random.Range(-jitter, jitter);
        sfx.pitch = Mathf.Clamp(p, 0.5f, 2f);
        sfx.PlayOneShot(clip, vol);
    }

    public void PlayNormalHit() => PlaySfx(normalHitSfx, normalHitVolume, normalHitPitch, normalHitPitchJitter);
    public void PlaySpecial()   => PlaySfx(specialSfx, specialSfxVolume, specialSfxPitch, specialSfxPitchJitter);
}
