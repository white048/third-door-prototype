using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

// Bounded, deterministic planning through the shipped model. These are not player win rates.
public static class CaseDeckBalance
{
    sealed class Node
    {
        public RelationshipRun Run; public int At=1,Legs,Packets,Steps,Peak,Used; public string Trace="";
        public Node Parent; public Action<RelationshipRun> Apply; public int ActionAt; public string ActionName; public bool Packet; public DispatchTool ActionTool;
        public float Cost { get { return Legs*1.6f+Packets*.7f+(8-Run.Capacity)*1.5f+Run.Load*.65f+Peak*.2f; } }
        public float Rank { get { int bits=0;for(int s=0;s<2;s++){var m=Run.MessageFrom(s);bits+=RelationshipRun.BitCount(m.PublicProgress);if(m.TaskActive)bits--;}
            return bits*16+(Run.RedConsents?3:0)+(Run.BlueConsents?3:0)-Cost; } }
    }
    sealed class Fixture { public string Name; public RelationshipSaveData Save; }
    static RelationshipRun Clone(RelationshipRun r){RelationshipRun copy;if(!RelationshipRun.TryRestoreSave(r.ExportSave(),out copy))throw new Exception("planner generated an invalid save");return copy;}
    static Node Step(Node n,int at,string action,Action<RelationshipRun> apply,bool packet=false,DispatchTool tool=DispatchTool.None)
    {
        var r=Clone(n.Run);apply(r);int legs=n.Legs+(n.At==at?0:1);
        return new Node{Run=r,At=at,Legs=legs,Packets=n.Packets+(packet?1:0),Steps=n.Steps+1,Peak=Math.Max(n.Peak,r.Load),Used=n.Used|(tool==DispatchTool.None?0:1<<((int)tool-1)),Trace=n.Trace+action+" [B"+r.Capacity+" L"+r.Load+"]; ",Parent=n,Apply=apply,ActionAt=at,ActionName=action,Packet=packet,ActionTool=tool};
    }
    static Node Replay(RelationshipRun seed,Node certificate)
    {
        var path=new Stack<Node>();for(var n=certificate;n.Parent!=null;n=n.Parent)path.Push(n);
        var root=new Node{Run=seed,Peak=seed.Load};
        while(path.Count>0){var step=path.Pop();int revision=root.Run.Revision;root=Step(root,step.ActionAt,step.ActionName,step.Apply,step.Packet,step.ActionTool);if(root.Run.Revision<=revision)throw new Exception("route certificate contains an unavailable action");}
        if(!Goal(root.Run))throw new Exception("route certificate did not reach its goal");return root;
    }
    static bool Goal(RelationshipRun r)
    {
        if(!r.BothPlansVerified||r.CurrentTasks)return false;
        if(r.Phase<2)return r.CanAdvanceEncounter;
        return r.RedConsents&&r.BlueConsents&&Clone(r).Decide(1)==RelationshipEnding.SharedTable;
    }
    static string Key(Node n)
    {
        var r=n.Run;var b=new StringBuilder();b.Append(n.At).Append(',').Append(r.Capacity).Append(',').Append(r.Load).Append(',').Append(r.Bridge).Append(',').Append(r.RedClarity).Append(',').Append(r.BlueClarity).Append(',').Append(r.RedTrust).Append(',').Append(r.BlueTrust).Append(',').Append(r.ManagedMessages).Append(',').Append(r.RedConsents).Append(',').Append(r.BlueConsents).Append(',').Append(r.RestUsedThisRound);
        for(int i=1;i<=9;i++)b.Append(r.DeckStock((DispatchTool)i));
        for(int s=0;s<2;s++){var m=r.MessageFrom(s);b.Append('|').Append(m.PublicProgress).Append(',').Append(m.DispatchAttempts).Append(',').Append(m.SourceChecked).Append(',').Append(m.Inspected).Append(',').Append(m.TaskActive).Append(',').Append(m.Assumed).Append(',').Append(m.GuessHeard).Append(',').Append((int)m.LastTool);}
        return b.ToString();
    }
    static IEnumerable<Node> Expand(Node n)
    {
        var r=n.Run;
        if(r.CanRest)yield return Step(n,2,"bench",x=>x.Rest());
        for(int source=0;source<2;source++)
        {
            int s=source;var m=r.MessageFrom(s);
            foreach(var action in new[]{MessageAction.CheckFacts,MessageAction.HandOffTask,MessageAction.Reconfirm})
            {
                int at=action==MessageAction.HandOffTask?1-s:s;
                if(r.CanActOnMessage(m.Id,action,MessagePayload.Request,at))yield return Step(n,at,"basic "+s+" "+action,x=>x.ActOnMessage(x.MessageFrom(s).Id,action,MessagePayload.Request,at));
            }
            for(int tool=0;tool<=9;tool++)
            {
                var t=(DispatchTool)tool;if(!r.CanUseDispatchTool(s,t))continue;
                // Accurate packets and empty tool actions; fabricated guarantees are analysed by rule tests separately.
                foreach(var pair in new[]{new[]{0,0},new[]{1,0},new[]{2,0},new[]{1,2},new[]{2,1}})
                {
                    var f=pair[0]==0?DispatchFragment.None:pair[0]==1?DispatchFragment.Fact:DispatchFragment.Request;
                    var second=pair[1]==0?DispatchFragment.None:pair[1]==1?DispatchFragment.Fact:DispatchFragment.Request;
                    if(!r.CanSendPacket(m.Id,f,second,t,1-s))continue;
                    yield return Step(n,1-s,s+":"+f+"/"+second+"+"+t,x=>x.SendPacket(x.MessageFrom(s).Id,f,second,t,1-s),true,t);
                }
            }
        }
    }
    static Node Search(RelationshipRun run,int width,int depth)
    {
        var start=new Node{Run=run,Peak=run.Load};var frontier=new List<Node>{start};var seen=new Dictionary<string,float>();Node best=null;
        for(int step=0;step<=depth&&frontier.Count>0;step++)
        {
            var next=new List<Node>();
            foreach(var n in frontier)
            {
                if(Goal(n.Run)){if(best==null||n.Cost<best.Cost)best=n;continue;}
                if(step==depth)continue;
                foreach(var child in Expand(n))
                {
                    string key=Key(child);float prior;
                    if(seen.TryGetValue(key,out prior)&&prior<=child.Cost)continue;
                    seen[key]=child.Cost;next.Add(child);
                }
            }
            frontier=next.OrderByDescending(n=>n.Rank).ThenBy(n=>n.Trace,StringComparer.Ordinal).Take(width).ToList();
        }
        return best;
    }
    static void BasicSend(RelationshipRun r,int source)
    {
        var m=r.MessageFrom(source);if(r.Phase==1&&!m.SourceChecked)r.ActOnMessage(m.Id,MessageAction.CheckFacts,MessagePayload.Request,source);
        var f=source==0?DispatchFragment.Request:DispatchFragment.Fact;var second=source==0?DispatchFragment.Fact:DispatchFragment.Request;
        if(r.SendPacket(m.Id,f,second,DispatchTool.None,1-source)==null)throw new Exception("fixture cannot start basic delivery");
        if(!r.PublicPlanVerified(source)&&r.SendPacket(m.Id,second,DispatchFragment.None,DispatchTool.None,1-source)==null)throw new Exception("fixture cannot finish basic delivery");
    }
    static List<Fixture> Fixtures(bool previous)
    {
        var result=new List<Fixture>();var r=previous?RelationshipRun.PreviousDeckCase():RelationshipRun.DeckCase();int mask=RelationshipRun.DeckPreset(0);r.PrepareDeck(mask);
        for(int phase=0;phase<3;phase++)
        {
            r.Rest();r.Respond(0,phase==2?1:0);r.Respond(1,phase==2?2:0);
            result.Add(new Fixture{Name=phase==0?"first-plan":phase==1?"new-edition":"rushed-confirmation",Save=r.ExportSave()});
            if(phase==0)
            {
                var guarded=r.ExportSave();guarded.State.BlueTrust=0;guarded.State.RedTrust=0;guarded.State.RedClarity=Math.Max(1,guarded.State.RedClarity);guarded.State.BlueClarity=Math.Max(1,guarded.State.BlueClarity);foreach(var envelope in guarded.Messages)envelope.SourceChecked=true;result.Add(new Fixture{Name="guarded-recipient",Save=guarded});
                var heavy=r.ExportSave();heavy.State.Load=6;heavy.State.Capacity=4;result.Add(new Fixture{Name="heavy-load",Save=heavy});
            }
            if(phase==1)
            {
                var low=r.ExportSave();low.State.Capacity=2;result.Add(new Fixture{Name="low-breath-edition",Save=low});
                var missing=r.ExportSave();missing.Messages[missing.Messages.Length-2].DispatchAttempts=2;result.Add(new Fixture{Name="two-attempts-left",Save=missing});
            }
            if(phase<2)
            {
                // Establish the next checkpoint without consuming any hypothetical hand's cards.
                if(phase==1){r.ActOnMessage(r.MessageFrom(0).Id,MessageAction.CheckFacts,MessagePayload.Request,0);r.ActOnMessage(r.MessageFrom(1).Id,MessageAction.CheckFacts,MessagePayload.Request,1);}
                BasicSend(r,0);BasicSend(r,1);
                if(!r.CanAdvanceEncounter)throw new Exception("fixture closure failed");r.Decide(1);r.PrepareDeck(mask);
            }
        }
        return result;
    }
    static string Cards(int mask){return string.Join("+",Enumerable.Range(1,9).Where(i=>(mask&(1<<(i-1)))!=0).Select(i=>((DispatchTool)i).ToString()).ToArray());}
    public static void Main(string[] args)
    {
        string output=args.Length>0?args[0]:"CaseDeckBalance";int width=args.Length>1?int.Parse(args[1]):48;int depth=10;
        Directory.CreateDirectory(output);var csv=new List<string>{"fixture,mask,cards,found,cost,breath,load,peakLoad,doorLegs,packets,cardsUsed,trace"};var notes=new List<string>{"Bounded accurate-message planner; not player win rates.","Width="+width+"; depth="+depth+"; every one of 126 five-card masks; starts at door 02 after fixed dialogue. Costs account for changing door/bench location.","Objective: min 1.6*legs + .7*packets + 1.5*(8-breath) + .65*load + .2*peakLoad. Results are candidates, not proofs of optimal play.","Discovered route certificates are shared with all compatible hands and REPLAYED through the actual model, so irrelevant spare cards cannot create spurious preset differences.","Fixtures: actual authored checkpoints after fixed dialogue, with explicit stress-state edits for both trust values 0, load 6, breath 2, and two used attempts. No private disclosure or fabricated-guarantee actions in this planner."};
        notes.Add("Edition="+(args.Length>2&&args[2]=="previous"?"V9 preserved":"V10 tuned")+"; guarded fixture has both sources already checked.");
        foreach(var fixture in Fixtures(args.Length>2&&args[2]=="previous"))
        {
            int found=0;float minimum=999;var winners=new List<string>();var seeds=new Dictionary<int,RelationshipRun>();var certificates=new List<Node>();
            for(int mask=0;mask<512;mask++)
            {
                if(RelationshipRun.BitCount(mask)!=5)continue;
                RelationshipRun run;if(!RelationshipRun.TryRestoreSave(fixture.Save,out run))throw new Exception("invalid fixture "+fixture.Name);
                var data=run.ExportSave();data.Deck.Mask=mask;for(int i=0;i<=data.Deck.PreparedPhase;i++)data.Deck.Loadouts[i]=mask;for(int i=0;i<9;i++)data.Deck.Stock[i]=(mask&(1<<i))==0?0:1;
                if(!RelationshipRun.TryRestoreSave(data,out run))throw new Exception("invalid candidate hand");
                seeds[mask]=run;var candidate=Search(Clone(run),width,depth);if(candidate!=null)certificates.Add(candidate);
            }
            foreach(var entry in seeds)
            {
                int mask=entry.Key;var certificate=certificates.Where(n=>(n.Used&mask)==n.Used).OrderBy(n=>n.Cost).ThenBy(n=>n.Trace,StringComparer.Ordinal).FirstOrDefault();
                var best=certificate==null?null:Replay(entry.Value,certificate);
                if(best==null){csv.Add(fixture.Name+","+mask+","+Cards(mask)+",false,,,,,,,,");continue;}
                found++;if(best.Cost<minimum-.001f){minimum=best.Cost;winners.Clear();}if(Math.Abs(best.Cost-minimum)<.001f)winners.Add(mask.ToString());
                csv.Add(string.Join(",",new[]{fixture.Name,mask.ToString(),Cards(mask),"true",best.Cost.ToString("0.00",CultureInfo.InvariantCulture),best.Run.Capacity.ToString(),best.Run.Load.ToString(),best.Peak.ToString(),best.Legs.ToString(),best.Packets.ToString(),RelationshipRun.BitCount(best.Used).ToString(),best.Trace}));
                for(int p=0;p<3;p++)if(mask==RelationshipRun.DeckPreset(p))notes.Add(fixture.Name+" / preset "+p+" / cost="+best.Cost.ToString("0.00",CultureInfo.InvariantCulture)+" / breath="+best.Run.Capacity+" / load="+best.Run.Load+" / legs="+best.Legs+" / packets="+best.Packets+" / "+best.Trace);
            }
            notes.Add(fixture.Name+" / found="+found+"/126 / best="+minimum.ToString("0.00",CultureInfo.InvariantCulture)+" / tiedMasks="+string.Join("+",winners));
            Console.WriteLine(fixture.Name+" / "+found+"/126 / cost="+minimum.ToString("0.00",CultureInfo.InvariantCulture));
        }
        File.WriteAllLines(Path.Combine(output,"hands.csv"),csv.Select(line=>line.TrimEnd()));File.WriteAllLines(Path.Combine(output,"summary.txt"),notes.Select(line=>line.TrimEnd()));
    }
}
