using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    public static int LastClosedFrame { get; private set; } = -1;
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private ExhibitionText speakerText;
    [SerializeField] private ExhibitionText lineText;
    [SerializeField] private ExhibitionText resultText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private ExhibitionText[] optionLabels;
    [SerializeField] private ExhibitionText statusText;
    [SerializeField] private ExhibitionText endingText;
    [SerializeField] private PlayerController player;
    [SerializeField] private Image speakerColorBar;
    [SerializeField] private Image speakerUnderline;
    [SerializeField] private Image panelOutline;
    private Button continueButton;
    private ExhibitionText deltaText, inputHint;
    private Coroutine endingRoutine;
    private DialogueActor currentActor;
    private bool choosing, awaitingContinue;
    private System.Func<string> reflectionSpeaker,reflectionLine;
    public bool IsDialogueOpen => dialogueGroup != null && dialogueGroup.alpha > .01f;
    public bool IsChoosing => IsDialogueOpen && choosing;
    public bool AwaitingContinue => IsDialogueOpen && awaitingContinue;
    public bool IsFinalDecision => currentActor == DialogueActor.FinalChoice;
    public bool IsReflection => currentActor == DialogueActor.ExitDoor;
    public int Voice => currentActor == DialogueActor.RedDoor ? 0 : 1;
    public bool EndingVisible => endingText != null && endingText.gameObject.activeSelf;
    public Texture2D ReflectionArtwork { get; private set; }
    private void OnEnable(){LocalizationManager.OnLanguageChanged+=RefreshLanguage;}
    private void OnDisable(){LocalizationManager.OnLanguageChanged-=RefreshLanguage;}

    public void Configure(CanvasGroup group, ExhibitionText speaker, ExhibitionText line, ExhibitionText result, Button[] buttons, ExhibitionText[] labels, ExhibitionText status, ExhibitionText ending, PlayerController controller, Image bar = null, Image underline = null, Image outline = null)
    {
        dialogueGroup = group; speakerText = speaker; lineText = line; resultText = result;
        optionButtons = buttons; optionLabels = labels; statusText = status; endingText = ending;
        player = controller; speakerColorBar = bar; speakerUnderline = underline; panelOutline = outline;
        HideDialogueImmediate();
        endingText.gameObject.SetActive(false);
    }
    public void ConfigureFeedback(Button next, ExhibitionText delta, ExhibitionText hint)
    {
        continueButton = next; deltaText = delta; inputHint = hint;
        continueButton.onClick.AddListener(ContinueResult);
        continueButton.gameObject.SetActive(false);
    }
    public void ShowDialogue(DialogueActor actor)
    {
        var state = GameStateManager.Instance;
        if (state == null || state.Run.Ended) return;
        currentActor = actor; choosing = true; awaitingContinue = false;
        player.SetControlsEnabled(false);
        dialogueGroup.alpha = 1; dialogueGroup.interactable = true; dialogueGroup.blocksRaycasts = true;
        resultText.text = "";
        if (deltaText != null) deltaText.text = "";
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        Color accent = actor == DialogueActor.BlueDoor ? ExhibitionDesign.Ink : ExhibitionDesign.Red;
        if (speakerColorBar != null) speakerColorBar.color = accent;
        if (speakerUnderline != null) speakerUnderline.color = accent;
        if (panelOutline != null) panelOutline.color = accent;
        if (actor == DialogueActor.FinalChoice)
        {
            speakerText.text = NarrativeText.L("03 / 你的位置", "03 / WHERE YOU STAND");
            lineText.text = NarrativeText.FinalLine(state.Run);
        }
        else
        {
            state.Run.SelectCard(ConversationCard.None, Voice);
            speakerText.text = NarrativeText.Speaker(Voice) + "    /    " + (state.Run.Phase + 1) + "/3  " + NarrativeText.PhaseName(state.Run.Phase);
            lineText.text = NarrativeText.Line(state.Run, Voice);
        }
        RefreshOptions();
    }
    public void RefreshOptions()
    {
        if (!IsChoosing) return;
        var run = GameStateManager.Instance.Run;
        for (int i = 0; i < 3; i++)
        {
            optionButtons[i].gameObject.SetActive(true);
            optionButtons[i].onClick.RemoveAllListeners();
            int index = i;
            optionButtons[i].onClick.AddListener(() => Choose(index));
            string caption = IsFinalDecision ? NarrativeText.FinalChoice(run, i) : NarrativeText.Choice(run, Voice, i);
            string preview;
            if (IsFinalDecision) preview = i == 0 && !run.AllVoicesHeard ? NarrativeText.L("回到展厅，继续这一局", "RETURN TO THE GALLERY") : NarrativeText.L("结束本次旅程", "ENDS THIS JOURNEY");
            else { int[] d = run.Preview(Voice, i); preview = NarrativeText.Delta(d[0], d[1], d[2])+"  /  "+NarrativeText.ChoiceTag(run,Voice,i); }
            optionLabels[i].supportRichText = true;
            optionLabels[i].text = "0" + (i + 1) + "  /  " + caption + "\n<size=18>        " + preview + "</size>";
        }
        if (inputHint != null) inputHint.text = IsFinalDecision
            ? NarrativeText.L("1–3 决定自己的位置 / Tab 回看 / ESC 暂停", "1–3 CHOOSE YOUR POSITION / TAB REVISIT / ESC PAUSE")
            : NarrativeText.L("1–3 回应 / Q 切换动作卡 / Tab 手记 / ESC 先返回展厅", "1–3 RESPOND / Q CYCLE CARD / TAB NOTES / ESC RETURN TO GALLERY");
    }
    public void ShowOneShotLine(string speaker, string line, float closeDelay = 1.8f)
    {
        ReflectionArtwork=null;
        reflectionSpeaker=()=>speaker;reflectionLine=()=>line;
        ShowReflection();
    }
    public void ShowLocalizedReflection(System.Func<string> speaker,System.Func<string> line)
    {
        ReflectionArtwork=null;
        reflectionSpeaker=speaker;reflectionLine=line;ShowReflection();
    }
    public void ShowArtwork(Texture2D artwork,System.Func<string> speaker,System.Func<string> line)
    {ReflectionArtwork=artwork;reflectionSpeaker=speaker;reflectionLine=line;ShowReflection();}
    private void ShowReflection()
    {
        choosing = false; awaitingContinue = true;
        currentActor = DialogueActor.ExitDoor;
        player.SetControlsEnabled(false);
        dialogueGroup.alpha = 1; dialogueGroup.interactable = true; dialogueGroup.blocksRaycasts = true;
        speakerText.text = reflectionSpeaker(); lineText.text = reflectionLine(); resultText.text = "";
        if (deltaText != null) deltaText.text = "";
        foreach (var button in optionButtons) button.gameObject.SetActive(false);
        ShowContinue();
    }
    private void Update()
    {
        if (ExhibitionHUD.Paused || NarrativeNotebook.Open || IntroSequence.IsActive) return;
        if (AwaitingContinue && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))) { ContinueResult(); return; }
        if (!IsChoosing) return;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) Choose(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) Choose(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) Choose(2);
    }
    public void Choose(int option)
    {
        if (!IsChoosing || option < 0 || option > 2 || ExhibitionHUD.Paused || NarrativeNotebook.Open) return;
        var state = GameStateManager.Instance;
        AudioManager.Instance?.Play(GameSound.ChoiceClick, .45f);
        choosing = false;
        string result = IsFinalDecision ? state.ApplyFinalChoice(option) : Voice == 0 ? state.ApplyRedDoorChoice(option) : state.ApplyBlueDoorChoice(option);
        if (state.Run.Ended || IsFinalDecision) { HideDialogueImmediate(); if (!state.Run.Ended) player.SetControlsEnabled(true); return; }
        foreach (var button in optionButtons) button.gameObject.SetActive(false);
        resultText.text = result;
        var record = state.LastRecord;
        if (deltaText != null && record != null) deltaText.text = NarrativeText.Delta(record.CapacityAfter - record.CapacityBefore, record.LoadAfter - record.LoadBefore, record.BridgeAfter - record.BridgeBefore);
        awaitingContinue = true;
        ShowContinue();
    }
    private void ShowContinue()
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.GetComponentInChildren<ExhibitionText>().text = ReflectionArtwork!=null?NarrativeText.L("空格 / E    返回展厅","SPACE / E    RETURN TO THE GALLERY"):NarrativeText.L("空格 / E    继续", "SPACE / E    CONTINUE");
        }
        if (inputHint != null) inputHint.text = ReflectionArtwork!=null?NarrativeText.L("随时可以回来再看。","You can come back to this print whenever you like."):NarrativeText.L("读完再继续。余力为 0 时，追问不会增加理解；可去 00 长椅休息。", "Read at your own pace. At 0 BREATH, asking adds no insight. Rest at bench 00.");
    }
    public void ContinueResult()
    {
        if (!AwaitingContinue || ExhibitionHUD.Paused || NarrativeNotebook.Open) return;
        HideDialogueImmediate();
        if (!GameStateManager.Instance.Run.Ended||IsReflection) player.SetControlsEnabled(true);
    }
    public bool CancelChoice()
    {
        if(!IsChoosing||IsFinalDecision||ExhibitionHUD.Paused||NarrativeNotebook.Open)return false;
        HideDialogueImmediate();player.SetControlsEnabled(true);return true;
    }
    public void ShowEndingText(string text)
    {
        if (endingText == null) return;
        if (endingRoutine != null) StopCoroutine(endingRoutine);
        if (string.IsNullOrEmpty(text)) { endingText.gameObject.SetActive(false); return; }
        endingRoutine = StartCoroutine(EndingFadeIn(text));
    }
    private IEnumerator EndingFadeIn(string text)
    {
        yield return new WaitForSeconds(1.4f);
        endingText.text = GameStateManager.Instance!=null&&GameStateManager.Instance.Run.Ended?NarrativeText.EndingText(GameStateManager.Instance.Run):text; endingText.gameObject.SetActive(true);
        if (!ExhibitionHUD.Paused && !NarrativeNotebook.Open) player.SetControlsEnabled(true);
        for (float t = 0; t < 1; t += Time.deltaTime / 1.2f)
        { Color c = endingText.color; c.a = t; endingText.color = c; yield return null; }
        Color final = endingText.color; final.a = 1; endingText.color = final;
    }
    public void SetStatusText(string text) { if (statusText != null) statusText.text = text; }
    private void RefreshLanguage()
    {
        var state=GameStateManager.Instance;if(state==null||dialogueGroup==null)return;
        if(EndingVisible)endingText.text=NarrativeText.EndingText(state.Run);
        if(!IsDialogueOpen)return;
        if(IsReflection)
        {
            if(reflectionSpeaker!=null)speakerText.text=reflectionSpeaker();
            if(reflectionLine!=null)lineText.text=reflectionLine();
            ShowContinue();return;
        }
        if(IsFinalDecision)
        {speakerText.text=NarrativeText.L("03 / 你的位置","03 / WHERE YOU STAND");lineText.text=NarrativeText.FinalLine(state.Run);RefreshOptions();return;}
        var record=state.LastRecord;
        int phase=AwaitingContinue&&record!=null?record.Phase:state.Run.Phase;
        speakerText.text=NarrativeText.Speaker(Voice)+"    /    "+(phase+1)+"/3  "+NarrativeText.PhaseName(phase);
        lineText.text=AwaitingContinue&&record!=null?NarrativeText.RecordedLine(record):NarrativeText.Line(state.Run,Voice);
        if(IsChoosing)RefreshOptions();
        else if(AwaitingContinue&&record!=null)
        {
            resultText.text=state.LastFeedback();
            if(deltaText!=null)deltaText.text=NarrativeText.Delta(record.CapacityAfter-record.CapacityBefore,record.LoadAfter-record.LoadBefore,record.BridgeAfter-record.BridgeBefore);
            ShowContinue();
        }
    }
    private void HideDialogueImmediate()
    {
        if (dialogueGroup == null) return;
        dialogueGroup.alpha = 0; dialogueGroup.interactable = false; dialogueGroup.blocksRaycasts = false;
        LastClosedFrame = Time.frameCount;
        choosing = awaitingContinue = false;
        ReflectionArtwork=null;
        if (continueButton != null) continueButton.gameObject.SetActive(false);
    }
}
