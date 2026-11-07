using UnityEngine;
using UnityEngine.UI;

public class NarrativeNotebook : MonoBehaviour
{
    public static bool Open { get; private set; }
    public static int EscapeFrame { get; private set; } = -1;
    private PlayerController player;
    private DialogueUI dialogue;
    private CanvasGroup group;
    private ExhibitionText title,explanation,margin;
    private ExhibitionText[] memories,history,changes,endingTitles,endingBodies;
    private Image[] endingCards;
    private GameObject currentPage,archivePage;
    private Button current,archive,rewind,titleArchive,close;
    private bool viewingArchive;
    private int revision=-1,known=-1;
    private Language language;
    private CaseId activeCase;
    private GameObject trace;
    private ExhibitionText traceTitle,traceSource,traceReply,traceConsequence;
    private Button traceClose;
    private Button[] readButtons;
    private int traceIndex=-1;
    private bool viewingCorrespondence;
    private GameObject correspondencePage;
    private Button correspondenceTab;
    private CorrespondenceFolio folio;
    private GameObject ribbonPage;
    private Button ribbonTab;
    private NarrativeRibbon ribbon;
    private bool viewingRibbon;
    private CaseDeckPanel deckPanel;
    public void ConfigureDeck(CaseDeckPanel panel){deckPanel=panel;}
    public void ShowDeck(int voice=-1)
    {
        if(ExhibitionHUD.Paused||IntroSequence.IsActive||deckPanel==null)return;
        viewingArchive=viewingCorrespondence=viewingRibbon=false;Present();deckPanel.Show(voice);
    }

