using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    private static GameStateManager instance;
    public static GameStateManager Instance => instance != null ? instance : (instance = FindObjectOfType<GameStateManager>());

    // Preserve the original scene layout; the model owns the new rules.
    public int directConnection;
    public bool hasRedTruth, hasBlueTruth, redDoorDone, blueDoorDone, finalChoiceAvailable, gameEnded, thirdDoorTried;
    [SerializeField] private DoorController doorController;
    [SerializeField] private EndingManager endingManager;
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private SymbolicDoorController redDoorController;
    [SerializeField] private SymbolicDoorController blueDoorController;
    private RelationshipRun run;
    public RelationshipRun Run => run ?? (run = NewCase(CaseSession.Current));
    private static RelationshipRun NewCase(CaseId id)=>new RelationshipRun(id,true,id==CaseId.Office);
    public bool GoodEnding => RelationshipRun.OpensExit(Run.Ending);
    public ConversationRecord LastRecord { get; private set; }

    private void Awake() { instance = this; run = NewCase(CaseSession.Current); }
    private void OnEnable() { LocalizationManager.OnLanguageChanged += UpdateWorldState; }
    private void OnDisable() { LocalizationManager.OnLanguageChanged -= UpdateWorldState; }
    private void Start() { UpdateWorldState(); }
    private void OnApplicationQuit(){if(HasActiveJourney())SaveCheckpoint();}
    private void OnApplicationFocus(bool focused){if(!focused&&HasActiveJourney())SaveCheckpoint();}
    private bool HasActiveJourney()=>IntroSequence.HasPlayed&&!IntroSequence.IsActive&&run!=null&&!run.Ended;

    public void Configure(DoorController door, EndingManager endings, DialogueUI ui)
    { doorController = door; endingManager = endings; dialogueUI = ui; }
    public void ConfigureSymbolicDoors(SymbolicDoorController redDoor, SymbolicDoorController blueDoor)
    { redDoorController = redDoor; blueDoorController = blueDoor; }

    public void BeginCase(CaseId id)
    {
        run=NewCase(id);LastRecord=null;
        UpdateWorldState();SaveCheckpoint();
    }
    public void RestoreRun(RelationshipRun restored)
    {
        if(restored==null||restored.Ended)return;
        run=restored;LastRecord=null;UpdateWorldState();
    }
    public void SaveCheckpoint()
    {
        if(!Run.Ended)CheckpointStore.Save(Run);
    }

    public string ApplyRedDoorChoice(int option) => Respond(0, option);
    public string ApplyBlueDoorChoice(int option) => Respond(1, option);
    private string Respond(int voice, int option)
    {
        LastRecord = Run.Respond(voice, option);
        if (LastRecord == null) return "";
        UpdateWorldState();
        SaveCheckpoint();
        return LastFeedback();
    }
    public string LastFeedback()
    {
        if(LastRecord==null)return "";
        string feedback = NarrativeText.Feedback(LastRecord);
        if (Run.Phase > LastRecord.Phase)
            feedback += "\n" + (Run.Phase == 1 ? NarrativeText.L("下一轮：获得“追问”和“翻译”各一张。", "NEXT ROUND: one LISTEN and one REFRAME card.") : NarrativeText.L("最后一轮：获得“代偿”和“重述”。G 可撤回上一句。", "FINAL ROUND: TAKE OVER and SAY IT AGAIN. G revises your last response."));
        return feedback;
    }
    public string ApplyFinalChoice(int option)
    {
        if (Run.Ended || option < 0 || option > 2) return "";
        RelationshipEnding outcome = Run.Decide(option);
        if (outcome == RelationshipEnding.None) { UpdateWorldState(); SaveCheckpoint(); return ""; }
        EndGame(outcome);
        return NarrativeText.EndingBody(outcome);
    }
    public void TryUseExitDoor() { if (!Run.Ended && dialogueUI != null) dialogueUI.ShowDialogue(DialogueActor.FinalChoice); }
    public void MarkThirdDoorTried() { thirdDoorTried = true; }
    public void RefreshAfterCorrespondence(){UpdateWorldState();SaveCheckpoint();}
    public bool Rest()
    {
        bool changed = Run.Rest();
        if (changed) {UpdateWorldState();SaveCheckpoint();}
        return changed;
    }
    public bool Rewind()
    {
        if (!Run.CanRewind || dialogueUI == null || dialogueUI.IsDialogueOpen || ExhibitionHUD.Paused) return false;
        int voice = Run.History[Run.History.Count - 1].Voice;
        if (!Run.Rewind()) return false;
        UpdateWorldState();
        SaveCheckpoint();
        dialogueUI.ShowDialogue(voice == 0 ? DialogueActor.RedDoor : DialogueActor.BlueDoor);
        return true;
    }
    private void EndGame(RelationshipEnding outcome)
    {
        gameEnded = true; finalChoiceAvailable = false;
        bool exit = RelationshipRun.OpensExit(outcome);
        if (outcome == RelationshipEnding.SharedTable)
        { redDoorController?.OpenDoor(); blueDoorController?.OpenDoor(); }
        else if (outcome == RelationshipEnding.Alliance)
        {
            if (Run.RedTrust > Run.BlueTrust) redDoorController?.OpenDoor();
            else if (Run.BlueTrust > Run.RedTrust) blueDoorController?.OpenDoor();
            else { redDoorController?.SetProgress(1); blueDoorController?.SetProgress(1); }
        }
        else if (outcome == RelationshipEnding.FalsePeace)
        { redDoorController?.SetProgress(2); blueDoorController?.SetProgress(2); }
        else { redDoorController?.DarkenDoor(); blueDoorController?.DarkenDoor(); }
        if (exit) doorController?.OpenDoor(); else doorController?.DarkenDoor();
        if (AudioManager.Instance != null)
        {
            if (exit) AudioManager.Instance.StopLoop();
            AudioManager.Instance.Play(exit ? GameSound.DoorOpenSuccess : GameSound.DoorCloseBad, .55f);
        }
        EndingArchive.Record(outcome);
        CheckpointStore.Clear();
        endingManager?.ShowEnding(outcome, Run);
    }
    private void UpdateWorldState()
    {
        directConnection = Run.Bridge;
        hasRedTruth = Run.RedClarity > 0; hasBlueTruth = Run.BlueClarity > 0;
        redDoorDone = Run.RedDone; blueDoorDone = Run.BlueDone;
        gameEnded = Run.Ended; finalChoiceAvailable = !gameEnded;
        if (!gameEnded)
        {
            doorController?.UpdateDoorState(Run.Bridge);
            redDoorController?.SetProgress(Run.RedClarity > 0 ? 1 : 0);
            blueDoorController?.SetProgress(Run.BlueClarity > 0 ? 1 : 0);
            endingManager?.UpdateProgressFeedback(Run.Bridge);
            dialogueUI?.SetStatusText(NarrativeText.Objective(Run));
        }
    }
}
