using System.Collections;
using UnityEngine;

// Edith-Finch style: when the player walks into the trigger volume around a single letter,
// the letter detaches from the door, drifts away from the player, spins, fades to alpha 0, and self-destructs.
// One-shot per letter — once dispersed, never returns (until scene reload).
[RequireComponent(typeof(TextMesh))]
public class LetterDisperse : MonoBehaviour
{
    [SerializeField] private float flightDuration = 1.6f;
    [SerializeField] private float initialSpeed = 1.1f;
    [SerializeField] private float upwardBias = 0.55f;
    [SerializeField] private float spinSpeed = 180f;
    [SerializeField] private float dragPerSecond = 0.5f;

    private bool triggered;
    private TextMesh textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TextMesh>();
    }

    // Public so siblings can cascade-trigger this letter when any one of them gets touched.
    public void TriggerDisperse(Vector3 playerPosition)
    {
        if (triggered)
        {
            return;
        }
        triggered = true;
        StartCoroutine(Disperse(playerPosition));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
        {
            return;
        }

        if (other.GetComponent<CharacterController>() == null)
        {
            return;
        }

        Vector3 playerPosition = other.transform.position;

        // Play the shatter sound once at the cascade source (not in each letter's TriggerDisperse,
        // which would stack 3x volume when all siblings fire).
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play(GameSound.LetterShatter, 0.55f);
        }

        // Cascade: touching any letter shatters the whole word at once. Otherwise wide letter spacing
        // means the player would only trigger the closest letter and the others would dangle awkwardly.
        Transform group = transform.parent;
        if (group != null)
        {
            LetterDisperse[] siblings = group.GetComponentsInChildren<LetterDisperse>();
            foreach (LetterDisperse letter in siblings)
            {
                letter.TriggerDisperse(playerPosition);
            }
        }
        else
        {
            TriggerDisperse(playerPosition);
        }
    }

    private IEnumerator Disperse(Vector3 playerPosition)
    {
        // Detach so the door's later motion (partial-open rotation, good-ending lift) doesn't carry the floating letter.
        transform.SetParent(null, true);

        // Fly toward (and past) the player so the letter sweeps across the camera — more cinematic
        // than flying into the door, and avoids visually disappearing behind the panel.
        Vector3 toPlayer = playerPosition - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            toPlayer = Vector3.back;
        }
        toPlayer.Normalize();

        Vector3 velocity = (toPlayer + Vector3.up * upwardBias + Random.insideUnitSphere * 0.5f).normalized * initialSpeed;
        Vector3 spinAxis = Random.onUnitSphere;

        Color startColor = textMesh != null ? textMesh.color : Color.white;
        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            transform.position += velocity * dt;
            velocity *= Mathf.Max(0f, 1f - dragPerSecond * dt);
            transform.Rotate(spinAxis, spinSpeed * dt, Space.World);

            if (textMesh != null)
            {
                Color c = startColor;
                c.a = Mathf.Clamp01(1f - elapsed / flightDuration);
                textMesh.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
