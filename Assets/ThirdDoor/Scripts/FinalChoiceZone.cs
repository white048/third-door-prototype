using UnityEngine;

public class FinalChoiceZone : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private float triggerDelay = 0.6f;

    private bool hasTriggered;
    private float timeInZone;

    public void Configure(DialogueUI ui)
    {
        dialogueUI = ui;
    }

    private void Awake()
    {
        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUI>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>())
        {
            timeInZone = 0f;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        TryTrigger(other, Time.deltaTime);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>())
        {
            timeInZone = 0f;
        }
    }

    private void TryTrigger(Collider other, float deltaTime)
    {
        if (hasTriggered || !other.GetComponentInParent<PlayerController>())
        {
            return;
        }

        GameStateManager state = GameStateManager.Instance;
        if (state == null || !state.finalChoiceAvailable || state.gameEnded)
        {
            timeInZone = 0f;
            return;
        }

        if (dialogueUI != null && dialogueUI.IsDialogueOpen)
        {
            return;
        }

        timeInZone += deltaTime;
        if (timeInZone < triggerDelay)
        {
            return;
        }

        hasTriggered = true;
        dialogueUI.ShowDialogue(DialogueActor.FinalChoice);
    }
}
