using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    private static GameStateManager _instance;

    // Self-healing accessor: if the static reference goes stale (scene reload, hot-reload, etc.)
    // it transparently re-resolves via FindObjectOfType so callers never see a null state.
    public static GameStateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameStateManager>();
            }
            return _instance;
        }
    }

    public int directConnection = 0;
    public bool hasRedTruth = false;
    public bool hasBlueTruth = false;
    public bool redDoorDone = false;
    public bool blueDoorDone = false;
    public bool finalChoiceAvailable = false;
    public bool gameEnded = false;
    public bool thirdDoorTried = false;

    [SerializeField] private DoorController doorController;
    [SerializeField] private EndingManager endingManager;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private SymbolicDoorController redDoorController;
    [SerializeField] private SymbolicDoorController blueDoorController;

    public void Configure(DoorController door, EndingManager endings, DialogueUI ui)
    {
        doorController = door;
        endingManager = endings;
        dialogueUI = ui;
    }

    public void ConfigureSymbolicDoors(SymbolicDoorController redDoor, SymbolicDoorController blueDoor)
    {
        redDoorController = redDoor;
        blueDoorController = blueDoor;
    }

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        if (doorController == null)
        {
            doorController = FindObjectOfType<DoorController>();
        }

        if (endingManager == null)
        {
            endingManager = FindObjectOfType<EndingManager>();
        }

        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUI>();
        }

        UpdateWorldState();
    }

    public string ApplyRedDoorChoice(int optionIndex)
    {
        if (redDoorDone)
        {
            return string.Empty;
        }

        redDoorDone = true;

        if (optionIndex == 0)
        {
            directConnection -= 1;
            UpdateWorldState();
            return "门开了一点。远处变暗了。";
        }

        if (optionIndex == 1)
        {
            directConnection += 1;
            hasRedTruth = true;
            UpdateWorldState();
            return "门似乎敞开了一点。";
        }

        UpdateWorldState();
        return "还不是时候。";
    }

    public string ApplyBlueDoorChoice(int optionIndex)
    {
        if (blueDoorDone)
        {
            return string.Empty;
        }

        blueDoorDone = true;

        if (optionIndex == 0)
        {
            directConnection -= 1;
            UpdateWorldState();
            return "门关的更紧了。";
        }

        if (optionIndex == 1)
        {
            directConnection += 1;
            hasBlueTruth = true;
            UpdateWorldState();
            return "门似乎敞开了一点。";
        }

        if (hasRedTruth)
        {
            directConnection += 1;
            UpdateWorldState();
            return "门似乎敞开了一点。";
        }

        directConnection -= 1;
        UpdateWorldState();
        return "还不是时候。";
    }

    public string ApplyFinalChoice(int optionIndex)
    {
        if (gameEnded)
        {
            return string.Empty;
        }

        if (optionIndex == 1 && directConnection >= 2)
        {
            EndGame(true);
            return "门开了。";
        }

        EndGame(false);
        return "那就继续循环吧。";
    }

    public void TryUseExitDoor()
    {
        if (gameEnded)
        {
            return;
        }

        if (finalChoiceAvailable)
        {
            FindObjectOfType<DialogueUI>().ShowDialogue(DialogueActor.FinalChoice);
        }
    }

    public void MarkThirdDoorTried()
    {
        if (thirdDoorTried)
        {
            return;
        }

        thirdDoorTried = true;
        if (dialogueUI != null && !gameEnded)
        {
            dialogueUI.SetStatusText(GetStatusText());
        }
    }

    private void EndGame(bool goodEnding)
    {
        gameEnded = true;
        finalChoiceAvailable = false;

        if (goodEnding)
        {
            if (redDoorController != null)
            {
                redDoorController.OpenDoor();
            }

            if (blueDoorController != null)
            {
                blueDoorController.OpenDoor();
            }

            if (doorController != null)
            {
                doorController.OpenDoor();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopLoop();
                AudioManager.Instance.Play(GameSound.DoorOpenSuccess, 0.85f);
            }
        }
        else
        {
            if (redDoorController != null)
            {
                redDoorController.DarkenDoor();
            }

            if (blueDoorController != null)
            {
                blueDoorController.DarkenDoor();
            }

            if (doorController != null)
            {
                doorController.DarkenDoor();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play(GameSound.DoorCloseBad, 0.8f);
            }
        }

        if (endingManager != null)
        {
            endingManager.ShowEnding(goodEnding);
        }
    }

    private void UpdateWorldState()
    {
        finalChoiceAvailable = redDoorDone && blueDoorDone;
        if (doorController != null)
        {
            doorController.UpdateDoorState(directConnection);
        }

        if (redDoorController != null)
        {
            redDoorController.SetProgress(hasRedTruth ? 1 : redDoorDone && directConnection < 0 ? -1 : 0);
        }

        if (blueDoorController != null)
        {
            blueDoorController.SetProgress(hasBlueTruth ? 1 : blueDoorDone && directConnection < 0 ? -1 : 0);
        }

        if (endingManager != null)
        {
            endingManager.UpdateProgressFeedback(directConnection);
        }

        if (dialogueUI != null && !gameEnded)
        {
            dialogueUI.SetStatusText(GetStatusText());
        }
    }

    private string GetStatusText()
    {
        // Open with a push toward the third door — the player should try it first and discover it won't open.
        if (!thirdDoorTried && !redDoorDone && !blueDoorDone)
        {
            return "目标：推开面前的门。";
        }

        if (!redDoorDone && !blueDoorDone)
        {
            return "目标：那扇门没动。先去听她和他说话。";
        }

        if (!redDoorDone)
        {
            return "目标：靠近红门，听他的话。";
        }

        if (!blueDoorDone)
        {
            return "目标：靠近蓝门，听她的话。";
        }

        if (finalChoiceAvailable)
        {
            return "目标：回到中间，走向第三扇门。";
        }

        return string.Empty;
    }
}
