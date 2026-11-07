public sealed partial class RelationshipRun
{
    private static bool ValidDeckSave(RelationshipSaveData data)
    {
        if(!data.CaseDeckRules)return true;
        var d=data.Deck;if(d==null)return false;
        if(data.Version<3||!InRange(d.Edition,1)||data.Case!=(int)CaseId.Office||!data.DispatchRules||data.HasUndo||data.SelectedCard!=-1||d.Mask<0||d.Mask>=512
            ||d.Stock==null||d.Stock.Length!=9||d.Loadouts==null||d.Loadouts.Length!=3||d.Combos==null||d.Combos.Length!=4)return false;
        int phase=data.State.Phase;
        if(d.Mask==0)
        {
            if(phase!=0||data.History.Length!=0||data.Messages.Length!=0||d.PreparedPhase!=-1)return false;
        }
        else if(BitCount(d.Mask)!=5||d.PreparedPhase<0||d.PreparedPhase>phase||d.PreparedPhase<phase-1)return false;
        for(int i=0;i<9;i++)if(!InRange(d.Stock[i],1)||((d.Mask&(1<<i))==0&&d.Stock[i]!=0))return false;
        for(int i=0;i<3;i++)
        {
            int mask=d.Loadouts[i];if(mask<0||mask>=512)return false;
            if(i<=d.PreparedPhase){if(BitCount(mask)!=5)return false;if(i>0&&BitCount(mask^d.Loadouts[i-1])>2)return false;}
            else if(mask!=0)return false;
        }
        if(d.PreparedPhase>=0&&d.Mask!=d.Loadouts[d.PreparedPhase])return false;
        foreach(int count in d.Combos)if(!InRange(count,3))return false;
        return true;
    }
    private static bool ValidDeckMessage(MessageSaveData m,bool deck)
    {
        if(!deck)return !m.SourceChecked&&!m.TaskActive&&!m.TaskHandedOff&&m.LastTool==0;
        return InRange(m.LastTool,9)&&!(m.TaskActive&&m.TaskHandedOff)&&(!m.TaskHandedOff||(m.PublicProgress==3&&!m.Assumed));
    }
    private static bool ValidDeckEvent(CorrespondenceEventSaveData e,bool deck)
    {
        if(!deck)return e.Combo==0&&!e.CheckMissing;
        if(!InRange(e.Combo,4))return false;
        if(!e.Packet&&(e.Combo!=0||e.CheckMissing))return false;
        if(e.Combo==(int)DispatchCombo.CheckedQuote&&e.Tool!=(int)DispatchTool.Quote)return false;
        if(e.Combo==(int)DispatchCombo.PacedReframe&&e.Tool!=(int)DispatchTool.Reframe)return false;
        if(e.Combo==(int)DispatchCombo.ReceiptChain&&e.Tool!=(int)DispatchTool.Receipt)return false;
        if(e.Combo==(int)DispatchCombo.Handoff&&e.Tool!=(int)DispatchTool.Handoff)return false;
        if(e.CheckMissing&&(e.PublicBefore!=e.PublicAfter||e.HeardCount!=0))return false;
        return true;
    }
}