    public void Configure(GameObject paper,PlayerController controller,DialogueUI ui,ExhibitionText heading,Button closeButton,Button currentButton,Button archiveButton,GameObject notes,ExhibitionText definitions,ExhibitionText[] needs,ExhibitionText[] ledger,ExhibitionText[] deltas,GameObject endings,ExhibitionText[] names,ExhibitionText[] bodies,Image[] cards,ExhibitionText note,Button revise,Button titleButton)
    {
        player=controller;dialogue=ui;title=heading;close=closeButton;current=currentButton;archive=archiveButton;
        currentPage=notes;explanation=definitions;memories=needs;history=ledger;changes=deltas;archivePage=endings;
        endingTitles=names;endingBodies=bodies;endingCards=cards;margin=note;rewind=revise;titleArchive=titleButton;
        group=paper.AddComponent<CanvasGroup>();
        close.onClick.AddListener(Close);
        current.onClick.AddListener(()=>Show(false));archive.onClick.AddListener(()=>Show(true));
        titleArchive.onClick.AddListener(()=>Show(true));
        rewind.onClick.AddListener(()=>{if(GameStateManager.Instance.Run.DeckRules){ShowDeck();return;}Close();GameStateManager.Instance.Rewind();});
        Open=false;group.alpha=0;group.blocksRaycasts=group.interactable=false;
    }
    public void ConfigureTrace(GameObject panel,ExhibitionText heading,ExhibitionText source,ExhibitionText reply,ExhibitionText consequence,Button closeButton,Button[] rows)
    {
        trace=panel;traceTitle=heading;traceSource=source;traceReply=reply;traceConsequence=consequence;traceClose=closeButton;readButtons=rows;
        for(int i=0;i<rows.Length;i++){int index=i;rows[i].onClick.AddListener(()=>ReadResponse(index));}
        traceClose.onClick.AddListener(CloseTrace);trace.SetActive(false);
    }
    public void ConfigureCorrespondence(GameObject page,Button tab,CorrespondenceFolio content)
    {
        correspondencePage=page;correspondenceTab=tab;folio=content;
        tab.onClick.AddListener(()=>ShowCorrespondence());page.SetActive(false);
    }
    public void ShowCorrespondence(int atVoice=-1)
    {
        if(ExhibitionHUD.Paused||IntroSequence.IsActive)return;
        viewingArchive=false;viewingCorrespondence=true;viewingRibbon=false;folio.FocusDoor(atVoice);Present();
    }
    public void ConfigureRibbon(GameObject page,Button tab,NarrativeRibbon reader)
    {ribbonPage=page;ribbonTab=tab;ribbon=reader;tab.onClick.AddListener(ShowRibbon);page.SetActive(false);}
    public void ShowRibbon()
    {
        if(ExhibitionHUD.Paused||IntroSequence.IsActive)return;
        viewingArchive=viewingCorrespondence=false;viewingRibbon=true;Present();
    }
    public void ReadResponse(int index)
    {
        var run=GameStateManager.Instance.Run;if(index<0||index>=run.History.Count)return;
        traceIndex=index;trace.SetActive(true);RefreshTrace();
    }
    private void CloseTrace(){traceIndex=-1;if(trace!=null)trace.SetActive(false);}
    private void RefreshTrace()
    {
        if(traceIndex<0||traceIndex>=GameStateManager.Instance.Run.History.Count)return;
        var record=GameStateManager.Instance.Run.History[traceIndex];
        traceTitle.text=NarrativeText.L("逐句回看 / ","REVISIT A RESPONSE / ")+(record.Phase+1)+"·"+(record.Voice+1);
        traceClose.GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("返回手记 / ESC","BACK / ESC");
        traceSource.text=NarrativeText.L("对方说给你的：\n\n","WHAT THEY SAID:\n\n")+NarrativeText.RecordedLine(record);
        traceReply.text=NarrativeText.L("你选择的回应：\n\n","WHAT YOU SAID:\n\n")+NarrativeText.RecordReply(record);
        traceConsequence.text=NarrativeText.Feedback(record)+"\n\n"+NarrativeText.Delta(record.CapacityAfter-record.CapacityBefore,record.LoadAfter-record.LoadBefore,record.BridgeAfter-record.BridgeBefore);
    }
    private void Update()
    {
        if(ExhibitionHUD.Paused)return;
        if(Open&&Input.GetKeyDown(KeyCode.F2))LocalizationManager.ToggleLanguage();
        if(Input.GetKeyDown(KeyCode.Tab)){if(Open)Close();else Show(IntroSequence.IsActive);}
        else if(Input.GetKeyDown(KeyCode.V)&&!IntroSequence.IsActive&&!dialogue.IsDialogueOpen){if(Open&&viewingRibbon)Close();else ShowRibbon();}
        else if(Open&&Input.GetKeyDown(KeyCode.Escape)){EscapeFrame=Time.frameCount;if(deckPanel!=null&&deckPanel.IsOpen)deckPanel.Hide();else if(traceIndex>=0)CloseTrace();else if(folio!=null&&folio.ProofOpen)folio.CloseProof();else if(folio!=null&&folio.ComposerOpen)folio.CloseComposer();else Close();}
        else if(!Open&&!IntroSequence.IsActive&&!dialogue.IsDialogueOpen&&Input.GetKeyDown(KeyCode.G))GameStateManager.Instance?.Rewind();
        var state=GameStateManager.Instance;if(state==null)return;
        if(revision!=state.Run.Revision||language!=LocalizationManager.Current||known!=EndingArchive.Count||activeCase!=CaseInfo.Active)Refresh();
    }
    public void Show(bool endings)
    {
        if(ExhibitionHUD.Paused)return;
        viewingArchive=endings||IntroSequence.IsActive;Open=true;
        viewingCorrespondence=viewingRibbon=false;Present();
    }
    private void Present()
    {
        if(deckPanel!=null)deckPanel.Hide();
        Open=true;
        if(folio!=null){folio.CloseProof();folio.CloseComposer();}
        CloseTrace();
        group.alpha=1;group.blocksRaycasts=group.interactable=true;
        player.SetControlsEnabled(false);Refresh();
    }
    public void Close()
    {
        if(deckPanel!=null)deckPanel.Hide();
        Open=false;group.alpha=0;group.blocksRaycasts=group.interactable=false;
        if(folio!=null){folio.CloseProof();folio.CloseComposer();}
        CloseTrace();
        player.SetControlsEnabled(!IntroSequence.IsActive&&!dialogue.IsDialogueOpen&&!ExhibitionHUD.Paused);
    }
    private void Refresh()
    {
        var state=GameStateManager.Instance;if(state==null)return;
        var run=state.Run;revision=run.Revision;language=LocalizationManager.Current;known=EndingArchive.Count;activeCase=CaseInfo.Active;
        title.text=viewingRibbon?"A SENTENCE GETS HEAVY":viewingCorrespondence?"WORDS IN YOUR HANDS":viewingArchive?"SEVEN WAYS TO STAND":run.Case==CaseId.Office?"A REASON LEFT BLANK":"WORDS IN TRANSIT";
        close.GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("Tab / 返回","TAB / RETURN");
        current.GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("本局手记","CURRENT NOTES");
        archive.GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("走过的位置 ","POSITIONS FOUND ")+known+" / 7";
        titleArchive.GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("档案 ","ARCHIVE ")+known+" / 7";
        current.interactable=!IntroSequence.IsActive;
        if(correspondenceTab!=null)
        {
            correspondenceTab.interactable=!IntroSequence.IsActive;
            correspondenceTab.GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("传话夹 ","CORRESPONDENCE ")+run.HeldMessages+" / 2";
            correspondencePage.SetActive(viewingCorrespondence);if(viewingCorrespondence)folio.Refresh();
        }
        if(ribbonTab!=null)
        {
            ribbonTab.interactable=!IntroSequence.IsActive;
            ribbonTab.GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("拼贴长卷 / V","COLLAGE STRIP / V");
            ribbonPage.SetActive(viewingRibbon);if(viewingRibbon)ribbon.Refresh(run);
        }
        StyleTab(current,!viewingArchive&&!viewingCorrespondence&&!viewingRibbon);
        StyleTab(archive,viewingArchive);StyleTab(correspondenceTab,viewingCorrespondence);StyleTab(ribbonTab,viewingRibbon);
        currentPage.SetActive(!viewingArchive&&!viewingCorrespondence&&!viewingRibbon);archivePage.SetActive(viewingArchive);
        explanation.text=NarrativeText.L("余力为 0 时，先休息。承重达到 6，承接和追问会多耗余力。\n直连表示直接交流的准备程度，仍须本人同意。每轮可休息一次，每种卡仅一张。","At 0 BREATH, rest first. At 6 LOAD, carrying and asking cost extra breath.\nBRIDGE helps direct conversation; consent is still needed. Rest once per round. Each card is single use.");
        if(run.DeckRules)explanation.text=NarrativeText.L("本局选五张策略牌，每场各用一次；下一场恢复，可换一张。\n00 每场休息一次。基本核对、传话和交接始终可用，卡牌只改变做法。","Five strategy cards, once each per scene. Refresh next scene; swap up to one.\nRest once per scene at 00. Basic checking, delivery and handoff remain available.");
        for(int i=0;i<2;i++)memories[i].text=NarrativeText.Speaker(i)+"   /   "+NarrativeText.L("理解 ","INSIGHT ")+run.Clarity(i)+" / 3   ·   "+NarrativeText.L("信任 ","TRUST ")+run.Trust(i)+" / 4\n"+NarrativeText.Memory(run,i);
        for(int i=0;i<6;i++)
        {
            changes[i].text="";
            if(i>=run.History.Count){history[i].text=i==0?NarrativeText.L("还没有回应记录。先去 01 或 02 听听。","No responses recorded yet. Start at 01 or 02."):"";continue;}
            var record=run.History[i];
            // Render the original phase, rather than reinterpreting a past choice as the current round.
            history[i].text=(record.Phase+1)+"·"+(record.Voice+1)+"  /  "+NarrativeText.ShortAction(record)+"  /  "+NarrativeText.CardName(record.Card);
            changes[i].text=NarrativeText.Delta(record.CapacityAfter-record.CapacityBefore,record.LoadAfter-record.LoadBefore,record.BridgeAfter-record.BridgeBefore);
        }
        for(int i=0;i<7;i++)
        {
            var ending=(RelationshipEnding)(i+1);bool discovered=EndingArchive.Known(ending);
            endingTitles[i].text=discovered?NarrativeText.EndingTitle(ending):"0"+(i+1)+" / "+NarrativeText.L("未走过的位置","AN UNVISITED POSITION");
            endingBodies[i].text=discovered?(run.DispatchRules?DispatchText.ArchiveBody(ending):NarrativeText.EndingBody(ending)):NarrativeText.L("换一种回应，房间也会有变化。\n不必把所有结局都走一遍。","Different responses change the room.\nYou don't have to find every ending.");
            endingCards[i].color=run.Ending==ending?new Color(.84f,.69f,.58f):new Color(.84f,.82f,.75f);
        }
        margin.text=viewingArchive?NarrativeText.L("编号按档案排列，不代表结局的好坏。","The numbers organise the archive. They do not rank the endings."):NarrativeText.L("手记保留你听到的话和自己的回应。\n重述可撤回上一句，用过的卡不返还。","Your notes keep what you heard and how you answered.\nRevising takes back the last response, but spent cards stay spent.");
        if(viewingCorrespondence)margin.text=NarrativeText.L("F / 去门前处理传话。私下内容要先问本人。\n交付、询问或退回后，不能再用重述撤回上一句。","F at a door opens correspondence. Ask before sharing private words.\nDelivering, asking or returning a message closes the chance to revise your last response.");
        if(viewingRibbon)margin.text=NarrativeText.L("滚轮 / ↑ ↓ 阅读，左侧按钮跳到各轮。\n长卷保留本局回应和实际传话；V / Tab 返回。","WHEEL / ↑ ↓ to read. Use the left buttons to jump by round.\nThe strip keeps this run's responses and exchanges. V / TAB returns.");
        rewind.gameObject.SetActive(!viewingArchive&&!viewingCorrespondence&&!viewingRibbon);
        rewind.interactable=(run.DeckRules||run.CanRewind)&&!dialogue.IsDialogueOpen&&!IntroSequence.IsActive;
        rewind.GetComponentInChildren<ExhibitionText>().text=run.DeckRules?NarrativeText.L("查看 / 调整五张牌","VIEW / PREPARE YOUR FIVE CARDS"):NarrativeText.L("G / 重述上一句","G / SAY IT AGAIN");
        if(readButtons!=null)for(int i=0;i<readButtons.Length;i++)readButtons[i].interactable=i<run.History.Count;
        if(traceIndex>=0)RefreshTrace();
    }
    private void OnDisable(){Open=false;}
    private static void StyleTab(Button button,bool selected)
    {
        if(button==null)return;var colors=button.colors;
        colors.normalColor=selected?ExhibitionDesign.Red:ExhibitionDesign.Ink;colors.selectedColor=colors.normalColor;
        colors.highlightedColor=Color.Lerp(colors.normalColor,ExhibitionDesign.Red,.3f);button.colors=colors;
    }
}
