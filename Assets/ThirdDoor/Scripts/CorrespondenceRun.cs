using System;
using System.Collections.Generic;

public enum MessagePayload { Request, Need }
public enum MessageAction { Deliver, AskPermission, Return, Repair, Reconfirm, CheckFacts, HandOffTask }
public enum MessageStatus { Held, Delivered, Returned, KeptPrivate }
public enum MessagePermission { Unasked, Granted, Declined }
public enum SharingPreference { AskFirst, OwnVoice }

public sealed class MessageEnvelope
{
    public int Id, Source, Phase, SourceTrust, PriorManaged, PriorAlliances;
    public bool HasNeed, Promised;
    public int PermissionPhase = -1;
    public MessagePermission Permission;
    public MessageStatus Status;
    public SharingPreference Sharing;
    public int PublicProgress,DispatchAttempts;
    public bool Inspected,PrivateHeard,GuessHeard,Assumed;
    public bool SourceChecked,TaskActive,TaskHandedOff;
    public DispatchTool LastTool;
    internal MessageEnvelope Copy() => (MessageEnvelope)MemberwiseClone();
    internal void Restore(MessageEnvelope copy)
    {
        Id=copy.Id;Source=copy.Source;Phase=copy.Phase;SourceTrust=copy.SourceTrust;PriorManaged=copy.PriorManaged;PriorAlliances=copy.PriorAlliances;
        HasNeed=copy.HasNeed;Promised=copy.Promised;PermissionPhase=copy.PermissionPhase;Permission=copy.Permission;Status=copy.Status;Sharing=copy.Sharing;
        PublicProgress=copy.PublicProgress;DispatchAttempts=copy.DispatchAttempts;Inspected=copy.Inspected;PrivateHeard=copy.PrivateHeard;GuessHeard=copy.GuessHeard;Assumed=copy.Assumed;
        SourceChecked=copy.SourceChecked;TaskActive=copy.TaskActive;TaskHandedOff=copy.TaskHandedOff;LastTool=copy.LastTool;
    }
}

public sealed class CorrespondenceEvent
{
    public int MessageId, Source, Phase;
    public MessageAction Action;
    public MessagePayload Payload;
    public bool Breach, PermissionGranted, RecipientReady, WasPromise, ConsentRenewed, PrefersOwnVoice;
    public int CapacityDelta, LoadDelta, BridgeDelta, SourceTrustDelta, RecipientTrustDelta;
    public bool Packet,Completed,Provisional,Interrupted,PriorityMiss,Guarded,Unbelieved,QuestionedGuess,CorrectedAssumption;
    public DispatchFragment First,Second;
    public DispatchTool Tool;
    public int PublicBefore,PublicAfter,HearingRoom,HeardCount;
    public DispatchCombo Combo;
    public bool CheckMissing;
}

// Two authored voices, two envelopes. The content stays attached to its actual source.
public sealed partial class RelationshipRun
{
    private readonly List<MessageEnvelope> messages = new List<MessageEnvelope>();
    private readonly List<CorrespondenceEvent> correspondence = new List<CorrespondenceEvent>();
    private readonly bool[] disclosures = new bool[2], consentBeforeDisclosure = new bool[2];
    public IReadOnlyList<MessageEnvelope> Messages => messages;
    public IReadOnlyList<CorrespondenceEvent> Correspondence => correspondence;
    public bool HasUnrepairedDisclosure(int voice) => voice >= 0 && voice < 2 && disclosures[voice];
    public int HeldMessages { get { int count=0;foreach(var item in messages)if(IsCurrentEnvelope(item)&&item.Status==MessageStatus.Held)count++;return count; } }
    public MessageEnvelope Message(int id) => messages.Find(item=>item.Id==id);
    public MessageEnvelope MessageFrom(int source) => messages.Find(item=>item.Source==source&&IsCurrentEnvelope(item));

