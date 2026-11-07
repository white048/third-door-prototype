using System;

[Serializable]
public sealed class CaseDeckData
{
    public int Mask,PreparedPhase=-1;
    public int[] Stock=new int[9],Loadouts=new int[3],Combos=new int[4];
    public CaseDeckData Copy()=>new CaseDeckData{Mask=Mask,PreparedPhase=PreparedPhase,Stock=(int[])Stock.Clone(),Loadouts=(int[])Loadouts.Clone(),Combos=(int[])Combos.Clone()};
}

public enum DispatchCombo { None, CheckedQuote, PacedReframe, ReceiptChain, Handoff }

public sealed partial class RelationshipRun
{
    private CaseDeckData caseDeck;
    public bool DeckRules=>caseDeck!=null;
    public bool DeckReady=>DeckRules&&caseDeck.PreparedPhase==Phase&&BitCount(caseDeck.Mask)==5;
    public int DeckMask=>caseDeck==null?0:caseDeck.Mask;
    public static RelationshipRun DeckCase()=>new RelationshipRun(CaseId.Office,true,true);
    public static int BitCount(int mask){int count=0;while(mask>0){count+=mask&1;mask>>=1;}return count;}
    public int DeckStock(DispatchTool tool)=>DeckRules&&tool>=DispatchTool.Quote&&tool<=DispatchTool.Handoff?caseDeck.Stock[(int)tool-1]:0;
    public bool InDeck(DispatchTool tool)=>DeckRules&&tool>=DispatchTool.Quote&&tool<=DispatchTool.Handoff&&(caseDeck.Mask&(1<<((int)tool-1)))!=0;
    public int ComboCount(DispatchCombo combo)=>DeckRules&&combo>DispatchCombo.None&&combo<=DispatchCombo.Handoff?caseDeck.Combos[(int)combo-1]:0;
    public static int DeckPreset(int id)
    {
        var tools=id==0?new[]{DispatchTool.Check,DispatchTool.Quote,DispatchTool.Receipt,DispatchTool.Pause,DispatchTool.Boundary}:id==1?new[]{DispatchTool.Pause,DispatchTool.Reframe,DispatchTool.Boundary,DispatchTool.Listen,DispatchTool.Check}:new[]{DispatchTool.TakeOver,DispatchTool.Handoff,DispatchTool.Receipt,DispatchTool.Check,DispatchTool.Pause};
        int mask=0;foreach(var tool in tools)mask|=1<<((int)tool-1);return mask;
    }
    public bool CanPrepareDeck(int mask)
    {
        if(!DeckRules||Ended||DeckReady||mask<0||mask>=512||BitCount(mask)!=5)return false;
        return Phase==0||BitCount(mask^caseDeck.Mask)<=2;
    }
    public bool PrepareDeck(int mask)
    {
        if(!CanPrepareDeck(mask))return false;
        caseDeck.Mask=mask;caseDeck.PreparedPhase=Phase;caseDeck.Loadouts[Phase]=mask;
        for(int i=0;i<9;i++)caseDeck.Stock[i]=(mask&(1<<i))!=0?1:0;
        Revision++;return true;
    }
    private bool CanUseDeckTool(int source,DispatchTool tool)
    {
        if(!DeckReady||source<0||source>1||tool<DispatchTool.None||tool>DispatchTool.Handoff)return false;
        if(tool==DispatchTool.None)return true;
        if(DeckStock(tool)==0)return false;
        var m=MessageFrom(source);if(m==null)return false;
        if(tool==DispatchTool.Check)return !m.SourceChecked;
        if(tool==DispatchTool.Receipt){var other=MessageFrom(1-source);return m.PublicProgress>0||(other!=null&&other.PublicProgress==3&&!other.Assumed);}
        if(tool==DispatchTool.Handoff)return m.TaskActive&&m.PublicProgress==3&&!m.Assumed;
        if(tool==DispatchTool.TakeOver)return Capacity>=2;
        if(tool==DispatchTool.Quote)return Clarity(source)>0;
        if(tool==DispatchTool.Reframe)return Clarity(source)>0;
        if(tool==DispatchTool.Boundary)return Clarity(1-source)>0;
        return true;
    }
    public bool CanAdvanceEncounter=>DeckRules&&DeckReady&&Phase<2&&RedDone&&BlueDone&&BothPlansVerified&&!CurrentTasks;
    public bool CurrentTasks=>messages.Exists(m=>m.Phase==Phase&&m.TaskActive);
    public int CurrentTaskCount=>messages.FindAll(m=>m.Phase==Phase&&m.TaskActive).Count;
    private bool AdvanceEncounter()
    {
        if(!CanAdvanceEncounter)return false;
        Phase++;RedDone=BlueDone=false;RedConsents=BlueConsents=false;SelectedCard=ConversationCard.None;beforeLastChoice=null;
        Capacity+=2;Load--;Normalize();
        if(Phase==1){Draw(ConversationCard.Listen);Draw(ConversationCard.Reframe);}
        if(Phase==2){Draw(ConversationCard.TakeOver);Draw(ConversationCard.Rewind);}
        Revision++;return true;
    }
    private void CommitDeckPacket(CorrespondenceEvent result)
    {
        if(result.Tool!=DispatchTool.None)caseDeck.Stock[(int)result.Tool-1]--;
        if(result.Combo!=DispatchCombo.None)caseDeck.Combos[(int)result.Combo-1]++;
    }
    private bool NeedsSourceCheck(MessageEnvelope m)=>DeckRules&&Phase==1&&!m.SourceChecked;
    private bool IsCurrentEnvelope(MessageEnvelope m)=>!DeckRules||m.Phase==Phase;
    private void DeckTaskHandoff(MessageEnvelope m)
    {m.TaskActive=false;m.TaskHandedOff=true;ManagedMessages=Math.Max(0,ManagedMessages-1);Load-=3;}
}
