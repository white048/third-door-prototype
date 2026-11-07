using UnityEngine;
using UnityEngine.UI;
using static NarrativeText;

// The same finite deck is used here and in door replies. Opening a draft spends nothing.
public class DispatchComposer : MonoBehaviour
{
    private GameObject panel;
    private CorrespondenceFolio folio;
    private PlayerController player;
    private Button[] parts,tools;
    private Button send,close,clear;
    private ExhibitionText title,actor,progress,words,cost,response,help;
    private int source,revision=-1;
    private Language language;
    private DispatchFragment first,second;
    private DispatchTool tool;
    private CorrespondenceEvent last;
    public bool IsOpen=>panel!=null&&panel.activeSelf;
    public DispatchFragment First=>first;
    public DispatchFragment Second=>second;
    public DispatchTool Tool=>tool;
    public void Configure(GameObject root,CorrespondenceFolio f,PlayerController p,Button[] fragments,Button[] cards,Button submit,Button back,Button reset,ExhibitionText heading,ExhibitionText recipient,ExhibitionText status,ExhibitionText packet,ExhibitionText price,ExhibitionText reaction,ExhibitionText instructions)
    {
        panel=root;folio=f;player=p;parts=fragments;tools=cards;send=submit;close=back;clear=reset;
        title=heading;actor=recipient;progress=status;words=packet;cost=price;response=reaction;help=instructions;
        for(int i=0;i<4;i++){var part=(DispatchFragment)(i+1);parts[i].onClick.AddListener(()=>Toggle(part));}
        for(int i=0;i<tools.Length;i++){var card=(DispatchTool)i;tools[i].onClick.AddListener(()=>{tool=card;Refresh();});}
        send.onClick.AddListener(()=>folio.OpenPacketProof(source,first,second,tool));
        close.onClick.AddListener(Close);clear.onClick.AddListener(()=>{first=second=DispatchFragment.None;Refresh();});
        panel.SetActive(false);
    }
    public void Show(int voice)
    {
        source=voice;first=second=DispatchFragment.None;tool=DispatchTool.None;last=null;
        panel.SetActive(true);Refresh();
    }
    public void Close(){if(panel!=null)panel.SetActive(false);folio.CloseProof();}
    private void Toggle(DispatchFragment part)
    {
        if(first==part){first=second;second=DispatchFragment.None;}
        else if(second==part)second=DispatchFragment.None;
        else if(first==DispatchFragment.None)first=part;
        else if(second==DispatchFragment.None)second=part;
        else {first=second;second=part;}
        Refresh();
    }
    public void Resolved(CorrespondenceEvent result)
    {last=result;first=second=DispatchFragment.None;tool=DispatchTool.None;Refresh();}
    private void Update()
    {if(IsOpen&&(revision!=GameStateManager.Instance.Run.Revision||language!=LocalizationManager.Current))Refresh();}
    private static void Style(Button b,bool selected)
    {
        var c=b.colors;c.normalColor=selected?ExhibitionDesign.Red:ExhibitionDesign.Ink;c.selectedColor=c.normalColor;c.highlightedColor=Color.Lerp(c.normalColor,ExhibitionDesign.Red,.25f);b.colors=c;
        b.GetComponentInChildren<ExhibitionText>().color=b.interactable?ExhibitionDesign.Paper:ExhibitionDesign.Ink;
    }
    public void Refresh()
    {
        if(!IsOpen)return;
        var run=GameStateManager.Instance.Run;var m=run.MessageFrom(source);
        if(m==null){Close();return;}
        revision=run.Revision;language=LocalizationManager.Current;
        title.text="01 / 02    "+L("把话带到","MAKE IT HEARD")+"     "+CorrespondenceText.Source(source)+" → "+CorrespondenceText.Source(1-source);
        if(run.DeckRules)title.text=(run.Phase+1)+" / 3   "+CaseDeckText.Phase(run.Phase)+"    "+CorrespondenceText.Source(source)+" → "+CorrespondenceText.Source(1-source);
        actor.text=DispatchText.Actor(run,m)+"\n"+DispatchText.TimeClue(run,m);
        progress.text=DispatchText.Progress(m)+"\n"+L("接话次数 ","ATTEMPTS ")+m.DispatchAttempts+" / 4"+(m.DispatchAttempts==4?L(" · 代偿可再接一次"," · TAKE OVER ALLOWS ONE MORE"):"");
        for(int i=0;i<4;i++)
        {
            var p=(DispatchFragment)(i+1);bool available=run.FragmentAvailable(m,p);
            parts[i].interactable=available&&run.CanDraft(m.Id,1-source);
            string slot=first==p?"1 → ":second==p?"2 → ":"";
            parts[i].GetComponentInChildren<ExhibitionText>().text=slot+DispatchText.Name(p)+"\n"+(p==DispatchFragment.Need&&!m.HasNeed?L("这句还没听到。","You haven't heard this yet."):DispatchText.Fragment(run.Case,source,p,tool));
            Style(parts[i],first==p||second==p);
        }
        int visibleTool=0;
        for(int i=0;i<tools.Length;i++)
        {
            var t=(DispatchTool)i;bool visible=run.DeckRules?(i==0||run.InDeck(t)):i<7;tools[i].gameObject.SetActive(visible);if(!visible)continue;
            if(run.DeckRules)
            {
                var rect=tools[i].GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(35+(visibleTool%3)*275,-590-(visibleTool/3)*62);rect.sizeDelta=new Vector2(260,52);
                var label=tools[i].GetComponentInChildren<ExhibitionText>();label.rectTransform.sizeDelta=new Vector2(232,52);visibleTool++;
            }
            tools[i].interactable=run.CanUseDispatchTool(source,t);
            tools[i].GetComponentInChildren<ExhibitionText>().text=DispatchText.ToolName(t)+(i==0?"":"  ×"+(run.DeckRules?run.DeckStock(t):run.Stock(RelationshipRun.CardFor(t))));
            Style(tools[i],tool==t);
        }
        bool emptyAction=tool==DispatchTool.Listen||(run.DeckRules&&(tool==DispatchTool.Check||tool==DispatchTool.Receipt||tool==DispatchTool.Handoff));
        var preview=run.PreviewPacket(m.Id,first,second,tool,CorrespondenceFolio.NearbyVoice(player));
        words.text=first==DispatchFragment.None&&!emptyAction?L("先点一段，再点一段。\n点击已选的话可移除；先选的先说。","Choose a piece, then another.\nTap a selected piece to remove it. First picked, first spoken."):DispatchText.Packet(run.Case,source,first,second,tool,run.Phase,preview!=null&&preview.ConsentRenewed);
        send.interactable=preview!=null;
        // Do not solve the encounter by revealing the recipient's future answer in a preview.
        cost.text=DispatchText.ToolDetail(tool)+"\n"+L("余力 ","BREATH ")+run.Capacity+" / 8    "+L("承重 ","LOAD ")+run.Load+" / 8";
        response.text=last==null?L("门里还有动静。先说哪句？","You can hear someone inside. What do you lead with?"):DispatchText.Reaction(last)+"\n\n"+CorrespondenceText.Delta(last);
        if(run.DeckRules&&m.TaskActive&&m.PublicProgress==3)response.text=L("你整理的清单已送到，但这件事还挂在你名下。\n可空着两格用交接卡，或返回信封点交接按钮。","Your list arrived, but the task is still under your name.\nUse HANDOFF with empty slots, or return to the envelope's task button.");
        if(preview==null&&(first==DispatchFragment.Guess||second==DispatchFragment.Guess)&&(first==DispatchFragment.Fact||first==DispatchFragment.Request||second==DispatchFragment.Fact||second==DispatchFragment.Request)&&tool!=DispatchTool.Reframe)
            response.text=L("补出的保证要单独说。\n或用“翻译”，把它改成待确认的问题。","Make your added promise separately.\nOr use REFRAME to turn it into an unconfirmed question.");
        if(preview==null&&!run.PublicPlanVerified(source))response.text+="\n"+(m.DispatchAttempts>=4?L("这封话的接话次数已用完，可以离开，或用代偿再试一次。","No ordinary attempts left. Leave, or use TAKE OVER for one more."):run.Capacity==0?L("没有余力了。去长椅歇歇，或用停顿。","No breath left. Rest at the bench, or use PAUSE."):"");
        send.GetComponentInChildren<ExhibitionText>().text=L("看一遍，再开口 →","READ IT BACK, THEN SPEAK →");
        close.GetComponentInChildren<ExhibitionText>().text=L("返回信封 / ESC","BACK TO ENVELOPE / ESC");
        clear.GetComponentInChildren<ExhibitionText>().text=L("清空顺序","CLEAR ORDER");
        help.text=L("每封信最多接话四次，没听完的部分会留下。\n原话 / 翻译：先了解说话人；界限：先了解收信人。\n卡牌与门前回应共用，每种一张。","Four attempts per envelope. Unfinished words stay with you.\nQUOTE / REFRAME: know the speaker; BOUNDARY: know the recipient.\nThese are the same single-use cards as in your door replies.");
        if(run.DeckRules)help.text=L("本场五张牌各用一次，下一场恢复。\n已核对 → 原话省承重；停顿 → 翻译省余力。\n代办清单送达后须交接；核对与交接也有基本动作。","Five cards, once each this scene; refresh next scene.\nChecked → QUOTE saves load; PAUSE → REFRAME saves breath.\nHand back your list. Basic checking and handoff are also available.");
    }
}
