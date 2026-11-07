using System;
using System.IO;
using System.Collections.Generic;

public static class ModelTests
{
    private static int checks;
    private static void Check(bool value, string name)
    { checks++; if (!value) throw new Exception("FAIL / " + name); }
    private static string Fingerprint(RelationshipRun r)
    {
        string value = r.Capacity+","+r.Load+","+r.Bridge+","+r.Phase+","+r.RedClarity+","+r.BlueClarity+","+r.RedTrust+","+r.BlueTrust+","+r.Boundaries+","+r.ManagedMessages+","+r.Alliances+","+r.RedDone+","+r.BlueDone+","+r.RedConsents+","+r.BlueConsents+","+r.History.Count+","+r.Revision;
        for (int i=0;i<7;i++) value += ","+r.Stock((ConversationCard)i);
        value+=CorrespondenceTests.Fingerprint(r);
        return value;
    }
    private static void Bounds(RelationshipRun r)
    {
        Check(r.Capacity>=0&&r.Capacity<=8&&r.Load>=0&&r.Load<=8&&r.Bridge>=0&&r.Bridge<=6,"bounded resources");
        Check(r.RedClarity>=0&&r.RedClarity<=3&&r.BlueClarity>=0&&r.BlueClarity<=3,"bounded insight");
        for(int i=0;i<7;i++) Check(r.Stock((ConversationCard)i)>=0&&r.Stock((ConversationCard)i)<=1,"no card duplication");
    }
    public static void Main(string[] args)
    {
        var random=new Random(73);
        for(int test=0;test<3000;test++)
        {
            var r=RelationshipRun.Legacy();
            Check(r.Respond(-1,0)==null&&r.Respond(0,3)==null,"invalid choices rejected");
            Check(r.Decide(0)==RelationshipEnding.None&&!r.Ended,"early return continues play");
            for(int step=0;step<6;step++)
            {
                int voice=r.RedDone?1:r.BlueDone?0:random.Next(2);
                if(random.Next(3)==0) r.Rest();
                var card=(ConversationCard)random.Next(-1,6);
                r.SelectCard(card,voice);
                int option=random.Next(3);
                string before=Fingerprint(r);
                int[] preview=r.Preview(voice,option);
                Check(Fingerprint(r)==before,"preview has no side effects");
                var record=r.Respond(voice,option);
                Check(record!=null,"valid response resolves");
                Check(record.VoiceConsentsAfter==(voice==0?r.RedConsents:r.BlueConsents),"history preserves the actual consent response");
                Check(preview[0]==record.CapacityAfter-record.CapacityBefore&&preview[1]==record.LoadAfter-record.LoadBefore&&preview[2]==record.BridgeAfter-record.BridgeBefore,"preview matches actual resource cost");
                Bounds(r);
                if(step%2==0) Check(r.Respond(voice,option)==null,"same voice cannot answer twice in a round");
            }
            r.Decide(random.Next(3));
            Check(r.Ended,"full journey resolves");
            string ended=Fingerprint(r);
            RelationshipEnding outcome=r.Ending;
            r.Respond(0,1);r.Rest();r.Rewind();r.SelectCard(ConversationCard.Pause,0);r.Decide(2);
            Check(Fingerprint(r)==ended&&r.Ending==outcome,"endings are immutable");
        }

        var undo=RelationshipRun.Legacy();
        undo.Respond(0,1);undo.Respond(1,1);undo.Rest();
        undo.SelectCard(ConversationCard.Reframe,0);undo.Respond(0,1);
        int breath=undo.Capacity,load=undo.Load,bridge=undo.Bridge;
        undo.SelectCard(ConversationCard.Quote,1);undo.Respond(1,2);
        Check(undo.Phase==2&&undo.CanRewind,"rewind is drawn in round three");
        Check(undo.Rewind(),"rewind resolves");
        Check(undo.Phase==1&&undo.RedDone&&!undo.BlueDone&&undo.Capacity==breath&&undo.Load==load&&undo.Bridge==bridge,"rewind restores the prior choice state");
        Check(undo.Stock(ConversationCard.Quote)==0&&undo.Stock(ConversationCard.Reframe)==0&&undo.Stock(ConversationCard.Rewind)==0,"rewind never refunds spent cards");
        undo.Respond(1,2);
        Check(undo.Stock(ConversationCard.TakeOver)==1&&!undo.CanRewind,"re-crossing a round cannot duplicate draws");

        var exhausted=RelationshipRun.Legacy();
        exhausted.Respond(0,1);exhausted.Respond(1,1);exhausted.Respond(0,1);exhausted.Respond(1,1);
        int clarity=exhausted.RedClarity;
        var tired=exhausted.Respond(0,1);
        Check(tired.TooExhaustedToListen&&exhausted.RedClarity==clarity,"zero breath prevents free insight farming");
        Check(exhausted.Rest()&&!exhausted.Rest(),"rest is once per round");
        var repair=RelationshipRun.Legacy();repair.Rest();repair.Respond(0,1);repair.Respond(1,1);repair.Rest();repair.Respond(0,0);repair.Respond(1,0);repair.Rest();
        Check(repair.ManagedMessages==2,"edits leave unresolved promises");repair.Respond(0,1);repair.Respond(1,1);
        Check(repair.ManagedMessages==0&&repair.Decide(1)==RelationshipEnding.SharedTable,"later honesty can repair edited messages");
        var alliances=RelationshipRun.Legacy();alliances.Rest();alliances.Respond(0,1);alliances.Respond(1,1);alliances.Rest();alliances.Respond(0,2);alliances.Respond(1,2);alliances.Rest();alliances.Respond(0,1);alliances.Respond(1,1);
        Check(alliances.Alliances==0&&alliances.Decide(1)==RelationshipEnding.SharedTable,"returning promises can repair earlier alliances");

        var witness=new Dictionary<RelationshipEnding,string>();
        CorrespondenceTests.Run(Check);
        OfficeTests.Run(Check);
        SaveTests.Run(Check);
        var early=RelationshipRun.Legacy();witness[early.Decide(2)]="leave before accepting a message";
        for(int sequence=0;sequence<729;sequence++)
        for(int restMask=0;restMask<8;restMask++)
        for(int final=0;final<3;final++)
        {
            var r=RelationshipRun.Legacy();int code=sequence;string route="";
            for(int step=0;step<6;step++)
            {
                if(step%2==0&&(restMask&(1<<r.Phase))!=0)r.Rest();
                int option=code%3;code/=3;r.Respond(step%2,option);route+=(option+1).ToString();
            }
            var ending=r.Decide(final);
            if(final==2)Check(RelationshipRun.OpensExit(ending),"personal exit never requires reconciliation");
            if(ending==RelationshipEnding.SharedTable)Check(r.RedConsents&&r.BlueConsents&&r.AllVoicesHeard&&r.Bridge>=4,"shared table requires both people's consent");
            if(!witness.ContainsKey(ending))witness[ending]="responses="+route+" / restMask="+restMask+" / final="+(final+1);
        }
        for(int i=1;i<=7;i++)Check(witness.ContainsKey((RelationshipEnding)i),"ending reachable: "+(RelationshipEnding)i);
        DispatchTests.Run();
        CaseDeckTests.Run();
        var output=new List<string>{"RELATIONSHIP MODEL / PASS", "Case deck V9 checks: "+CaseDeckTests.Checks, "Dispatch V8 checks: "+DispatchTests.Checks, "Dispatch V8 resolved packets: "+DispatchTests.Packets, "Legacy compatibility checks: "+checks,"Checks: "+checks,"Random journeys: 3000","Exhaustive choice/rest/final combinations: 17496"};
        output.Add("Random correspondence journeys: 900");
        output.Add("Office random journeys: 600 / exhaustive combinations: 69984 / endings: 7");
        output.Add("Random save roundtrips: 250 across two cases");
        output.Add("Dispatch V8 ending route combinations: "+DispatchTests.EndingRoutes);
        output.Add("Dispatch V8 random journeys: 600");
        output.AddRange(DispatchTests.EndingWitnesses);
        output.AddRange(OfficeTests.LastWitnesses);
        foreach(var pair in witness)output.Add(pair.Key+" / "+pair.Value);
        foreach(string line in output)Console.WriteLine(line);
        if(args.Length>0)File.WriteAllLines(args[0],output);
    }
}
