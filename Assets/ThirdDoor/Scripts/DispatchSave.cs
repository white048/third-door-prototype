public sealed partial class RelationshipRun
{
    private static bool ValidPacketEvent(CorrespondenceEventSaveData e,bool rules)
    {
        if(!e.Packet)return e.First==0&&e.Second==0&&e.Tool==0&&e.PublicBefore==0&&e.PublicAfter==0&&e.HearingRoom==0&&e.HeardCount==0
            &&!e.Completed&&!e.Provisional&&!e.Interrupted&&!e.PriorityMiss&&!e.Guarded&&!e.Unbelieved&&!e.QuestionedGuess&&!e.CorrectedAssumption;
        if(!rules||e.Action!=(int)MessageAction.Deliver||!InRange(e.First,4)||!InRange(e.Second,4)||!InRange(e.Tool,6)
            ||!InRange(e.PublicBefore,3)||!InRange(e.PublicAfter,3)||!InRange(e.HearingRoom,2)||!InRange(e.HeardCount,2))return false;
        if(e.First==0&&(e.Second!=0||e.Tool!=(int)DispatchTool.Listen))return false;
        if(e.First!=0&&e.First==e.Second)return false;
        if((e.PublicBefore&~e.PublicAfter)!=0||e.Completed!=(e.PublicAfter==3)||e.Completed&&e.Provisional)return false;
        if(e.CorrectedAssumption&&!e.Completed)return false;
        return true;
    }
}
