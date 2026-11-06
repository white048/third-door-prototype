using System.Collections;
using UnityEngine;

public class SymbolicDoorController : MonoBehaviour
{
    [SerializeField] private Transform doorPanel;
    [SerializeField] private Transform glowSlit;
    [SerializeField] private Light glowLight;
    [SerializeField] private Material baseMaterial;
    [SerializeField] private Material glowMaterial;
    [SerializeField] private Vector3 openOffset = Vector3.up * 2.8f;
    [SerializeField] private float openAngle = 74f;

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private Coroutine moveRoutine;

    public Renderer[] DoorRenderers { get; private set; }

    public void Configure(Transform panel, Transform slit, Light light, Material doorMaterial, Material whiteGlowMaterial, Vector3 offset, float angle = 74f)
    {
        doorPanel = panel;
        glowSlit = slit;
        glowLight = light;
        baseMaterial = doorMaterial;
        glowMaterial = whiteGlowMaterial;
        openOffset = offset;
        openAngle = angle;

        DoorRenderers = doorPanel.GetComponentsInChildren<Renderer>();
        ApplyDoorMaterial(baseMaterial);

        closedPosition = doorPanel.position;
        closedRotation = doorPanel.rotation;

        HideGlow();
    }

    // Progress levels drive the door's rotation only — no light, just a swing on its hinge.
    //   level <= 0 → fully closed
    //   level == 1 → cracked open (about 20% of openAngle)
    //   level >= 2 → opened wider (about 40% of openAngle)
    // Full open (good ending) goes through OpenDoor.
    public void SetProgress(int level)
    {
        HideGlow();

        float angle;
        if (level <= 0)
        {
            angle = 0f;
        }
        else if (level == 1)
        {
            angle = openAngle * 0.06f;
        }
        else
        {
            angle = openAngle * 0.13f;
        }

        MoveTo(closedPosition, closedRotation * Quaternion.Euler(0f, angle, 0f));
    }

    public void OpenDoor()
    {
        HideGlow();
        // Full open (good ending) slides the door straight up out of the frame — cleaner than a 76° swing
        // that crowds the middle room. Partial progress still uses rotation for the "small crack" feel.
        MoveTo(closedPosition + openOffset, closedRotation);
    }

    public void DarkenDoor()
    {
        HideGlow();
        MoveTo(closedPosition, closedRotation);
    }

    private void MoveTo(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (!Application.isPlaying) { doorPanel.SetPositionAndRotation(targetPosition, targetRotation); return; }
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveRoutine(targetPosition, targetRotation));
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 start = doorPanel.position;
        Quaternion startRotation = doorPanel.rotation;
        float elapsed = 0f;
        const float duration = 0.9f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            doorPanel.position = Vector3.Lerp(start, targetPosition, t);
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

    private void ApplyDoorMaterial(Material material)
    {
        if (DoorRenderers == null || material == null)
        {
            return;
        }

        foreach (Renderer renderer in DoorRenderers)
        {
            // Skip TextMesh renderers — they own their font material and would render invisibly
            // if we overrode it with the door's color material.
            if (renderer.GetComponent<TextMesh>() != null || renderer.gameObject.name != "Printed door leaf")
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }
}
