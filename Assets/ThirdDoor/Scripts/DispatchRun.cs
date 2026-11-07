using System;

public enum DispatchFragment { None, Fact, Request, Need, Guess }
public enum DispatchTool { None, Quote, Pause, Listen, Reframe, Boundary, TakeOver, Check, Receipt, Handoff }

// A recipient hears an ordered packet, not an abstract good/bad answer.
public sealed partial class RelationshipRun
{
    public bool DispatchRules { get; private set; }
    public static RelationshipRun Legacy(CaseId id=CaseId.Home)=>new RelationshipRun(id,false);
    public bool PublicPlanVerified(int source)
    {var m=MessageFrom(source);return m!=null&&m.PublicProgress==3&&!m.Assumed;}
    public bool BothPlansVerified=>PublicPlanVerified(0)&&PublicPlanVerified(1);
    public bool HasAssumedPlan=>messages.Exists(m=>m.Assumed);
    public bool CanReconfirmPlan(int voice)
    {
        if(!DispatchRules||Ended||voice<0||voice>1||Phase!=2||Capacity==0||Trust(voice)<1||Clarity(voice)<2||HasUnrepairedDisclosure(voice)||!PublicPlanVerified(1-voice)||(voice==0?RedConsents:BlueConsents))return false;
        return history.Exists(r=>r.Phase==2&&r.Voice==voice&&IsConsentResponse(voice,r.Option));
    }
    public int ReceiptState(int recipient)
    {var m=MessageFrom(1-recipient);return m==null?0:m.Assumed?3:m.PublicProgress==3?2:m.PublicProgress!=0?1:0;}
    public DispatchFragment PriorityAt(int recipient)=>recipient==0?DispatchFragment.Fact:DispatchFragment.Request;
    public int HearingRoom(int recipient,MessageEnvelope item)
    {
        if(DeckRules&&Phase==2)return 1;
        if(DeckRules&&Phase==1)return 2;
        int room=Case==CaseId.Home?(recipient==0?(Phase==0?2:1):(Phase==1?2:1)):(recipient==0?(Phase==1?2:1):(Phase==0?2:1));
        return Math.Min(2,room+(item!=null&&(item.Inspected||item.PrivateHeard)?1:0));
    }
    public static ConversationCard CardFor(DispatchTool tool)
    {
        switch(tool){case DispatchTool.Quote:return ConversationCard.Quote;case DispatchTool.Pause:return ConversationCard.Pause;
            case DispatchTool.Listen:return ConversationCard.Listen;case DispatchTool.Reframe:return ConversationCard.Reframe;
            case DispatchTool.Boundary:return ConversationCard.Boundary;case DispatchTool.TakeOver:return ConversationCard.TakeOver;default:return ConversationCard.None;}
    }
    public bool CanUseDispatchTool(int source,DispatchTool tool)
    {
        if(DeckRules)return CanUseDeckTool(source,tool);
        if(source<0||source>1)return false;
        if(tool<DispatchTool.None||tool>DispatchTool.TakeOver)return false;
        if(tool==DispatchTool.None)return true;
        if(Stock(CardFor(tool))==0)return false;
        if((tool==DispatchTool.Quote||tool==DispatchTool.Reframe)&&Clarity(source)==0)return false;
        if(tool==DispatchTool.Boundary&&Clarity(1-source)==0)return false;
        return true;
    }
    public bool CanDraft(int id,int atVoice)
    {
        var m=Message(id);return DispatchRules&&(!DeckRules||DeckReady)&&!Ended&&m!=null&&IsCurrentEnvelope(m)&&atVoice==1-m.Source
            &&(m.Status==MessageStatus.Held||m.Assumed||(DeckRules&&m.TaskActive))&&!(m.PublicProgress==3&&!m.Assumed&&(!DeckRules||!m.TaskActive))
            &&m.Status!=MessageStatus.Returned&&m.Status!=MessageStatus.KeptPrivate;
    }
    public bool FragmentAvailable(MessageEnvelope m,DispatchFragment part)
    {
        if(m==null||part<DispatchFragment.Fact||part>DispatchFragment.Guess)return false;
        if(part==DispatchFragment.Fact)return (m.PublicProgress&1)==0;
        if(part==DispatchFragment.Request)return (m.PublicProgress&2)==0;
        if(part==DispatchFragment.Need)return m.HasNeed&&!m.PrivateHeard;
        return !m.GuessHeard;
    }
    public bool CanSendPacket(int id,DispatchFragment first,DispatchFragment second,DispatchTool tool,int atVoice)
    {
        if(!CanDraft(id,atVoice)||!CanUseDispatchTool(Message(id).Source,tool))return false;
        var m=Message(id);
        if(m.PublicProgress==3&&!m.Assumed)return DeckRules&&tool==DispatchTool.Handoff&&first==DispatchFragment.None&&second==DispatchFragment.None;
        if(m.DispatchAttempts>=4&&!(m.DispatchAttempts==4&&tool==DispatchTool.TakeOver))return false;
        if(Capacity==0&&tool!=DispatchTool.Pause&&!(DeckRules&&tool==DispatchTool.Receipt))return false;
        if(first==DispatchFragment.None)return second==DispatchFragment.None&&(tool==DispatchTool.Listen||(DeckRules&&(tool==DispatchTool.Check||tool==DispatchTool.Receipt)));
        bool addition=first==DispatchFragment.Guess||second==DispatchFragment.Guess;
        bool publicWords=first==DispatchFragment.Fact||first==DispatchFragment.Request||second==DispatchFragment.Fact||second==DispatchFragment.Request;
        // A fabricated guarantee is a separate bid. Accurate words cannot launder it into a verified plan.
        if(addition&&publicWords&&tool!=DispatchTool.Reframe)return false;
        return FragmentAvailable(m,first)&&(second==DispatchFragment.None||(second!=first&&FragmentAvailable(m,second)));
    }
    public CorrespondenceEvent PreviewPacket(int id,DispatchFragment first,DispatchFragment second,DispatchTool tool,int atVoice)
    {
        if(!CanSendPacket(id,first,second,tool,atVoice))return null;
        var snapshot=new Snapshot(this);var result=ResolvePacket(id,first,second,tool);snapshot.Restore(this);return result;
    }
    public CorrespondenceEvent SendPacket(int id,DispatchFragment first,DispatchFragment second,DispatchTool tool,int atVoice)
    {
        if(!CanSendPacket(id,first,second,tool,atVoice))return null;
        var result=ResolvePacket(id,first,second,tool);
        var card=CardFor(tool);if(DeckRules)CommitDeckPacket(result);else if(card!=ConversationCard.None)stock[(int)card]--;
        if(card!=ConversationCard.None&&SelectedCard==card)SelectedCard=ConversationCard.None;
        correspondence.Add(result);beforeLastChoice=null;Revision++;return result;
    }
    private CorrespondenceEvent ResolvePacket(int id,DispatchFragment first,DispatchFragment second,DispatchTool tool)
    {
        var m=Message(id);int source=m.Source,target=1-source;
        int breath=Capacity,load=Load,bridge=Bridge,sourceTrust=Trust(source),targetTrust=Trust(target),oldProgress=m.PublicProgress;
        bool wasAssumed=m.Assumed;
        var result=new CorrespondenceEvent{MessageId=id,Source=source,Phase=Phase,Action=MessageAction.Deliver,
            Payload=first==DispatchFragment.Need&&second==DispatchFragment.None?MessagePayload.Need:MessagePayload.Request,
            Packet=true,First=first,Second=second,Tool=tool,WasPromise=m.Promised,RecipientReady=Trust(target)>=2,
            PrefersOwnVoice=m.Sharing==SharingPreference.OwnVoice,PublicBefore=oldProgress};
        if(!DeckRules||tool!=DispatchTool.Handoff)m.DispatchAttempts++;
        Capacity--;
        int room=HearingRoom(target,m);
        bool bypass=tool==DispatchTool.Quote||tool==DispatchTool.TakeOver;
        if(tool==DispatchTool.Pause){Capacity+=2;room=2;}
        if(tool==DispatchTool.Quote){Load++;room=2;}
        if(tool==DispatchTool.Listen){m.Inspected=true;AddClarity(target,1);room=2;}
        if(tool==DispatchTool.Reframe){Load--;AddTrust(target,1);m.Inspected=true;}
        if(tool==DispatchTool.Boundary){Load-=2;Boundaries++;room=2;}
        if(tool==DispatchTool.TakeOver){Load+=3;ManagedMessages++;AddTrust(target,1);room=2;}
        if(DeckRules)
        {
            if(tool==DispatchTool.Check){m.SourceChecked=true;AddClarity(source,1);m.Inspected=true;}
            if(tool==DispatchTool.Receipt)
            {
                var other=MessageFrom(target);if(other!=null&&other.PublicProgress==3&&!other.Assumed)m.SourceChecked=true;
                Capacity++;room=2;result.Combo=DispatchCombo.ReceiptChain;
            }
            if(tool==DispatchTool.Quote&&m.SourceChecked){Load--;result.Combo=DispatchCombo.CheckedQuote;}
            if(tool==DispatchTool.Reframe&&m.LastTool==DispatchTool.Pause){Capacity++;room=2;result.Combo=DispatchCombo.PacedReframe;}
            if(tool==DispatchTool.TakeOver){Capacity--;m.TaskActive=true;m.SourceChecked=true;}
        }
        result.HearingRoom=room;
        if(DeckRules&&tool==DispatchTool.Handoff)
        {Capacity++;DeckTaskHandoff(m);result.Combo=DispatchCombo.Handoff;result.Completed=true;}
        else if(NeedsSourceCheck(m)&&(first==DispatchFragment.Fact||first==DispatchFragment.Request||second==DispatchFragment.Fact||second==DispatchFragment.Request))
        {result.CheckMissing=true;Load++;}
        else if(Trust(target)==0&&!bypass)
        {result.Guarded=true;Load++;m.Inspected=true;}
        else
        {
            var parts=new[]{first,second};
            // Asking first makes room for either starting point; it never silently rewrites the packet.
            for(int i=0;i<parts.Length;i++)
            {
                var part=parts[i];if(part==DispatchFragment.None)continue;
                if(result.HeardCount>=room){result.Interrupted=true;break;}
                result.HeardCount++;
                if(part==DispatchFragment.Fact||part==DispatchFragment.Request)
                {
                    int bit=part==DispatchFragment.Fact?1:2;
                    if(m.PublicProgress==0&&part!=PriorityAt(target)&&tool!=DispatchTool.Listen&&tool!=DispatchTool.TakeOver)
                    {result.PriorityMiss=true;m.Inspected=true;break;}
                    m.PublicProgress|=bit;
                }
                else if(part==DispatchFragment.Need)
                {
                    m.PrivateHeard=true;AddTrust(target,1);
                    result.PermissionGranted=m.Permission==MessagePermission.Granted;
                    if(result.PermissionGranted){Load--;Bridge++;}
                    else
                    {
                        Load++;AddTrust(source,-2);result.Breach=true;disclosures[source]=true;
                        consentBeforeDisclosure[source]=source==0?RedConsents:BlueConsents;
                        if(source==0)RedConsents=false;else BlueConsents=false;
                    }
                }
                else
                {
                    m.GuessHeard=true;
                    if(tool==DispatchTool.Reframe){m.Inspected=true;Bridge++;result.QuestionedGuess=true;}
                    else if(Trust(target)>=2||tool==DispatchTool.TakeOver)
                    {m.Assumed=true;Load+=2;AddTrust(source,-1);AddTrust(target,1);Bridge++;result.Provisional=true;}
                    else {result.Unbelieved=true;m.Inspected=true;}
                }
            }
            int gained=m.PublicProgress&~oldProgress;
            Bridge+=((gained&1)!=0?1:0)+((gained&2)!=0?1:0);
            if(m.PublicProgress==3)
            {
                m.Assumed=false;m.Status=MessageStatus.Delivered;m.Promised=false;
                Load-=oldProgress==0?1:2;AddTrust(source,1);AddClarity(source,1);AddClarity(target,1);
                result.Completed=true;result.CorrectedAssumption=wasAssumed;
            }
            else if(m.Assumed){m.Status=MessageStatus.Delivered;m.Promised=false;}
            else if(gained!=0)Load++;
            else if(result.PriorityMiss||result.Unbelieved)Load++;
        }
        if(DeckRules)m.LastTool=tool;
        Normalize();result.PublicAfter=m.PublicProgress;result.Provisional=m.Assumed;
        result.CapacityDelta=Capacity-breath;result.LoadDelta=Load-load;result.BridgeDelta=Bridge-bridge;
        result.SourceTrustDelta=Trust(source)-sourceTrust;result.RecipientTrustDelta=Trust(target)-targetTrust;
        return result;
    }
}
