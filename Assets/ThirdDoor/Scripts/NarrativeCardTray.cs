using UnityEngine;
using UnityEngine.UI;

public class NarrativeCardTray : MonoBehaviour
{
    private DialogueUI dialogue;
    private ExhibitionText heading,nameText,detail;
    private Button[] cards;
    private Button none;
    private int revision=-1;
    private bool lastChoosing,lastFinal,lastReflection,lastArtwork;
    private Language language;
    public void Configure(DialogueUI ui,ExhibitionText h,ExhibitionText n,ExhibitionText d,Button[] buttons,Button noCard)
    {
        dialogue=ui;heading=h;nameText=n;detail=d;cards=buttons;none=noCard;
        for(int i=0;i<cards.Length;i++){int card=i;cards[i].onClick.AddListener(()=>Select((ConversationCard)card));}
        none.onClick.AddListener(()=>Select(ConversationCard.None));
    }
    public void Select(ConversationCard card)
    {
        if(!dialogue.IsChoosing||dialogue.IsFinalDecision||ExhibitionHUD.Paused||NarrativeNotebook.Open)return;
        var run=GameStateManager.Instance.Run;
        if(run.SelectedCard==card)card=ConversationCard.None;
        if(!run.SelectCard(card,dialogue.Voice))return;
        AudioManager.Instance?.Play(GameSound.ChoiceClick,.28f);
        dialogue.RefreshOptions();
        Refresh();
    }
    private void Update()
    {
        var state=GameStateManager.Instance;if(state==null)return;
        if(!state.Run.DeckRules&&dialogue.IsChoosing&&!dialogue.IsFinalDecision&&!ExhibitionHUD.Paused&&!NarrativeNotebook.Open&&Input.GetKeyDown(KeyCode.Q))
        {
            var run=state.Run;int start=(int)run.SelectedCard;
            for(int step=1;step<=7;step++)
            {
                var candidate=(ConversationCard)(((start+1+step)%7)-1);
                if(run.CanEquip(candidate,dialogue.Voice)){Select(candidate);break;}
            }
        }
        if(revision!=state.Run.Revision||lastChoosing!=dialogue.IsChoosing||lastFinal!=dialogue.IsFinalDecision||lastReflection!=dialogue.IsReflection||lastArtwork!=(dialogue.ReflectionArtwork!=null)||language!=LocalizationManager.Current)Refresh();
    }
    private void Refresh()
    {
        lastArtwork=dialogue.ReflectionArtwork!=null;
        heading.gameObject.SetActive(!lastArtwork);nameText.gameObject.SetActive(!lastArtwork);detail.gameObject.SetActive(!lastArtwork);
        var run=GameStateManager.Instance.Run;revision=run.Revision;lastChoosing=dialogue.IsChoosing;lastFinal=dialogue.IsFinalDecision;lastReflection=dialogue.IsReflection;
        language=LocalizationManager.Current;
        nameText.fontSize=run.DeckRules?25:31;
        bool final=dialogue.IsFinalDecision;
        bool reflection=dialogue.IsReflection;
        detail.rectTransform.sizeDelta=new Vector2(470,final||reflection||run.DeckRules?340:136);
        var display=dialogue.AwaitingContinue&&!reflection&&GameStateManager.Instance.LastRecord!=null?GameStateManager.Instance.LastRecord.Card:run.SelectedCard;
        heading.text=final?NarrativeText.L("是否谈下去 / 是否留下","WHETHER TO TALK / WHETHER TO STAY"):NarrativeText.L("动作卡 / 每种一张","ACTION CARDS / ONE OF EACH");
        nameText.text=final?NarrativeText.L("可以现在离开","YOU CAN LEAVE NOW"):reflection?NarrativeText.L("先歇一会儿","TAKE A MOMENT"):NarrativeText.CardName(display);
        detail.text=final?NarrativeText.L("一起谈，需要双方了解情况并各自答应。\n你也可以先离开。\nTab 可回看原话、回应和传话记录。","Both people need to understand and agree before they talk.\nYou can also leave now.\nTAB revisits the words, responses and messages."):reflection?NarrativeText.L("慢慢读，不用赶。\n空格或 E 返回展厅。","Take your time reading.\nSPACE or E returns to the gallery."):NarrativeText.CardDescription(display);
        for(int i=0;i<cards.Length;i++)
        {
            var card=(ConversationCard)i;cards[i].gameObject.SetActive(!run.DeckRules&&!final&&!reflection);
            bool available=run.CanEquip(card,dialogue.Voice);
            cards[i].interactable=dialogue.IsChoosing&&available;
            string reason=NarrativeText.CardUnavailable(run,card,dialogue.Voice);
            var label=cards[i].GetComponentInChildren<ExhibitionText>();
            label.text=(display==card?"● ":"")+NarrativeText.CardName(card)+(run.Stock(card)>0?"  /  1":"")+"\n<size=16>"+(reason.Length>0?reason:NarrativeText.L("点击选用","SELECT"))+"</size>";
            var colors=cards[i].colors;colors.normalColor=display==card?ExhibitionDesign.Red:new Color(.84f,.82f,.75f);colors.selectedColor=colors.normalColor;colors.highlightedColor=Color.Lerp(colors.normalColor,ExhibitionDesign.Red,.35f);colors.disabledColor=display==card?ExhibitionDesign.Red:new Color(.76f,.75f,.7f,.7f);cards[i].colors=colors;
            label.color=display==card?ExhibitionDesign.Paper:ExhibitionDesign.Ink;
        }
        none.gameObject.SetActive(!run.DeckRules&&!final&&!reflection);none.interactable=dialogue.IsChoosing;
        none.GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("不出牌 / Q 循环切换","NO CARD / Q TO CYCLE");
        if(run.DeckRules&&!lastArtwork)
        {
            heading.text=NarrativeText.L("本局五张手牌","YOUR FIVE CARDS");nameText.text=CaseDeckText.Phase(run.Phase);nameText.fontSize=25;detail.text=CaseDeckText.Hand(run);
        }
    }
}
