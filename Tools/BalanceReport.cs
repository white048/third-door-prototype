using System;
using System.Collections.Generic;
using System.IO;

// Explore the shipped model, without duplicating any resolution rules.
public static class BalanceReport
{
    private static readonly List<string> lines = new List<string>();
    private static string Values(RelationshipRun r) => "BREATH="+r.Capacity+" LOAD="+r.Load+" BRIDGE="+r.Bridge;
    private static void Journey(string name, string route, int rests, int decision, bool pause,CaseId selected=CaseId.Home,bool dispatch=false)
    {
        var r=new RelationshipRun(selected);lines.Add("");lines.Add(name+" / "+selected);lines.Add("Start / "+Values(r));
        for(int step=0;step<route.Length;step++)
        {
            if(step%2==0&&(rests&(1<<r.Phase))!=0&&r.Rest())lines.Add("  Rest / "+Values(r));
            if(pause&&step==0)r.SelectCard(ConversationCard.Pause,0);
            var record=r.Respond(step%2,route[step]-'1');
            lines.Add("  "+(record.Phase+1)+"."+(record.Voice+1)+" option "+(record.Option+1)+" + "+record.Card+" / "+Values(r)+(record.TooExhaustedToListen?" / no new insight":""));
            if(dispatch&&step==1)
            {
                var letter=r.MessageFrom(1);var sent=r.ActOnMessage(letter.Id,MessageAction.Deliver,MessagePayload.Request,0);
                lines.Add("  Deliver 02 public Request to 01 / "+Values(r)+" / allowed="+sent.PermissionGranted);
            }
        }
        var ending=r.Decide(decision);
        lines.Add("Result / "+ending+" / consents="+r.RedConsents+","+r.BlueConsents+" / personal exit="+RelationshipRun.OpensExit(ending));
    }
    public static void Main(string[] args)
    {
        lines.Add("THE THIRD DOOR / BALANCE EXPLORATION / v4 TWO CASES");
        lines.Add("Generated from the same RelationshipRun.cs used by the player.");
        lines.Add("Home routes explore main responses; Office routes explicitly compare actual public dispatch with no delivery. Random correspondence journeys are tested separately.");
        lines.Add("B = breath available for attention; L = dependency on the messenger; bridge never substitutes for consent.");
        Journey("Carry every request; never rest", "111111",0,0,false);
        Journey("Edit and manage; rest every round", "211111",7,0,false);
        Journey("Take sides to gain closeness", "123311",7,0,false);
        Journey("Ask every time; never make room for yourself", "222222",0,1,false);
        Journey("Ask and rest; invite both voices", "222222",7,1,false);
        Journey("Use PAUSE to sustain attention; no bench", "222222",0,1,true);
        Journey("Name your limits, then leave", "33",0,2,false);
        Journey("Leave before accepting a message", "",0,2,false);
        Journey("Office: protect scope, actually deliver public Request, agree", "232323",7,1,false,CaseId.Office,true);
        Journey("Office: protect scope but leave public Request undelivered", "232323",7,1,false,CaseId.Office,false);
        Journey("Office: repeat the family asking pattern, even with public delivery", "222222",7,1,false,CaseId.Office,true);
        Journey("Office: leave an allegiance uncorrected", "213111",7,0,false,CaseId.Office,false);
        var totals=new int[8];
        for(int sequence=0;sequence<729;sequence++)
        for(int restMask=0;restMask<8;restMask++)
        for(int final=0;final<3;final++)
        {
            var r=new RelationshipRun();int code=sequence;
            for(int step=0;step<6;step++)
            {
                if(step%2==0&&(restMask&(1<<r.Phase))!=0)r.Rest();
                r.Respond(step%2,code%3);code/=3;
            }
            totals[(int)r.Decide(final)]++;
        }
        lines.Add("");lines.Add("EXHAUSTIVE NO-CARD COUNTS / 17,496 route-rest-final combinations");
        lines.Add("Counts describe the rule space, not player probabilities or a target ending distribution.");
        for(int i=1;i<=7;i++)lines.Add(((RelationshipEnding)i)+" / "+totals[i]);
        lines.Add("");lines.Add("DESIGN READOUT");
        lines.Add("Constantly asking without rest can exhaust attention. Listening is not a free universal solution.");
        lines.Add("Short-term trust is available through carrying or allegiance, so trust alone does not measure freedom.");
        lines.Add("Pause changes timing. Quote preserves wording and adds load; it never grants private permission. Reframe spends trust as well as breath.");
        lines.Add("Office agreement requires an actual delivered public Request and scope consent. Deeper private questioning is not a universal route.");
        lines.Add("Earlier edits and allegiances can be corrected; the model does not make a first mistake permanent.");
        lines.Add("A shared table can coexist with tiredness. Reconciliation and personal capacity are separate axes.");
        lines.Add("Leaving early, setting limits, and resting are valid positions with no timer penalty.");
        if(args.Length>0)File.WriteAllLines(args[0],lines);
        Console.WriteLine("BALANCE REPORT / "+lines.Count+" lines / 17496 combinations");
        Console.WriteLine(string.Join("; ",Array.ConvertAll(totals,v=>v.ToString())));
    }
}
