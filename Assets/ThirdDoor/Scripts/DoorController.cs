using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Transform doorPanel;
    [SerializeField] private Transform glowSlit;
    [SerializeField] private Light glowLight;
    [SerializeField] private Renderer doorRenderer;
    [SerializeField] private Material blackDoorMaterial;
    [SerializeField] private Material whiteGlowMaterial;

    private const float FullOpenAngle = 78f;
    private const float MoveDuration = 1.2f;

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private Coroutine moveRoutine;

    public void Configure(
        Transform panel,
        Transform slit,
        Light slitLight,
        Renderer panelRenderer,
        Material doorMaterial,
        Material glowMaterial)
    {
        doorPanel = panel;
        glowSlit = slit;
        glowLight = slitLight;
        doorRenderer = panelRenderer;
        blackDoorMaterial = doorMaterial;
        whiteGlowMaterial = glowMaterial;
        closedPosition = doorPanel.position;
        closedRotation = doorPanel.rotation;
        HideGlow();
        UpdateDoorState(0);
    }

    private void Awake()
    {
        if (doorPanel == null)
        {
            doorPanel = transform;
        }

        if (doorRenderer == null)
        {
            doorRenderer = doorPanel.GetComponent<Renderer>();
        }

        closedPosition = doorPanel.position;
        closedRotation = doorPanel.rotation;
    }

    // Drives the third door's rotation by directConnection.
    //   <= 0 → closed
    //   == 1 → cracked open ~14°
    //   >= 2 → cracked open ~28°
    // Full open / closed for the ending happens via OpenDoor() / DarkenDoor().
    public void UpdateDoorState(int directConnection)
    {
        if (doorRenderer != null && blackDoorMaterial != null)
        {
            doorRenderer.sharedMaterial = blackDoorMaterial;
        }

        HideGlow();

        float angle;
        if (directConnection <= 0)
        {
            angle = 0f;
        }
        else if (directConnection == 1)
        {
            angle = 4f;
        }
        else
        {
            angle = 9f;
        }

        MoveDoorTo(closedPosition, closedRotation * Quaternion.Euler(0f, angle, 0f));
    }

    public void OpenDoor()
    {
        HideGlow();
        // Full open (good ending) slides the door straight up out of the frame — clean reveal of the corridor beyond.
        MoveDoorTo(closedPosition + Vector3.up * 4.25f, closedRotation);
    }

    public void DarkenDoor()
    {
        HideGlow();
        MoveDoorTo(closedPosition, closedRotation);
    }

    private void MoveDoorTo(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (!Application.isPlaying) { doorPanel.SetPositionAndRotation(targetPosition, targetRotation); return; }
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(LerpTo(targetPosition, targetRotation));
    }

    private IEnumerator LerpTo(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = doorPanel.position;
        Quaternion startRotation = doorPanel.rotation;
        float elapsed = 0f;

        while (elapsed < MoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / MoveDuration);
            doorPanel.position = Vector3.Lerp(startPosition, targetPosition, t);
            doorPanel.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        doorPanel.SetPositionAndRotation(targetPosition, targetRotation);
    }

    private void HideGlow()
    {
        if (glowSlit != null)
        {
            glowSlit.gameObject.SetActive(false);
        }

        if (glowLight != null)
        {
            glowLight.enabled = false;
        }
    }
}
