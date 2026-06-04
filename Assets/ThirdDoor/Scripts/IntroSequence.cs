using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IntroSequence : MonoBehaviour
{
    [SerializeField] private CanvasGroup blackGroup;
    [SerializeField] private Text introText;
    [SerializeField] private PlayerController player;

    // Persists across SceneManager.LoadScene calls (static survives scene reloads but not Editor recompile).
    // On second+ playthroughs the intro is skipped entirely so the player can replay quickly.
    public static bool HasPlayed { get; private set; }

    private static readonly string[] Lines =
    {
        "家庭，是一个最小的系统。",
        "两个人的系统不稳定。",
        "所以，总会有第三个支点存在。",
        "",
        "《第三扇门》"
    };

    private bool skipRequested;

    public void Configure(CanvasGroup group, Text text, PlayerController playerController)
    {
        blackGroup = group;
        introText = text;
        player = playerController;

        if (HasPlayed)
        {
            // Second playthrough — skip the intro entirely.
            if (blackGroup != null)
            {
                blackGroup.alpha = 0f;
                blackGroup.blocksRaycasts = false;
                blackGroup.gameObject.SetActive(false);
            }
            return;
        }

        if (blackGroup != null)
        {
            blackGroup.alpha = 1f;
            blackGroup.blocksRaycasts = true;
        }

        if (introText != null)
        {
            introText.text = string.Empty;
        }

        if (player != null)
        {
            player.SetControlsEnabled(false);
        }
    }

    private void Start()
    {
        if (HasPlayed)
        {
            return;
        }
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        yield return WaitOrSkip(1.2f);

        string accumulated = string.Empty;
        for (int i = 0; i < Lines.Length && !skipRequested; i++)
        {
            accumulated = string.IsNullOrEmpty(accumulated) ? Lines[i] : accumulated + "\n" + Lines[i];
            if (introText != null)
            {
                introText.text = accumulated;
            }

            float waitFor = i == Lines.Length - 1 ? 2.4f : 1.6f;
            yield return WaitOrSkip(waitFor);
        }

        if (skipRequested && introText != null)
        {
            introText.text = string.Join("\n", Lines);
        }

        yield return WaitOrSkip(2.2f);

        // Re-enable controls before the fade so the world feels alive the moment it appears,
        // rather than the player watching a fade they can't act in.
        if (player != null)
        {
            player.SetControlsEnabled(true);
        }

        const float fadeDuration = 1.1f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (blackGroup != null)
            {
                blackGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            }
            yield return null;
        }

        if (blackGroup != null)
        {
            blackGroup.alpha = 0f;
            blackGroup.blocksRaycasts = false;
            blackGroup.gameObject.SetActive(false);
        }

        HasPlayed = true;
    }

    private IEnumerator WaitOrSkip(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (!skipRequested && Input.anyKeyDown)
            {
                skipRequested = true;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