    private void ObserveMessage(ConversationRecord record)
    {
        if(record.TooExhaustedToListen)return;
        var item=MessageFrom(record.Voice);
        bool workScope=Case==CaseId.Office&&record.Voice==1&&record.Option==2;
        if(item==null&&(record.Option<2||record.Card==ConversationCard.Listen||workScope||DeckRules))
        {
            item=new MessageEnvelope{Id=messages.Count,Source=record.Voice,Phase=record.Phase,SourceTrust=record.VoiceTrustBefore,PriorManaged=record.PriorManagedMessages,PriorAlliances=record.PriorAlliances,Sharing=record.Voice==1?SharingPreference.OwnVoice:SharingPreference.AskFirst};
            messages.Add(item);
            record.CreatedMessageId=item.Id;
        }
        if(item==null)return;
        if(DeckRules)
        {
            item.HasNeed=false;
            if((record.Voice==0&&record.Option==1)||(record.Voice==1&&record.Option==2))item.SourceChecked=true;
        }
        if(DispatchRules&&!DeckRules)item.HasNeed=item.HasNeed||(record.Phase<2&&record.Option==1&&!workScope&&!(Case==CaseId.Office&&record.Voice==1))||record.Card==ConversationCard.Listen;
        if(item.Status!=MessageStatus.Held)return;
        if(!DispatchRules)item.HasNeed=Clarity(record.Voice)>0;
        if(record.Option==0||workScope)item.Promised=true;
        // Asking them to speak for themselves also returns the promise to carry it for them.
        if(record.Phase==2&&IsConsentResponse(record.Voice,record.Option))item.Promised=false;
    }

    public bool CanActOnMessage(int id,MessageAction action,MessagePayload payload,int atVoice)
    {
        if(action<MessageAction.Deliver||action>MessageAction.HandOffTask||payload<MessagePayload.Request||payload>MessagePayload.Need)return false;
        var item=Message(id);if(Ended||item==null)return false;
        if(!IsCurrentEnvelope(item))return false;
        if(action==MessageAction.CheckFacts)return DeckReady&&atVoice==item.Source&&!item.SourceChecked&&Capacity>0;
        if(action==MessageAction.HandOffTask)return DeckReady&&atVoice==1-item.Source&&item.TaskActive&&item.PublicProgress==3&&!item.Assumed&&Capacity>0;
        if(action==MessageAction.Reconfirm)return atVoice==item.Source&&CanReconfirmPlan(item.Source);
        if(DispatchRules&&action==MessageAction.Deliver)return false;
        if(action==MessageAction.Repair)return atVoice==item.Source&&disclosures[item.Source]&&Capacity>0;
        if(item.Status!=MessageStatus.Held)return false;
        if(action==MessageAction.AskPermission)return atVoice==item.Source&&item.HasNeed&&item.Permission!=MessagePermission.Granted&&item.PermissionPhase<Phase&&Capacity>0&&(item.Sharing!=SharingPreference.OwnVoice||item.Permission==MessagePermission.Unasked);
        if(action==MessageAction.Return)return !item.Promised||atVoice==item.Source;
        return atVoice==1-item.Source&&(payload==MessagePayload.Request||(item.HasNeed&&Capacity>0));
    }

    public CorrespondenceEvent PreviewMessage(int id,MessageAction action,MessagePayload payload,int atVoice)
    {
        if(!CanActOnMessage(id,action,payload,atVoice))return null;
        var snapshot=new Snapshot(this);
        var result=ResolveMessage(id,action,payload);
        snapshot.Restore(this);return result;
    }

    public CorrespondenceEvent ActOnMessage(int id,MessageAction action,MessagePayload payload,int atVoice)
    {
        if(!CanActOnMessage(id,action,payload,atVoice))return null;
        var result=ResolveMessage(id,action,payload);
        correspondence.Add(result);
        // They have heard it. Rewinding a menu choice cannot erase that exchange.
        beforeLastChoice=null;Revision++;
        return result;
    }

