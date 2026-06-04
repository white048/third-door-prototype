using System.Collections;
using UnityEngine;

public class ToggleDoor : Interactable
{
    [SerializeField] private Transform doorPanel;
    [SerializeField] private Vector3 openOffset = Vector3.up * 2.7f;
    [SerializeField] private float moveTime = 0.55f;
    [SerializeField] private bool startsOpen;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen;
    private Coroutine moveRoutine;

    private void Awake()
    {
        if (doorPanel == null)
        {
            doorPanel = transform;
        }

        closedPosition = doorPanel.position;
        openPosition = closedPosition + openOffset;
        isOpen = startsOpen;
        doorPanel.position = isOpen ? openPosition : closedPosition;
    }

    public void Configure(Transform panel, Vector3 offset, bool openAtStart)
    {
        doorPanel = panel;
        openOffset = offset;
        startsOpen = openAtStart;
        closedPosition = doorPanel.position;
        openPosition = closedPosition + openOffset;
        isOpen = startsOpen;
        doorPanel.position = isOpen ? openPosition : closedPosition;
    }

    public override void Interact(PlayerController player)
    {
        isOpen = !isOpen;

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveDoor(isOpen ? openPosition : closedPosition));
    }

    private IEnumerator MoveDoor(Vector3 target)
    {
        Vector3 start = doorPanel.position;
        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveTime);
            doorPanel.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        doorPanel.position = target;
    }
}
