using System;

public static class CaseDeckTests
{
    public static int Checks;
    private static void Check(bool ok,string label){Checks++;if(!ok)throw new Exception("CASE DECK FAIL / "+label);}
    private static void Save(RelationshipRun run)
    {
        RelationshipRun loaded;var data=run.ExportSave();
        Check(RelationshipRun.TryRestoreSave(data,out loaded),"version 3 accepts complete deck state");
        Check(DispatchTests.Fingerprint(data)==DispatchTests.Fingerprint(loaded.ExportSave()),"deck, flags, history and stocks roundtrip");
    }
    private static CorrespondenceEvent Send(RelationshipRun r,int source,DispatchFragment f,DispatchFragment s=DispatchFragment.None,DispatchTool t=DispatchTool.None)
    {
        var m=r.MessageFrom(source);string before=DispatchTests.Fingerprint(r.ExportSave());var pre=r.PreviewPacket(m.Id,f,s,t,1-source);
        Check(DispatchTests.Fingerprint(r.ExportSave())==before,"deck preview changes no field");
        var e=r.SendPacket(m.Id,f,s,t,1-source);Check(e!=null&&pre!=null,"authored deck action available: "+t+" round "+r.Phase);
        Check(DispatchTests.Fingerprint(e)==DispatchTests.Fingerprint(pre),"deck preview equals resolution");Save(r);return e;
    }
    private static void CheckSource(RelationshipRun r,int source)
    {var m=r.MessageFrom(source);if(!m.SourceChecked)Check(r.ActOnMessage(m.Id,MessageAction.CheckFacts,MessagePayload.Request,source)!=null,"source check is available without a card");}
    public static void Run()
    {
        var inlineLegacy=new RelationshipRun().ExportSave();inlineLegacy.Deck=new CaseDeckData();RelationshipRun inlineRestored;
        Check(RelationshipRun.TryRestoreSave(inlineLegacy,out inlineRestored)&&!inlineRestored.DeckRules,"Unity inline default deck object cannot opt a home save into deck mode");
        inlineLegacy.Version=2;
        Check(RelationshipRun.TryRestoreSave(inlineLegacy,out inlineRestored)&&!inlineRestored.DeckRules,"old saves keep their rules even when the serializer allocates the missing class");
        var missingDeck=RelationshipRun.DeckCase().ExportSave();missingDeck.Deck=null;
        Check(!RelationshipRun.TryRestoreSave(missingDeck,out inlineRestored),"explicit deck mode requires its deck data");
        for(int preset=0;preset<3;preset++)
        {
            var r=RelationshipRun.DeckCase();Save(r);
            Check(!r.CanTalk(0)&&!r.PrepareDeck(0),"choose exactly five before starting");
            int mask=RelationshipRun.DeckPreset(preset);Check(r.PrepareDeck(mask)&&r.DeckReady,"preset locks five cards");Save(r);
            Check(!r.PrepareDeck(mask)&&!r.CanEquip(ConversationCard.Pause,0),"cannot refill or use the old deck in a conversation");
            for(int phase=0;phase<3;phase++)
            {
                r.Rest();r.Respond(0,phase==2?1:0);r.Respond(1,phase==2?2:0);
                Check(r.Phase==phase&&r.RedDone&&r.BlueDone,"hearing both voices waits for actual delivery");Save(r);
                if(phase==1)
                {
                    var missed=Send(r,0,DispatchFragment.Request,DispatchFragment.Fact);
                    Check(missed.CheckMissing&&missed.PublicAfter==0,"new edition needs a real source check");
                }
                if(preset==0)
                {
                    if(!r.MessageFrom(0).SourceChecked)Send(r,0,DispatchFragment.None,DispatchFragment.None,DispatchTool.Check);
                    Check(Send(r,0,DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.Quote).Combo==DispatchCombo.CheckedQuote,"check powers quote");
                    Check(Send(r,1,DispatchFragment.Fact,DispatchFragment.Request,DispatchTool.Receipt).Combo==DispatchCombo.ReceiptChain,"actual opposite receipt powers return trip");
                }
                else if(preset==1)
                {
                    if(!r.MessageFrom(0).SourceChecked)Send(r,0,DispatchFragment.None,DispatchFragment.None,DispatchTool.Check);
                    Send(r,0,DispatchFragment.Request,DispatchFragment.None,DispatchTool.Pause);
                    Check(Send(r,0,DispatchFragment.Fact,DispatchFragment.None,DispatchTool.Reframe).Combo==DispatchCombo.PacedReframe,"pause powers reframe once");
                    if(phase==1)CheckSource(r,1);
                    Send(r,1,DispatchFragment.Fact,DispatchFragment.Request,DispatchTool.Listen);
                }
                else
                {
                    Send(r,0,DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.TakeOver);
                    Check(r.MessageFrom(0).TaskActive&&!r.CanAdvanceEncounter,"own job persists after hearing the words");
                    Check(Send(r,0,DispatchFragment.None,DispatchFragment.None,DispatchTool.Handoff).Combo==DispatchCombo.Handoff&&!r.MessageFrom(0).TaskActive,"handoff returns the actual job");
                    Send(r,1,DispatchFragment.Fact,DispatchFragment.Request,DispatchTool.Receipt);
                }
                Check(r.BothPlansVerified,"both current plans are verified");
                if(phase<2)
                {
                    Check(r.CanAdvanceEncounter&&r.Decide(1)==RelationshipEnding.None&&r.Phase==phase+1,"advance restores breath and starts a new encounter");
                    Check(!r.DeckReady&&!r.CanTalk(0),"stage break offers one swap before refilling");Save(r);
                    Check(r.PrepareDeck(mask),"keeping a loadout is allowed");
                }
                else
                {
                    for(int voice=0;voice<2;voice++)if(r.CanReconfirmPlan(voice))Check(r.ActOnMessage(r.MessageFrom(voice).Id,MessageAction.Reconfirm,MessagePayload.Request,voice)!=null,"ask each person again after late delivery");
                    Check(r.Decide(1)==RelationshipEnding.SharedTable,"each build reaches a concrete agreement");Save(r);
                }
            }
            Check(r.Messages.Count==6&&r.History.Count==6,"all three authored encounters remain in history");
        }
        var swap=RelationshipRun.DeckCase();int initial=RelationshipRun.DeckPreset(0);swap.PrepareDeck(initial);swap.Rest();swap.Respond(0,1);swap.Respond(1,2);
        Send(swap,0,DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.Pause);Send(swap,1,DispatchFragment.Fact,DispatchFragment.Request,DispatchTool.Quote);swap.Decide(1);
        int remove=1<<((int)DispatchTool.Quote-1),add=1<<((int)DispatchTool.Reframe-1);Check(swap.PrepareDeck((initial&~remove)|add),"a single replacement is accepted");Save(swap);
        var fifth=RelationshipRun.DeckCase();fifth.PrepareDeck(RelationshipRun.DeckPreset(2));fifth.Respond(0,0);fifth.Respond(1,0);
        var fifthData=fifth.ExportSave();fifthData.State.Capacity=8;fifthData.State.BlueTrust=0;RelationshipRun fifthRestored;
        Check(RelationshipRun.TryRestoreSave(fifthData,out fifthRestored),"fifth-attempt fixture is valid");fifth=fifthRestored;
        for(int i=0;i<4;i++)Send(fifth,0,DispatchFragment.Request);
        Send(fifth,0,DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.TakeOver);
        Check(fifth.MessageFrom(0).DispatchAttempts==5,"taking a task permits the fifth hearing");
        Send(fifth,0,DispatchFragment.None,DispatchFragment.None,DispatchTool.Handoff);
        Check(fifth.MessageFrom(0).DispatchAttempts==5&&!fifth.MessageFrom(0).TaskActive,"handoff remains possible after the last hearing without corrupting the attempt limit");Save(fifth);
        var basic=RelationshipRun.DeckCase();basic.PrepareDeck(RelationshipRun.DeckPreset(0));
        for(int phase=0;phase<3;phase++)
        {
            basic.Rest();
            if(phase<2){basic.Respond(0,phase==0?0:1);basic.Respond(1,phase==0?0:2);}
            else {basic.Respond(1,2);}
            if(phase==2)
            {
                Send(basic,1,DispatchFragment.Fact,DispatchFragment.Request);Send(basic,1,DispatchFragment.Request);basic.Respond(0,1);
            }
            Send(basic,0,DispatchFragment.Request,DispatchFragment.Fact);
            if(!basic.PublicPlanVerified(0))Send(basic,0,DispatchFragment.Fact);
            if(phase<2){Send(basic,1,DispatchFragment.Fact,DispatchFragment.Request);if(!basic.PublicPlanVerified(1))Send(basic,1,DispatchFragment.Request);}
            if(phase<2){Check(basic.CanAdvanceEncounter,"basic actions can close a scene");basic.Decide(1);basic.PrepareDeck(basic.DeckMask);}
            else {if(basic.CanReconfirmPlan(1))basic.ActOnMessage(basic.MessageFrom(1).Id,MessageAction.Reconfirm,MessagePayload.Request,1);Check(basic.Decide(1)==RelationshipEnding.SharedTable,"timed basic route needs no strategy card");}
            Save(basic);
        }
        for(int i=1;i<=9;i++)if(basic.InDeck((DispatchTool)i))Check(basic.DeckStock((DispatchTool)i)==1,"basic route spends no card");
        var random=new Random(9116);
        for(int test=0;test<250;test++)
        {
            var r=RelationshipRun.DeckCase();r.PrepareDeck(RelationshipRun.DeckPreset(test%3));r.Rest();
            for(int step=0;step<24&&!r.Ended;step++)
            {
                if(random.Next(4)==0)r.Rest();
                if(r.CanTalk(0)||r.CanTalk(1)){int v=r.CanTalk(0)?0:1;r.Respond(v,random.Next(3));}
                else if(r.CanAdvanceEncounter){r.Decide(1);r.PrepareDeck(r.DeckMask);}
                else if(r.Messages.Count>0)
                {
                    int source=random.Next(2);var m=r.MessageFrom(source);if(m!=null)
                    {
                        var first=(DispatchFragment)random.Next(5);var second=(DispatchFragment)random.Next(5);var tool=(DispatchTool)random.Next(10);
                        string before=DispatchTests.Fingerprint(r.ExportSave());var pre=r.PreviewPacket(m.Id,first,second,tool,1-source);
                        Check(DispatchTests.Fingerprint(r.ExportSave())==before,"random preview is read only");
                        var e=r.SendPacket(m.Id,first,second,tool,1-source);Check((e!=null)==(pre!=null),"random legality agrees");
                    }
                }
                Save(r);
            }
            r.Decide(2);Check(r.Ended&&RelationshipRun.OpensExit(r.Ending),"any deck can leave");Save(r);
        }
        Console.WriteLine("CASE DECK V9 / PASS / checks="+Checks);
    }
}
