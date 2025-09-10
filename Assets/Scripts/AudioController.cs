using UnityEngine;

public class AudioController : MonoBehaviour
{
    [Header("Normal Hit")]
    public AudioClip normalHitSfx;
    [Range(0f, 1f)] public float normalHitVolume = 0.9f;
    [Range(0f, 0.3f)] public float normalHitPitchJitter = 0.03f;
    public float normalHitPitch = 1.0f;

    [Header("Special")]
    public AudioClip specialSfx;
    [Range(0f, 1f)] public float specialSfxVolume = 0.9f;
    [Range(0f, 0.3f)] public float specialSfxPitchJitter = 0.04f;
    public float specialSfxPitch = 1.0f;

    AudioSource sfx;

    void Awake()
    {
        sfx = GetComponent<AudioSource>();
        if (!sfx) sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.loop = false;
        sfx.spatialBlend = 0f;
    }

    public void PlayNormalHit()
    {
        if (!normalHitSfx || !sfx) return;
        float p = normalHitPitch + Random.Range(-normalHitPitchJitter, normalHitPitchJitter);
        sfx.pitch = Mathf.Clamp(p, 0.5f, 2f);
        sfx.PlayOneShot(normalHitSfx, normalHitVolume);
    }

    public void PlaySpecial()
    {
        if (!specialSfx || !sfx) return;
        float p = specialSfxPitch + Random.Range(-specialSfxPitchJitter, specialSfxPitchJitter);
        sfx.pitch = Mathf.Clamp(p, 0.5f, 2f);
        sfx.PlayOneShot(specialSfx, specialSfxVolume);
    }
}