    private CorrespondenceEvent ResolveMessage(int id,MessageAction action,MessagePayload payload)
    {
        var item=Message(id);int source=item.Source,recipient=1-source;
        int breath=Capacity,load=Load,bridge=Bridge,sourceTrust=Trust(source),recipientTrust=Trust(recipient);
        var result=new CorrespondenceEvent{MessageId=id,Source=source,Phase=Phase,Action=action,Payload=payload,WasPromise=item.Promised,RecipientReady=Trust(recipient)>=2,PrefersOwnVoice=item.Sharing==SharingPreference.OwnVoice};
        if(action==MessageAction.CheckFacts){Capacity--;item.SourceChecked=true;AddClarity(source,1);AddTrust(source,1);}
        else if(action==MessageAction.HandOffTask){Capacity--;DeckTaskHandoff(item);}
        else if(action==MessageAction.Reconfirm)
        {
            Capacity--;Load--;Bridge++;if(source==0)RedConsents=true;else BlueConsents=true;result.ConsentRenewed=true;
        }
        else if(action==MessageAction.AskPermission)
        {
            Capacity--;item.PermissionPhase=Phase;
            result.PermissionGranted=Trust(source)>=2&&item.Sharing!=SharingPreference.OwnVoice;
            item.Permission=result.PermissionGranted?MessagePermission.Granted:MessagePermission.Declined;
        }
        else if(action==MessageAction.Return)
        {
            if(item.Promised){Capacity++;Load--;Boundaries++;AddTrust(source,-1);item.Status=MessageStatus.Returned;}
            else item.Status=MessageStatus.KeptPrivate;
            item.Promised=false;
        }
        else if(action==MessageAction.Repair)
        {
            Capacity--;RepairDisclosure(source);
            result.ConsentRenewed=consentBeforeDisclosure[source]&&Trust(source)>=1&&Clarity(source)>=2;
            if(result.ConsentRenewed){if(source==0)RedConsents=true;else BlueConsents=true;}
            consentBeforeDisclosure[source]=false;
        }
        else
        {
            result.PermissionGranted=payload==MessagePayload.Request||item.Permission==MessagePermission.Granted;
            if(payload==MessagePayload.Request)
            {
                Load--;if(item.Promised)AddTrust(source,1);
                if(result.RecipientReady)Bridge++;else{AddTrust(recipient,-1);Load++;}
            }
            else
            {
                Capacity--;AddTrust(recipient,1);
                if(result.PermissionGranted){Load--;Bridge+=result.RecipientReady?2:1;}
                else
                {
                    Load++;AddTrust(source,-2);result.Breach=true;disclosures[source]=true;
                    consentBeforeDisclosure[source]=source==0?RedConsents:BlueConsents;
                    if(source==0)RedConsents=false;else BlueConsents=false;
                }
            }
            item.Status=MessageStatus.Delivered;item.Promised=false;
        }
        Normalize();
        result.CapacityDelta=Capacity-breath;result.LoadDelta=Load-load;result.BridgeDelta=Bridge-bridge;
        result.SourceTrustDelta=Trust(source)-sourceTrust;result.RecipientTrustDelta=Trust(recipient)-recipientTrust;
        return result;
    }

    private void RepairDisclosure(int source)
    {
        if(!disclosures[source])return;
        disclosures[source]=false;AddTrust(source,1);
    }

    private sealed partial class CorrespondenceSnapshot
    {
        private readonly MessageEnvelope[] items;
        private readonly int eventCount;
        private readonly bool red,blue,redConsent,blueConsent;
        public CorrespondenceSnapshot(RelationshipRun run)
        {
            items=new MessageEnvelope[run.messages.Count];for(int i=0;i<items.Length;i++)items[i]=run.messages[i].Copy();
            eventCount=run.correspondence.Count;red=run.disclosures[0];blue=run.disclosures[1];redConsent=run.consentBeforeDisclosure[0];blueConsent=run.consentBeforeDisclosure[1];
        }
        public void Restore(RelationshipRun run)
        {
            for(int i=0;i<items.Length;i++)
            {
                if(i<run.messages.Count)run.messages[i].Restore(items[i]);else run.messages.Add(items[i].Copy());
            }
            if(run.messages.Count>items.Length)run.messages.RemoveRange(items.Length,run.messages.Count-items.Length);
            if(run.correspondence.Count>eventCount)run.correspondence.RemoveRange(eventCount,run.correspondence.Count-eventCount);
            run.disclosures[0]=red;run.disclosures[1]=blue;run.consentBeforeDisclosure[0]=redConsent;run.consentBeforeDisclosure[1]=blueConsent;
        }
    }
}
