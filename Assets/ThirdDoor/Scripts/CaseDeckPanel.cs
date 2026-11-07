using UnityEngine;
using UnityEngine.UI;
using static NarrativeText;

public sealed class CaseDeckPanel : MonoBehaviour
{
    private GameObject panel;
    private NarrativeNotebook book;
    private PlayerController player;
    private DialogueUI dialogue;
    private Button[] cards,presets;
    private Button confirm,back;
    private ExhibitionText heading,rule,hand,hint,selection;
    private int draft,pendingVoice=-1,revision=-1;
    private Language language;
    private string error="";
    public bool IsOpen=>panel!=null&&panel.activeSelf;
    public int DraftMask=>draft;
    public void Configure(GameObject root,NarrativeNotebook notebook,PlayerController controller,DialogueUI ui,Button[] choices,Button[] quick,Button apply,Button cancel,ExhibitionText title,ExhibitionText rules,ExhibitionText selected,ExhibitionText combos,ExhibitionText count)
    {
        panel=root;book=notebook;player=controller;dialogue=ui;cards=choices;presets=quick;confirm=apply;back=cancel;heading=title;rule=rules;hand=selected;hint=combos;selection=count;
        for(int i=0;i<9;i++){int slot=i;cards[i].onClick.AddListener(()=>Toggle(slot));}
        for(int i=0;i<3;i++){int id=i;presets[i].onClick.AddListener(()=>{var r=GameStateManager.Instance.Run;draft=r.Phase==0?RelationshipRun.DeckPreset(id):r.DeckMask;error="";Refresh();});}
        confirm.onClick.AddListener(Confirm);back.onClick.AddListener(book.Close);panel.SetActive(false);
    }
    public void Show(int voice)
    {pendingVoice=voice;draft=GameStateManager.Instance.Run.DeckMask;error="";panel.SetActive(true);Refresh();}
    public void Hide(){if(panel!=null)panel.SetActive(false);}
    private void Toggle(int slot)
    {
        var run=GameStateManager.Instance.Run;if(run.DeckReady||run.Ended)return;
        int bit=1<<slot;
        if((draft&bit)!=0)draft&=~bit;
        else if(RelationshipRun.BitCount(draft)<5)draft|=bit;
        else {error=L("先移除一张，再选新牌。","Remove a card before adding another.");Refresh();return;}
        error="";Refresh();
    }
    public void Confirm()
    {
        var state=GameStateManager.Instance;var run=state.Run;
        if(run.Ended){book.Close();return;}
        if(!run.DeckReady&&!run.PrepareDeck(draft))return;
        state.RefreshAfterCorrespondence();AudioManager.Instance?.Play(GameSound.ChoiceClick,.35f);book.Close();
        if(pendingVoice>=0&&run.CanTalk(pendingVoice)&&CorrespondenceFolio.NearbyVoice(player)==pendingVoice)
            dialogue.ShowDialogue(pendingVoice==0?DialogueActor.RedDoor:DialogueActor.BlueDoor);
    }
    private void Update(){if(IsOpen&&(revision!=GameStateManager.Instance.Run.Revision||language!=LocalizationManager.Current))Refresh();}
    public void Refresh()
    {
        if(!IsOpen)return;
        var run=GameStateManager.Instance.Run;if(!run.DeckRules){Hide();return;}
        revision=run.Revision;language=LocalizationManager.Current;bool read=run.DeckReady||run.Ended;
        heading.text=L("五张牌，三场传话","FIVE CARDS, THREE SCENES")+"    "+(run.Phase+1)+" / 3";
        rule.text=read?L("本场手牌 / 每场各用一次；下一场恢复，并可换一张。","THIS SCENE'S HAND / Once per scene. Refresh next scene; swap up to one."):run.Phase==0?L("九张选五张，全程可见。可以先选一套，再调整。","Choose five of nine, all visible. Start with a hand below, then adjust it."):L("下一场可以换一张，也可保留原牌组。确认后恢复本场使用次数。","Swap up to one card, or keep this hand. Confirm to refresh uses for this scene.");
        for(int i=0;i<9;i++)
        {
            var tool=(DispatchTool)(i+1);bool selected=(draft&(1<<i))!=0;var label=cards[i].GetComponentInChildren<ExhibitionText>();
            label.text=(selected?"● ":"")+CaseDeckText.Name(tool)+(read?"  ×"+run.DeckStock(tool):"")+"\n"+CaseDeckText.Detail(tool);
            cards[i].interactable=!read;var colors=cards[i].colors;colors.normalColor=selected?ExhibitionDesign.Red:ExhibitionDesign.Ink;colors.selectedColor=colors.normalColor;colors.highlightedColor=Color.Lerp(colors.normalColor,ExhibitionDesign.Red,.3f);colors.disabledColor=selected?ExhibitionDesign.Red:ExhibitionDesign.Ink;cards[i].colors=colors;label.color=ExhibitionDesign.Paper;
        }
        for(int i=0;i<3;i++)
        {
            presets[i].interactable=!read&&(run.Phase==0||i==0);
            presets[i].GetComponentInChildren<ExhibitionText>().text=run.Phase>0&&i==0?L("保留原来五张","KEEP THIS HAND"):CaseDeckText.Preset(i);
        }
        hand.text=L("选入手牌\n\n","YOUR HAND\n\n");int number=0;
        for(int i=0;i<9;i++)if((draft&(1<<i))!=0)hand.text+=(++number)+" / "+CaseDeckText.Name((DispatchTool)(i+1))+(read?"  ×"+run.DeckStock((DispatchTool)(i+1)):"")+"\n";
        hint.text=L("核对 → 原话\n已核对时，原话不加承重。\n\n停顿 → 翻译\n下一次翻译省余力，留两段工夫。\n\n代办 → 交接\n送到后把清单交回本人。","VERIFY → QUOTE\nChecked words add no extra load.\n\nPAUSE → REFRAME\nSave breath; room for two pieces.\n\nTASK → HANDOFF\nDeliver, then hand the list back.");
        int count=RelationshipRun.BitCount(draft);selection.text=error.Length>0?error:L("已选 ","SELECTED ")+count+" / 5"+(run.Phase>0&&!read&&RelationshipRun.BitCount(draft^run.DeckMask)>2?L(" · 这一场最多换一张"," · SWAP AT MOST ONE"):"");
        confirm.interactable=read||run.CanPrepareDeck(draft);
        confirm.GetComponentInChildren<ExhibitionText>().text=read?L("返回展厅","RETURN TO THE GALLERY"):L("带上这五张 →","TAKE THIS HAND →");
        back.GetComponentInChildren<ExhibitionText>().text=L("先不改 / 返回展厅","LEAVE IT / RETURN");
    }
}
