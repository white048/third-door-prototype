using System.Collections;
using UnityEngine;

public class EndingManager : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private Light middleRoomLight;
    [SerializeField] private Light exitBackLight;
    [SerializeField] private Transform redPressureWall;
    [SerializeField] private Transform bluePressureWall;
    [SerializeField] private Renderer[] guidanceLineRenderers;
    [SerializeField] private Renderer[] sideDoorRenderers;
    [SerializeField] private Transform middleCeiling;

    private Vector3 ceilingOriginPosition;
    private Coroutine ceilingRoutine;

    public void Configure(DialogueUI ui, Light centerLight)
    {
        dialogueUI = ui;
        middleRoomLight = centerLight;
    }

    public void ConfigureEnvironment(Light exitLight, Transform redWall, Transform blueWall, Renderer[] guideLines, Renderer[] doorPanels, Transform ceiling)
    {
        exitBackLight = exitLight;
        redPressureWall = redWall;
        bluePressureWall = blueWall;
        guidanceLineRenderers = guideLines;
        sideDoorRenderers = doorPanels;
        middleCeiling = ceiling;

        if (middleCeiling != null)
        {
            ceilingOriginPosition = middleCeiling.position;
        }

        if (exitBackLight != null)
        {
            exitBackLight.enabled = false;
        }

        if (redPressureWall != null)
        {
            redPressureWall.gameObject.SetActive(false);
        }

        if (bluePressureWall != null)
        {
            bluePressureWall.gameObject.SetActive(false);
        }
    }

    private void Awake()
    {
        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUI>();
        }
    }

    public void ShowEnding(bool goodEnding)
    {
        if (goodEnding)
        {
            if (middleRoomLight != null)
            {
                middleRoomLight.intensity = 4.8f;
                middleRoomLight.color = Color.white;
            }

            if (exitBackLight != null)
            {
                exitBackLight.enabled = true;
                exitBackLight.intensity = 8f;
                exitBackLight.range = 9f;
            }

            SetGuideLineEmission(1.6f);
            LerpCeilingTo(ceilingOriginPosition.y);
            dialogueUI.ShowEndingText("门开了。");
        }
        else
        {
            if (middleRoomLight != null)
            {
                middleRoomLight.intensity = 0.45f;
                middleRoomLight.color = new Color(0.42f, 0.36f, 0.17f);
            }

            if (redPressureWall != null)
            {
                redPressureWall.gameObject.SetActive(true);
            }

            if (bluePressureWall != null)
            {
                bluePressureWall.gameObject.SetActive(true);
            }

            SetGuideLineEmission(0f);
            LerpCeilingTo(ceilingOriginPosition.y - 0.55f);
            dialogueUI.ShowEndingText("那就继续循环吧。");
        }
    }

    public void UpdateProgressFeedback(int directConnection)
    {
        if (middleRoomLight != null)
        {
            if (directConnection <= 0)
            {
                middleRoomLight.intensity = directConnection < 0 ? 1.2f : 2.2f;
                middleRoomLight.color = new Color(0.85f, 0.62f, 0.22f);
            }
            else if (directConnection == 1)
            {
                middleRoomLight.intensity = 3.0f;
                middleRoomLight.color = new Color(1f, 0.83f, 0.38f);
            }
            else
            {
                middleRoomLight.intensity = 3.8f;
                middleRoomLight.color = new Color(1f, 0.92f, 0.68f);
            }
        }

        SetGuideLineEmission(directConnection <= 0 ? 0.25f : directConnection == 1 ? 0.75f : 1.15f);

        // Ceiling presses down when player drifts toward the bad path.
        float targetY = ceilingOriginPosition.y + Mathf.Min(0, directConnection) * 0.2f;
        LerpCeilingTo(targetY);
    }

    private void LerpCeilingTo(float targetY)
    {
        if (middleCeiling == null)
        {
            return;
        }

        if (ceilingRoutine != null)
        {
            StopCoroutine(ceilingRoutine);
        }

        ceilingRoutine = StartCoroutine(CeilingRoutine(targetY));
    }

    private IEnumerator CeilingRoutine(float targetY)
    {
        Vector3 start = middleCeiling.position;
        Vector3 end = new Vector3(start.x, targetY, start.z);
        const float duration = 0.9f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            middleCeiling.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        middleCeiling.position = end;
        ceilingRoutine = null;
    }

    private void SetGuideLineEmission(float intensity)
    {
        if (guidanceLineRenderers == null)
        {
            return;
        }

        Color emission = Color.white * intensity;
        foreach (Renderer lineRenderer in guidanceLineRenderers)
        {
            if (lineRenderer == null)
            {
                continue;
            }

            foreach (Material material in lineRenderer.materials)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
        }
    }
}
