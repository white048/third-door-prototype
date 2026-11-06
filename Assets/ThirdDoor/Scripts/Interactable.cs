using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    // Preserve the original scene's serialized layout when using the existing player as a preview host.
    [SerializeField, HideInInspector] private string promptText = "";
    [SerializeField] private string interactionName = "";

    // The "Press E to interact" prefix is pulled from LocalizationManager so it tracks language.
    // The target name is set by subclasses (e.g. DoorDialogue can override this getter to localize).
    public virtual string PromptText
    {
        get
        {
            string prefix = LocalizationManager.Get("prompt_press_e");
            if (string.IsNullOrEmpty(prefix)) prefix = promptText;
            return string.IsNullOrWhiteSpace(interactionName) ? prefix : prefix + " - " + interactionName;
        }
    }

    public void ConfigurePrompt(string targetName)
    {
        interactionName = targetName;
    }

    public virtual bool CanInteract(PlayerController player)
    {
        return player != null;
    }

    public virtual void SetFocused(bool focused)
    {
        FocusHighlighter highlighter = GetComponent<FocusHighlighter>();
        if (highlighter != null)
        {
            highlighter.SetFocused(focused);
        }
    }

    public abstract void Interact(PlayerController player);
}
