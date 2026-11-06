using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
public static class DispatchTests
{
    public static int Checks,Packets,EndingRoutes;
    public static readonly List<string> EndingWitnesses=new List<string>();
    private static void Check(bool v,string name){Checks++;if(!v)throw new Exception("DISPATCH FAIL / "+name);}
    public static string Fingerprint(object value)
    {
        if(value==null)return "null";var type=value.GetType();if(type.IsPrimitive||type.IsEnum||value is string)return value.ToString();
        var s=new StringBuilder();var array=value as IEnumerable;
        if(array!=null){foreach(var item in array)s.Append('[').Append(Fingerprint(item)).Append(']');return s.ToString();}
        foreach(var field in type.GetFields(BindingFlags.Public|BindingFlags.Instance))s.Append(field.Name).Append('=').Append(Fingerprint(field.GetValue(value))).Append(';');return s.ToString();
    }
    private static RelationshipRun Restore(RelationshipSaveData data)
    {RelationshipRun r;Check(RelationshipRun.TryRestoreSave(data,out r),"complete v2 save accepted");return r;}
    private static void Roundtrip(RelationshipRun r)
    {Check(Fingerprint(r.ExportSave())==Fingerprint(Restore(r.ExportSave()).ExportSave()),"roundtrip preserves progress, deck, replies and undo");}
    private static CorrespondenceEvent Send(RelationshipRun r,int source,DispatchFragment first,DispatchFragment second=DispatchFragment.None,DispatchTool tool=DispatchTool.None)
    {
        var m=r.MessageFrom(source);string before=Fingerprint(r.ExportSave());var pre=r.PreviewPacket(m.Id,first,second,tool,1-source);
        Check(Fingerprint(r.ExportSave())==before,"preview is completely read only");var actual=r.SendPacket(m.Id,first,second,tool,1-source);
        Check(pre!=null&&actual!=null,"authored packet available");Check(Fingerprint(pre)==Fingerprint(actual),"actual packet equals preview");Packets++;Roundtrip(r);return actual;
    }
    private static RelationshipRun Fixture(CaseId id,int phase,int source,int trust)
    {
        var r=new RelationshipRun(id);r.Respond(source,0);if(phase>0){r.Respond(1-source,0);r.Rest();}
        if(phase>1){r.Respond(0,1);r.Respond(1,id==CaseId.Office?2:1);r.Rest();}
        var d=r.ExportSave();d.State.Capacity=8;d.State.RedClarity=d.State.BlueClarity=2;
        if(source==0)d.State.BlueTrust=trust;else d.State.RedTrust=trust;
        foreach(var m in d.Messages)m.HasNeed=true;
        d.HasUndo=false;d.Undo=null;return Restore(d);
    }
    public static void Run()
    {
        foreach(CaseId id in new[]{CaseId.Home,CaseId.Office})
        {
            // Timing can save the entire dispatch deck: the two cases reward a different first door.
            var timed=new RelationshipRun(id);timed.Rest();int firstSource=id==CaseId.Home?1:0;
            timed.Respond(firstSource,0);Send(timed,firstSource,timed.PriorityAt(1-firstSource),timed.PriorityAt(1-firstSource)==DispatchFragment.Fact?DispatchFragment.Request:DispatchFragment.Fact);
            timed.Respond(1-firstSource,0);timed.Rest();int other=1-firstSource;
            Send(timed,other,timed.PriorityAt(1-other),timed.PriorityAt(1-other)==DispatchFragment.Fact?DispatchFragment.Request:DispatchFragment.Fact);
            timed.Respond(0,0);timed.Respond(1,0);timed.Rest();timed.Respond(0,1);timed.Respond(1,id==CaseId.Office?2:1);
            Check(timed.Decide(1)==RelationshipEnding.SharedTable&&timed.Correspondence.Count==2,"reading each case's timing gives a two-attempt agreement");
            Check(timed.Stock(ConversationCard.Pause)==1&&timed.Stock(ConversationCard.Quote)==1&&!timed.MessageFrom(0).HasNeed&&!timed.MessageFrom(1).HasNeed,"timed route keeps the deck and both private reasons");
            var r=new RelationshipRun(id);Check(r.DispatchRules,"new games use ordered dispatch");r.Rest();r.Respond(0,0);r.Respond(1,0);r.Rest();
            Check(!r.MessageFrom(0).HasNeed&&!r.MessageFrom(1).HasNeed,"public arrangements reveal no private need");
            var a=Send(r,0,DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.Pause);
            Check(a.Completed&&r.Stock(ConversationCard.Pause)==0,"pause buys time and shares the finite deck");
            var b=Send(r,1,DispatchFragment.Fact,DispatchFragment.Request);if(!b.Completed)Send(r,1,DispatchFragment.Request);
            Check(r.BothPlansVerified&&r.RedClarity>=2&&r.BlueClarity>=2,"practical delivery teaches enough without private questioning");
            r.Respond(0,0);r.Respond(1,0);r.Rest();r.Respond(0,1);r.Respond(1,id==CaseId.Office?2:1);
            Check(r.RedConsents&&r.BlueConsents&&r.Decide(1)==RelationshipEnding.SharedTable,"concrete agreement reachable in both cases");Roundtrip(r);
            Check(!r.MessageFrom(0).HasNeed&&!r.MessageFrom(1).HasNeed,"a full agreement never requires unlocking private motives");
            var paper=Fixture(id,1,0,3);var guessed=Send(paper,0,DispatchFragment.Guess);
            Check(guessed.Provisional&&paper.HasAssumedPlan&&!paper.PublicPlanVerified(0),"invented promise produces a provisional plan");
            var fixedPlan=Send(paper,0,DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.Quote);
            Check(fixedPlan.Completed&&fixedPlan.CorrectedAssumption&&!paper.HasAssumedPlan,"original words correct a provisional plan");
            Check(paper.SendPacket(paper.MessageFrom(0).Id,DispatchFragment.Guess,DispatchFragment.None,DispatchTool.None,1)==null,"verified envelope cannot farm rewards");
            var wrong=Fixture(id,0,0,3);var missed=Send(wrong,0,DispatchFragment.Fact,DispatchFragment.Request);
            Check(missed.PriorityMiss&&missed.PublicAfter==0&&wrong.MessageFrom(0).Inspected,"wrong order teaches preference at a cost");
            Check(Send(wrong,0,DispatchFragment.Request,DispatchFragment.Fact).Completed,"changed order recovers from a missed attempt");
            var guarded=Fixture(id,1,1,0);Check(Send(guarded,1,DispatchFragment.Fact).Guarded,"distrust blocks unsupported words");
            Check(Send(guarded,1,DispatchFragment.Fact,DispatchFragment.Request,DispatchTool.Quote).Completed,"quotation crosses distrust");
            var privateRun=Fixture(id,1,0,3);Check(Send(privateRun,0,DispatchFragment.Need).Breach&&privateRun.HasUnrepairedDisclosure(0),"private fragment leaves attributable breach");
            Check(!privateRun.FragmentAvailable(privateRun.MessageFrom(0),DispatchFragment.Need),"private words cannot be farmed");
            var question=Fixture(id,1,0,3);Check(Send(question,0,DispatchFragment.Guess,DispatchFragment.None,DispatchTool.Reframe).QuestionedGuess&&!question.HasAssumedPlan,"reframing asks rather than invents consent");
            var mixed=Fixture(id,1,0,3);Send(mixed,0,DispatchFragment.Request);
            Check(!mixed.CanSendPacket(mixed.MessageFrom(0).Id,DispatchFragment.Fact,DispatchFragment.Guess,DispatchTool.None,1),"a final true fragment cannot launder an added guarantee");
            Check(Send(mixed,0,DispatchFragment.Fact,DispatchFragment.Guess,DispatchTool.Reframe).Completed&&!mixed.HasAssumedPlan,"an explicit unconfirmed question can accompany the true final fragment");
            var limit=Fixture(id,2,0,0);for(int i=0;i<4;i++)Send(limit,0,DispatchFragment.Fact);
            Check(!limit.CanSendPacket(limit.MessageFrom(0).Id,DispatchFragment.Request,DispatchFragment.None,DispatchTool.None,1),"four ordinary attempts is a firm limit");
            Check(Send(limit,0,DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.TakeOver).Completed,"take over buys exactly one extra attempt");
            var unverified=new RelationshipRun(id);unverified.Rest();for(int p=0;p<3;p++){if(p>0)unverified.Rest();unverified.Respond(0,1);unverified.Respond(1,id==CaseId.Office&&p>0?2:1);}
            Check(!unverified.RedConsents&&!unverified.BlueConsents,"menu answers cannot replace delivery");
            var late=Restore(unverified.ExportSave());
            Send(late,0,DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.Pause);Send(late,1,DispatchFragment.Fact,DispatchFragment.Request,DispatchTool.Quote);
            Check(!late.RedConsents&&!late.BlueConsents,"late delivery never invents consent");
            for(int voice=0;voice<2;voice++){Check(late.CanReconfirmPlan(voice),"late confirmation becomes available");Check(late.ActOnMessage(late.MessageFrom(voice).Id,MessageAction.Reconfirm,MessagePayload.Request,voice)!=null,"individual late confirmation resolves");Check(!late.CanReconfirmPlan(voice),"late confirmation cannot repeat");Roundtrip(late);}
            Check(late.Decide(1)==RelationshipEnding.SharedTable,"late delivery and fresh confirmations can reach agreement");
            Check(unverified.Decide(1)!=RelationshipEnding.SharedTable,"skipping delivery cannot reach agreement");
            var old=RelationshipRun.Legacy(id);old.Respond(0,1);var v1=old.ExportSave();v1.Version=1;Check(!Restore(v1).DispatchRules,"v1 saves retain original rules");
        }
        foreach(CaseId id in new[]{CaseId.Home,CaseId.Office})for(int phase=0;phase<3;phase++)for(int source=0;source<2;source++)foreach(int trust in new[]{0,1,3})
        {
            var baseline=Fixture(id,phase,source,trust).ExportSave();
            for(int f=0;f<=4;f++)for(int s=0;s<=4;s++)for(int t=0;t<=6;t++)
            {
                var r=Restore(baseline);var m=r.MessageFrom(source);var first=(DispatchFragment)f;var second=(DispatchFragment)s;var tool=(DispatchTool)t;
                string before=Fingerprint(r.ExportSave());var preview=r.PreviewPacket(m.Id,first,second,tool,1-source);
                Check(Fingerprint(r.ExportSave())==before,"candidate preview preserves all fields");bool can=r.CanSendPacket(m.Id,first,second,tool,1-source);
                Check(can==(preview!=null),"legality and preview agree");var actual=r.SendPacket(m.Id,first,second,tool,1-source);Check(can==(actual!=null),"legality and dispatch agree");
                if(!can){Check(Fingerprint(r.ExportSave())==before,"invalid candidate cannot mutate");continue;}
                Packets++;Check(Fingerprint(actual)==Fingerprint(preview),"candidate resolves as previewed");
                Check(r.Capacity>=0&&r.Capacity<=8&&r.Load>=0&&r.Load<=8&&r.Bridge>=0&&r.Bridge<=6,"bounded resources");
                Check(m.DispatchAttempts==1&&m.PublicProgress>=0&&m.PublicProgress<=3,"bounded progress");
                if(tool!=DispatchTool.None)Check(r.Stock(RelationshipRun.CardFor(tool))==0,"shared card consumed");
                Check(actual.Completed==r.PublicPlanVerified(source),"verified plan agrees with receipt");Roundtrip(r);
            }
        }
        foreach(CaseId id in new[]{CaseId.Home,CaseId.Office})
        {
            var found=new HashSet<RelationshipEnding>();found.Add(RelationshipEnding.SharedTable);
            EndingWitnesses.Add("V8 "+id+" / SharedTable / carry both; request-first + pause; fact-first + follow-up; carry again; confirm both");
            for(int sequence=0;sequence<729&&found.Count<7;sequence++)for(int restMask=0;restMask<8&&found.Count<7;restMask++)for(int final=0;final<3&&found.Count<7;final++)for(int lead=0;lead<2&&found.Count<7;lead++)
            {
                var r=new RelationshipRun(id);int code=sequence;string route="";
                for(int step=0;step<6;step++){if(step%2==0&&(restMask&(1<<r.Phase))!=0)r.Rest();int option=code%3;code/=3;int voice=(step+lead)%2;r.Respond(voice,option);route+=(option+1).ToString();}
                var ending=r.Decide(final);EndingRoutes++;
                if(found.Add(ending))EndingWitnesses.Add("V8 "+id+" / "+ending+" / responses="+route+" / lead="+lead+" / restMask="+restMask+" / final="+final);
            }
            Check(found.Count==7,"all seven new-rule endings have a reachable witness in each case");
        }
        var random=new Random(20261104);
        for(int journey=0;journey<600;journey++)
        {
            var r=new RelationshipRun((CaseId)(journey%2));
            for(int step=0;step<24&&!r.Ended;step++)
            {
                if(random.Next(4)==0)r.Rest();
                if(random.Next(2)==0&&(r.CanTalk(0)||r.CanTalk(1)))
                {int voice=r.CanTalk(0)&&r.CanTalk(1)?random.Next(2):r.CanTalk(0)?0:1;r.SelectCard((ConversationCard)random.Next(-1,6),voice);r.Respond(voice,random.Next(3));}
                else if(r.Messages.Count>0)
                {
                    int source=r.Messages[random.Next(r.Messages.Count)].Source;var m=r.MessageFrom(source);
                    var f=(DispatchFragment)random.Next(5);var second=(DispatchFragment)random.Next(5);var tool=(DispatchTool)random.Next(7);
                    if(r.CanSendPacket(m.Id,f,second,tool,1-source))Send(r,source,f,second,tool);
                    if(r.CanReconfirmPlan(source)){Check(r.ActOnMessage(m.Id,MessageAction.Reconfirm,MessagePayload.Request,source).ConsentRenewed,"late confirmation resolves");Check(!r.CanReconfirmPlan(source),"late confirmation single use");}
                }
                Roundtrip(r);
            }
            r.Decide(2);Check(r.Ended&&RelationshipRun.OpensExit(r.Ending),"leaving always remains possible");Roundtrip(r);
        }
        Console.WriteLine("DISPATCH V8 / PASS / Checks: "+Checks+" / resolved packets: "+Packets);
    }
}
