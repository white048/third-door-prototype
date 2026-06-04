using UnityEngine;
using UnityEngine.UI;

public class InteractionPrompt : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Text promptText;
    [SerializeField] private float interactDistance = 5.5f;
    [SerializeField] private float interactRadius = 0.28f;
    [SerializeField] private LayerMask raycastMask = ~0;

    private Interactable currentInteractable;
    private Interactable previousInteractable;

    public void Configure(PlayerController playerController, Text promptLabel)
    {
        player = playerController;
        promptText = promptLabel;
    }

    private void Awake()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }
    }

    private void Update()
    {
        currentInteractable = null;

        if (player != null && player.ControlsEnabled && player.PlayerCamera != null)
        {
            currentInteractable = FindLookInteractable();
        }

        bool showPrompt = currentInteractable != null;
        UpdateFocus(currentInteractable);
        if (promptText != null)
        {
            promptText.gameObject.SetActive(showPrompt);
            if (showPrompt)
            {
                promptText.text = currentInteractable.PromptText;
            }
        }

        if (showPrompt && Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact(player);
        }

        // Press K to dump diagnostic info — helps debug "why no prompt" issues.
        if (Input.GetKeyDown(KeyCode.K))
        {
            DumpDiagnostics();
        }
    }

    private void DumpDiagnostics()
    {
        if (player == null)
        {
            Debug.Log("[Interaction] player is null");
            return;
        }
        Debug.Log($"[Interaction] player pos={player.transform.position} controlsEnabled={player.ControlsEnabled}");
        if (player.PlayerCamera == null)
        {
            Debug.Log("[Interaction] PlayerCamera is null");
            return;
        }
        Vector3 origin = player.PlayerCamera.transform.position;
        Vector3 dir = player.PlayerCamera.transform.forward;
        Debug.Log($"[Interaction] camera origin={origin} forward={dir}");

        if (Physics.SphereCast(origin, interactRadius, dir, out RaycastHit hit, interactDistance, raycastMask, QueryTriggerInteraction.Collide))
        {
            Interactable found = hit.collider.GetComponentInParent<Interactable>();
            Debug.Log($"[Interaction] SphereCast hit: collider={hit.collider.name} on GO={hit.collider.gameObject.name} layer={hit.collider.gameObject.layer} dist={hit.distance:F2}m Interactable={(found != null ? found.GetType().Name : "<none>")} CanInteract={(found != null ? found.CanInteract(player).ToString() : "n/a")}");
        }
        else
        {
            Debug.Log("[Interaction] SphereCast: no hit within " + interactDistance + "m");
        }

        if (Physics.Raycast(origin, dir, out RaycastHit rayHit, interactDistance, raycastMask, QueryTriggerInteraction.Collide))
        {
            Interactable found = rayHit.collider.GetComponentInParent<Interactable>();
            Debug.Log($"[Interaction] Raycast hit: collider={rayHit.collider.name} on GO={rayHit.collider.gameObject.name} dist={rayHit.distance:F2}m Interactable={(found != null ? found.GetType().Name : "<none>")} CanInteract={(found != null ? found.CanInteract(player).ToString() : "n/a")}");
        }
        else
        {
            Debug.Log("[Interaction] Raycast: no hit within " + interactDistance + "m");
        }
    }

    private void UpdateFocus(Interactable focusedInteractable)
    {
        if (previousInteractable == focusedInteractable)
        {
            return;
        }

        if (previousInteractable != null)
        {
            previousInteractable.SetFocused(false);
        }

        previousInteractable = focusedInteractable;

        if (previousInteractable != null)
        {
            previousInteractable.SetFocused(true);
        }
    }

    private Interactable FindLookInteractable()
    {
        Vector3 origin = player.PlayerCamera.transform.position;
        Vector3 direction = player.PlayerCamera.transform.forward;

        // SphereCast gives forgiveness on aim — a thin Raycast made it too easy to miss the door panel by a hair.
        if (Physics.SphereCast(origin, interactRadius, direction, out RaycastHit hit, interactDistance, raycastMask, QueryTriggerInteraction.Collide))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            if (interactable != null && interactable.CanInteract(player))
            {
                return interactable;
            }
        }

        // Fallback: thin Raycast (sometimes a sphere that overlaps the origin returns the wrong hit, so we also try the precise ray).
        Ray ray = new Ray(origin, direction);
        if (Physics.Raycast(ray, out RaycastHit rayHit, interactDistance, raycastMask, QueryTriggerInteraction.Collide))
        {
            Interactable interactable = rayHit.collider.GetComponentInParent<Interactable>();
            if (interactable != null && interactable.CanInteract(player))
            {
                return interactable;
            }
        }

        return null;
    }

}
