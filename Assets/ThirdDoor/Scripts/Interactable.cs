using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] private string promptText = "Press E to interact";
    [SerializeField] private string interactionName = "";

    public string PromptText => string.IsNullOrWhiteSpace(interactionName) ? promptText : promptText + " - " + interactionName;

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
