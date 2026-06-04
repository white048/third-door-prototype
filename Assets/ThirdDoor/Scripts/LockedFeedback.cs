using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LockedFeedback : MonoBehaviour
{
    [SerializeField] private Volume volume;

    private Vignette vignette;
    private float baseVignetteIntensity = 0.28f;
    private Coroutine pulseRoutine;

    public void Configure(Volume targetVolume)
    {
        volume = targetVolume;
        if (volume != null && volume.profile != null && volume.profile.TryGet(out Vignette v))
        {
            vignette = v;
            baseVignetteIntensity = vignette.intensity.value;
        }
    }

    public void Play()
    {
        if (vignette == null)
        {
            return;
        }

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
        }

        pulseRoutine = StartCoroutine(Pulse());
    }

    private IEnumerator Pulse()
    {
        const float duration = 0.55f;
        const float peakBoost = 0.32f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float bump = Mathf.Sin(t * Mathf.PI);
            vignette.intensity.Override(baseVignetteIntensity + peakBoost * bump);
            yield return null;
        }
        vignette.intensity.Override(baseVignetteIntensity);
        pulseRoutine = null;
    }
}
