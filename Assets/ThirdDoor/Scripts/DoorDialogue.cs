using UnityEngine;

public enum DialogueActor
{
    RedDoor,
    BlueDoor,
    FinalChoice,
    ExitDoor
}

public class DoorDialogue : Interactable
{
    [SerializeField] private DialogueActor actor;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private LockedFeedback lockedFeedback;

    public DialogueActor Actor => actor;

    public override string PromptText
    {
        get
        {
            string prefix = LocalizationManager.Get("prompt_press_e");
            string speakerKey =
                actor == DialogueActor.RedDoor ? "speaker_red" :
                actor == DialogueActor.BlueDoor ? "speaker_blue" : "speaker_third";
            var state = GameStateManager.Instance;
            string progress = state != null && actor != DialogueActor.ExitDoor ? "  /  " + (state.Run.Phase + 1) + "·3" : "";
            string who=actor==DialogueActor.RedDoor?NarrativeText.Speaker(0):actor==DialogueActor.BlueDoor?NarrativeText.Speaker(1):LocalizationManager.Get(speakerKey);
            return prefix + " - " + who + progress;
        }
    }

    public void Configure(DialogueActor dialogueActor, DialogueUI ui)
    {
        actor = dialogueActor;
        dialogueUI = ui;
    }

    public void ConfigureLockedFeedback(LockedFeedback feedback)
    {
        lockedFeedback = feedback;
    }

    private void Awake()
    {
        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUI>();
        }
    }

    public override bool CanInteract(PlayerController player)
    {
        GameStateManager state = GameStateManager.Instance;
        if (state == null)
        {
            state = FindObjectOfType<GameStateManager>();
        }

        if (state == null || state.gameEnded)
        {
            return false;
        }

        if (actor == DialogueActor.RedDoor || actor == DialogueActor.BlueDoor)
        {
            return true;
        }

        if (actor == DialogueActor.ExitDoor)
        {
            // Always interactable: when not yet available, we still show a hint that the door won't open.
            return true;
        }

        if (actor == DialogueActor.FinalChoice)
        {
            return state.finalChoiceAvailable;
        }

        return true;
    }

    public override void Interact(PlayerController player)
    {
        var deckState=GameStateManager.Instance;
        if(deckState!=null&&deckState.Run.DeckRules&&!deckState.Run.DeckReady&&(actor==DialogueActor.RedDoor||actor==DialogueActor.BlueDoor))
        {FindObjectOfType<NarrativeNotebook>().ShowDeck(actor==DialogueActor.RedDoor?0:1);return;}
        if (actor == DialogueActor.ExitDoor)
        {
            GameStateManager state = GameStateManager.Instance != null ? GameStateManager.Instance : FindObjectOfType<GameStateManager>();
            if (state == null)
            {
                return;
            }

            state.TryUseExitDoor();
            return;
        }

        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUI>();
        }

        if (dialogueUI == null)
        {
            Debug.LogWarning("DoorDialogue could not find DialogueUI.");
            return;
        }

        GameStateManager stateForLine = GameStateManager.Instance != null ? GameStateManager.Instance : FindObjectOfType<GameStateManager>();
        int voice = actor == DialogueActor.RedDoor ? 0 : 1;
        if (stateForLine != null && !stateForLine.Run.CanTalk(voice))
        {
            dialogueUI.ShowLocalizedReflection(()=>NarrativeText.Speaker(voice),()=>NarrativeText.L("这一轮的话先留在这里。\n", "Let these words rest here for now.\n") + NarrativeText.Objective(stateForLine.Run));
            return;
        }

        dialogueUI.ShowDialogue(actor);
    }
}
