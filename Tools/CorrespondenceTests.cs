using System;

public static class CorrespondenceTests
{
    public static string Fingerprint(RelationshipRun r)
    {
        string text="/"+r.Messages.Count+"/"+r.Correspondence.Count+"/"+r.HasUnrepairedDisclosure(0)+"/"+r.HasUnrepairedDisclosure(1);
        foreach(var item in r.Messages)text+="/"+item.Id+","+item.Source+","+item.Phase+","+item.HasNeed+","+item.Promised+","+item.Permission+","+item.PermissionPhase+","+item.Status+","+item.Sharing;
        return text+"/"+r.Capacity+","+r.Load+","+r.Bridge+","+r.RedTrust+","+r.BlueTrust+","+r.Boundaries+","+r.RedConsents+","+r.BlueConsents+","+r.Revision+","+r.CanRewind;
    }
    private static RelationshipRun Ready()
    {
        var r=RelationshipRun.Legacy();for(int i=0;i<6;i++){if(i%2==0)r.Rest();r.Respond(i%2,1);}return r;
    }
    public static void Run(Action<bool,string> check)
    {
        var allowed=RelationshipRun.Legacy();allowed.Respond(0,1);var envelope=allowed.MessageFrom(0);
        check(envelope!=null&&envelope.HasNeed&&!envelope.Promised&&allowed.HeldMessages==1,"listening creates a private envelope, not a promise");
        string before=Fingerprint(allowed);var permission=allowed.PreviewMessage(envelope.Id,MessageAction.AskPermission,MessagePayload.Need,0);
        check(permission.PermissionGranted&&permission.CapacityDelta==-1&&Fingerprint(allowed)==before&&envelope.Permission==MessagePermission.Unasked,"permission preview changes neither state nor an existing envelope reference");
        check(!allowed.CanActOnMessage(envelope.Id,MessageAction.AskPermission,MessagePayload.Need,1),"permission must be asked at the source");
        allowed.ActOnMessage(envelope.Id,MessageAction.AskPermission,MessagePayload.Need,0);
        check(envelope.Permission==MessagePermission.Granted&&!allowed.CanRewind,"asking permission closes the old revision opportunity");
        check(!allowed.CanActOnMessage(envelope.Id,MessageAction.Deliver,MessagePayload.Need,0),"delivery must occur at the recipient");
        var delivered=allowed.ActOnMessage(envelope.Id,MessageAction.Deliver,MessagePayload.Need,1);
        check(delivered!=null&&!delivered.Breach&&delivered.RecipientTrustDelta==1&&delivered.SourceTrustDelta==0&&envelope.Status==MessageStatus.Delivered,"allowed need reaches its listener without disclosing against its owner");
        check(allowed.ActOnMessage(envelope.Id,MessageAction.Deliver,MessagePayload.Need,1)==null,"an envelope cannot be delivered twice");
        var ownVoice=RelationshipRun.Legacy();ownVoice.Respond(1,1);
        var no=ownVoice.ActOnMessage(0,MessageAction.AskPermission,MessagePayload.Need,1);
        check(ownVoice.BlueTrust>=2&&!no.PermissionGranted&&no.PrefersOwnVoice,"a trusted person can still prefer to speak privately for themselves");
        ownVoice.Respond(0,1);
        check(!ownVoice.CanActOnMessage(0,MessageAction.AskPermission,MessagePayload.Need,1),"a clear request for one's own voice cannot be overcome by repeatedly asking");

        var guarded=RelationshipRun.Legacy();guarded.Respond(0,0);
        check(!guarded.MessageFrom(0).HasNeed&&!guarded.CanActOnMessage(0,MessageAction.Deliver,MessagePayload.Need,1),"a request does not reveal its private need");
        var harsh=guarded.ActOnMessage(0,MessageAction.Deliver,MessagePayload.Request,1);
        var ready=RelationshipRun.Legacy();ready.Respond(0,0);ready.Respond(1,1);
        var heard=ready.ActOnMessage(0,MessageAction.Deliver,MessagePayload.Request,1);
        check(harsh.RecipientTrustDelta==-1&&harsh.BridgeDelta==0&&heard.RecipientTrustDelta==0&&heard.BridgeDelta==1,"the same original words depend on recipient readiness");

        var privateWords=RelationshipRun.Legacy();privateWords.Respond(0,1);
        var breach=privateWords.ActOnMessage(0,MessageAction.Deliver,MessagePayload.Need,1);
        check(breach.Breach&&privateWords.RedTrust==0&&privateWords.BlueTrust==2&&privateWords.HasUnrepairedDisclosure(0),"a useful disclosure can gain one person's trust and breach the other's");
        var apology=privateWords.ActOnMessage(0,MessageAction.Repair,MessagePayload.Need,0);
        check(apology!=null&&!privateWords.HasUnrepairedDisclosure(0)&&privateWords.RedTrust==1&&!privateWords.RedConsents,"acknowledging a breach repairs trust without inventing consent");

        var late=Ready();check(late.RedConsents&&late.BlueConsents,"both voices agreed before late disclosure");
        late.ActOnMessage(0,MessageAction.Deliver,MessagePayload.Need,1);
        check(!late.RedConsents&&late.BlueConsents,"disclosure revokes only the owner's prior consent");
        var renewal=late.ActOnMessage(0,MessageAction.Repair,MessagePayload.Need,0);
        check(renewal.ConsentRenewed&&late.RedConsents&&late.Decide(1)==RelationshipEnding.SharedTable,"a late apology asks for and can receive a fresh personal yes");

        var returned=RelationshipRun.Legacy();returned.Respond(0,0);int limits=returned.Boundaries;
        check(!returned.CanActOnMessage(0,MessageAction.Return,MessagePayload.Request,1),"a promise is returned to its owner");
        returned.ActOnMessage(0,MessageAction.Return,MessagePayload.Request,0);
        check(returned.Boundaries==limits+1&&returned.Message(0).Status==MessageStatus.Returned,"returning a promise establishes a real limit");
        check(returned.ActOnMessage(0,MessageAction.Return,MessagePayload.Request,0)==null,"returning cannot farm breath or boundaries");
        var kept=RelationshipRun.Legacy();kept.Respond(0,1);int capacity=kept.Capacity;
        kept.ActOnMessage(0,MessageAction.Return,MessagePayload.Need,-1);
        check(kept.Capacity==capacity&&kept.Boundaries==0&&kept.Message(0).Status==MessageStatus.KeptPrivate,"keeping a private note needs no trip and grants no farmable resources");
        var undone=RelationshipRun.Legacy();undone.Respond(0,2);undone.Respond(1,2);undone.Respond(0,2);undone.Respond(1,1);
        check(undone.Messages.Count==1&&undone.CanRewind&&undone.Rewind()&&undone.Messages.Count==0,"rewind removes a note first heard in the withdrawn response");

        var random=new Random(907);
        for(int journey=0;journey<900;journey++)
        {
            var r=RelationshipRun.Legacy();
            for(int turn=0;turn<6;turn++)
            {
                if(random.Next(3)==0)r.Rest();
                r.Respond(r.RedDone?1:r.BlueDone?0:random.Next(2),random.Next(3));
                for(int actionIndex=0;actionIndex<3;actionIndex++)
                {
                    if(r.Messages.Count==0)continue;
                    var item=r.Message(random.Next(r.Messages.Count));var action=(MessageAction)random.Next(4);var payload=(MessagePayload)random.Next(2);int context=random.Next(-1,2);
                    string snapshot=Fingerprint(r);var status=item.Status;var preview=r.PreviewMessage(item.Id,action,payload,context);
                    check(Fingerprint(r)==snapshot&&item.Status==status,"correspondence preview is side-effect free including live references");
                    if(preview==null)continue;
                    int breath=r.Capacity,load=r.Load,bridge=r.Bridge,sourceTrust=r.Trust(item.Source),recipientTrust=r.Trust(1-item.Source);
                    var actual=r.ActOnMessage(item.Id,action,payload,context);
                    check(actual!=null&&actual.CapacityDelta==preview.CapacityDelta&&actual.LoadDelta==preview.LoadDelta&&actual.BridgeDelta==preview.BridgeDelta&&actual.SourceTrustDelta==preview.SourceTrustDelta&&actual.RecipientTrustDelta==preview.RecipientTrustDelta,"every shown dispatch consequence matches its resolution");
                    check(r.Capacity-breath==actual.CapacityDelta&&r.Load-load==actual.LoadDelta&&r.Bridge-bridge==actual.BridgeDelta&&r.Trust(item.Source)-sourceTrust==actual.SourceTrustDelta&&r.Trust(1-item.Source)-recipientTrust==actual.RecipientTrustDelta,"dispatch receipt records actual clamped changes");
                    check(!r.CanRewind&&r.HeldMessages<=2&&r.Messages.Count<=2&&r.Capacity>=0&&r.Capacity<=8&&r.Load>=0&&r.Load<=8&&r.Bridge>=0&&r.Bridge<=6,"real exchanges remain bounded and cannot be erased by revision");
                }
            }
            check(RelationshipRun.OpensExit(r.Decide(2)),"a disclosure never removes the personal right to leave");
            check(!r.CanActOnMessage(0,MessageAction.Deliver,MessagePayload.Request,1),"endings lock the correspondence ledger");
        }
    }
}
