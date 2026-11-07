using static NarrativeText;

public static class DispatchText
{
    public static string Name(DispatchFragment part)=>part==DispatchFragment.Fact?L("已知的事","WHAT YOU KNOW"):part==DispatchFragment.Request?L("要商量的事","THE PROPOSAL"):part==DispatchFragment.Need?L("私下那句","THE PRIVATE WORDS"):part==DispatchFragment.Guess?L("你补的一句","YOUR ADDITION"):L("空着","EMPTY");
    public static string Fragment(CaseId id,int source,DispatchFragment part,DispatchTool tool=DispatchTool.None,int phase=-1)
    {
        if(id==CaseId.Office&&CaseDeckText.Active)return CaseDeckText.Fragment(phase<0?GameStateManager.Instance.Run.Phase:phase,source,part,tool);
        if(part==DispatchFragment.None)return "";
        if(part==DispatchFragment.Need)return id==CaseId.Home?(source==0?L("她想知道，自己在他心里还有没有位置。","She wants to know she still has a place in his life."):L("他还想保持联系，但想自己拿主意。","He still wants to stay in touch, on his own terms.")):(source==0?L("他怕草稿交不出来，最后还得自己兜底。","They're worried they'll be left to finish the draft."):L("私人原因，他想留着自己说。","They want to discuss their private reasons themselves."));
        if(part==DispatchFragment.Guess)
        {
            if(tool==DispatchTool.Reframe)return id==CaseId.Home?L("我没核对过：能不能先试一次十分钟的电话？","I haven't checked this: could we try one ten-minute call?"):L("我没核对过：能否先按白天评审来商量？","I haven't checked this: could we discuss a daytime review?");
            return id==CaseId.Home?(source==0?L("她已经保证，以后不会临时加问题。","She's promised there won't be any surprise questions."):L("他已经保证，以后都会准时接电话。","He's promised he'll always pick up on time.")):(source==0?L("负责人保证，这次不会再改需求。","The manager guarantees there won't be more changes."):L("同事保证，这周每天都能加班配合。","Your colleague guarantees they can work late all week."));
        }
        if(id==CaseId.Home)
            return part==DispatchFragment.Fact?(source==0?L("她周日晚上在家。","She'll be home on Sunday evening."):L("他周日六点半能接电话。","He can take a call at 6:30 on Sunday."))
                :(source==0?L("她想六点半通十分钟电话。","She proposes a ten-minute call at 6:30."):L("他想只聊十分钟，不临时加问题。","He asks for ten minutes, without surprise questions."));
        return part==DispatchFragment.Fact?(source==0?L("草稿的期限是周五。","The draft is due on Friday."):L("同事能在周五交出草稿。","Your colleague can deliver the draft on Friday."))
            :(source==0?L("请同事报一个能参加评审的时间。","The manager asks for a review time your colleague can attend."):L("同事希望评审改到白天。","Your colleague asks to move the review to daytime."));
    }
    public static string Packet(CaseId id,int source,DispatchFragment first,DispatchFragment second,DispatchTool tool,int phase=-1,bool confirmationQuestion=false)
    {
        if(id==CaseId.Office&&CaseDeckText.Active)return CaseDeckText.Packet(phase<0?GameStateManager.Instance.Run.Phase:phase,source,first,second,tool,confirmationQuestion);
        string text=Fragment(id,source,first,tool);
        if(second!=DispatchFragment.None)text+="\n"+Fragment(id,source,second,tool);
        if(first==DispatchFragment.None)return L("我先问一句，你现在最急着弄清什么？","Before I pass this on, what do you need to know first?");
        string prefix=tool==DispatchTool.Quote?L("这是对方原来的意思：\n","Their words, as they meant them:\n"):tool==DispatchTool.Boundary?L("这趟我只带这几句：\n","This time, I'm only carrying these words:\n"):tool==DispatchTool.TakeOver?L("我先替这份安排担保：\n","I'll put my name behind this plan:\n"):tool==DispatchTool.Pause?L("等一下，我理一理：\n","Give me a moment to get this straight:\n"):"";
        return prefix+text;
    }
    public static string HeardWords(CorrespondenceEvent e)=>Packet(CaseInfo.Active,e.Source,e.First,e.Second,e.Tool,e.Phase,e.ConsentRenewed);
    public static string Actor(RelationshipRun run,MessageEnvelope m)
    {
        if(run.DeckRules)return CaseDeckText.Actor(run,m.Source);
        int target=1-m.Source;
        if(run.Trust(target)==0)return run.Case==CaseId.Office?L("“你上次那份‘准信’，落款写的是谁？”","Whose name was on that last 'confirmed' plan of yours?"):L("“这回是他的话，还是你替他想的？”","Are these their words, or what you think they mean?");
        if(run.Case==CaseId.Office)return target==0
            ?L("“先说周五能不能交。我这儿的表格比咖啡还凉。”","Start with whether Friday is possible. Even my spreadsheets have gone cold.")
            :L("“先说评审怎么排。事由栏今天放假。”","Tell me about the review slot first. The reason field has the day off.");
        return target==0?L("“周日能不能接？我还没问天气呢。”","Can he take the call on Sunday? I haven't even asked about the weather yet.")
            :L("“先说聊多久。‘十分钟’后面有没有括号？”","How long? Is there anything in brackets after 'ten minutes'?");
    }
    public static string TimeClue(RelationshipRun run,MessageEnvelope m)=>run.HearingRoom(1-m.Source,m)==1?L("“赶着走，先说一件。”","I'm on my way out. One thing first."):L("“两件事，说吧。”","Two things. Go on.");
    public static string ToolName(DispatchTool tool)=>CaseDeckText.Active?CaseDeckText.Name(tool):ToolNameLegacy(tool);
    public static string ToolNameLegacy(DispatchTool tool)=>tool==DispatchTool.None?L("直接说","JUST SAY IT"):CardName(RelationshipRun.CardFor(tool));
    public static string ToolDetail(DispatchTool tool)
    {
        if(CaseDeckText.Active)return CaseDeckText.Detail(tool);
        switch(tool)
        {
            case DispatchTool.Quote:return L("整句听完，也能应对怀疑。承重 +1；消耗“原话”。","They hear both pieces, even when doubtful. LOAD +1; spends QUOTE.");
            case DispatchTool.Pause:return L("给两段话留时间，余力先恢复 2；消耗“停顿”。","Make room for both pieces. Restore 2 BREATH first; spends PAUSE.");
            case DispatchTool.Listen:return L("先问清重点，再把两段说完。也可空手问；消耗“追问”。","Ask what matters first, then say both pieces. Can be used without words; spends LISTEN.");
            case DispatchTool.Reframe:return L("把补出的保证改成待确认的问题，缓和怀疑；消耗“翻译”。","Turn your addition into a question and ease doubt; spends REFRAME.");
            case DispatchTool.Boundary:return L("只带这两句，给双方留出空间。承重 -2；消耗“界限”。","Make room for these two pieces only. LOAD -2; spends BOUNDARY.");
            case DispatchTool.TakeOver:return L("替安排担保，能多换一次接话。承重 +3；消耗“代偿”。","Guarantee the plan, including one extra attempt if needed. LOAD +3; spends TAKE OVER.");
            default:return L("说一次消耗 1 余力。先后顺序和对方现在的工夫，都有影响。","Each attempt costs 1 BREATH. Order matters, and so does how much time they have.");
        }
    }
    public static string Progress(MessageEnvelope m)=>L("已对上：","HEARD: ")+((m.PublicProgress&1)!=0?"● ":"○ ")+Name(DispatchFragment.Fact)+"  /  "+((m.PublicProgress&2)!=0?"● ":"○ ")+Name(DispatchFragment.Request)+(m.Assumed?L("\n先照你的保证排了，原话仍未核对。","\nThey've pencilled in your promise. The original words still need checking."):"");
    public static string Reaction(CorrespondenceEvent e)
        =>CaseDeckText.Active?CaseDeckText.Reaction(e):ReactionLegacy(e);
    public static string ReactionLegacy(CorrespondenceEvent e)
    {
        if(e.Breach)return L("“哦，原来还说了这个。”\n收信人记下了。说这句话的人还不知道。","Oh. They said that too?\nThe recipient notes it down. The speaker doesn't know yet.")+(e.Completed?L("\n两项安排也已记下。","\nBoth parts of the plan were recorded too."):"");
        if(e.Guarded)return L("“先把出处说清楚。你上回就加过一句。”\n对方把纸推回来了。","Tell me whose words these are. You added something last time.\nThey push the paper back.");
        if(e.PriorityMiss)return e.Source==1?L("“等会儿，能不能做到还没说呢。”\n对方敲了敲第一行。","Wait. You haven't told me whether it's possible.\nThey tap the first line."):L("“先别讲经过。到底要怎么安排？”\n话被截住了，后半句还在你这里。","Leave the backstory. What's the actual arrangement?\nThey cut in. You're still holding the rest.");
        if(e.QuestionedGuess)return L("“这句是你问的，那我可以回答。别记成他答应过。”","If that's your question, I can answer it. Don't write it down as their promise.");
        if(e.Unbelieved)return L("“这就全答应了？那让本人来盖章吧。”\n对方没有按这句作安排。","Everything's settled already? Then let them sign it.\nThey don't act on your addition.");
        if(e.Provisional)return L("“行，我先照你说的排。回头别跟我说是你猜的。”\n那一格先被铅笔填上了。","All right. I'll pencil it in. Don't tell me later that you were guessing.\nThey fill the space in with pencil.");
        if(e.Completed)return CaseInfo.Active==CaseId.Office
            ?L("“周五草稿，评审时间另定。我把这两项圈起来了。”\n对方把自己的笔拿了回去。","Friday draft. A review time we can attend. I've circled both.\nThey take their own pen back.")
            :L("“六点半，十分钟。行，闹钟响了就收线。”\n对方把便签留在电话旁边。","6:30. Ten minutes. Fine. We hang up when the timer rings.\nThey leave the note beside the phone.");
        if(e.First==DispatchFragment.None)return L("“我先要的是这件，其他的后说。”\n对方指了指要先听的那一项。","I need this part first. The rest can wait.\nThey point to the line they want first.");
        if(e.Payload==MessagePayload.Need)return L("“这句我听进去了。安排呢？”","I hear that. What about the plan?");
        return e.Interrupted?L("“先停，我只来得及记这一件。”\n这一项留下了，剩下的需要另一次接话。","Stop there. I've only got time to write down one thing.\nOne piece stays. The rest needs another attempt."):L("“这一项记下了。还差另一件。”","I've got that part. There's still the other one.");
    }
    public static string Incoming(CaseId id,int phase,int voice,int receipt,bool disclosure=false)
    {
        if(disclosure)return L("“那句我没让你带出去。先把这件事说清楚。”","I didn't ask you to share that. We need to clear that up first.");
        if(phase==0)return id==CaseId.Home?(voice==0?L("“周日六点半，我想跟他通十分钟电话。\n你替我问问。十分钟，我写着呢。”","I'd like a ten-minute call at 6:30 on Sunday.\nAsk him for me. Ten minutes. I've written it down."):L("“周日六点半能接，只聊十分钟。\n别临时加问题，我不是移动问卷。”","I can take the call at 6:30 on Sunday. Ten minutes.\nNo surprise questions. I'm not a walking questionnaire."))
            :(voice==0?L("“周五要看草稿，让他报个能来的评审时间。\n先给日期，我这儿缺的不是人物传记。”","I need the draft on Friday and a review time they can attend.\nDates first. I'm not short of biographies."):L("“草稿周五给，评审能不能挪到白天？\n原因那格空着，它也没领工资。”","The draft will be ready Friday. Could the review be in the daytime?\nLeave the reason field blank. It's not on the payroll."));
        if(receipt==3)return L("“你说已经定了，我才往日程里写。\n这回别拿一块橡皮当答复。”","I put it in the diary because you said it was settled.\nPlease don't bring me an eraser as the answer.");
        if(receipt==2)return id==CaseId.Home?L("“那两句我听到了，便签还在。\n现在说说，谁来拨这个电话？”","I heard both pieces. The note's still here.\nNow, who's making the call?"):L("“那两项我圈过了。\n下一步请本人确认，别再套一层‘据说’。”","I've circled those two items.\nLet's hear from them now, without another 'apparently'.");
        return phase==1?L("“上一句还没对上，又添了一张纸？\n这回先说最要紧的。”","The last message isn't settled, and here's another piece of paper?\nStart with what matters this time."):L("“电话都拿起来了，还有哪句没带到？”","The phone's already in my hand. What's still missing?");
    }
    public static string Choice(RelationshipRun run,int voice,int option)=>ChoiceAt(run.Case,run.Phase,voice,option);
    public static string ChoiceAt(CaseId id,int phase,int voice,int option)
    {
        if(id==CaseId.Office&&voice==1&&phase==0&&option>0)return option==1?L("你为什么晚上来不了？我好替你解释。","Why can't you come in the evening? I need something to explain it."):L("只核对草稿和评审，原因那格先不填。","Just the draft and review. Leave the reason field blank.");
        if(phase==0)return option==0?L("这两句我带过去。先别再加便签了。","I'll carry those two pieces. Hold the extra notes for now."):option==1?L("你最想让对方听懂的是哪一句？","Which part do you most want them to understand?"):L("这一趟我先不接。","I'll sit this trip out.");
        if(phase==1)
        {
            if(id==CaseId.Office&&voice==1)return option==0?L("我先替你把时间占住，细节回头补。","I'll reserve the slot for you. We'll fill in the details later."):option==1?L("给我个说得过去的理由，我替你过这一关。","Give me a reason I can use, and I'll get it past them."):L("先核对你能交什么，其他格子先空着。","Let's check what you can deliver. Leave the other spaces empty.");
            return option==0?L("我换个更容易点头的版本，先替你排上。","I'll put it in a form they'll accept and pencil it in."):option==1?L("重说一遍，这次只核对要做什么。","Let's go over it again and check what needs doing."):L("那你先别回，我站你这边去谈。","Don't answer yet. I'll go in on your side.");
        }
        if(id==CaseId.Office&&voice==1)return option==0?L("先借我的名字把这事定下来。","Put my name behind it for now."):option==1?L("最后再补一句原因，也许就能定了。","One more reason might settle it."):L("这份安排你来确认，我把笔还给你。","You confirm your part. Here's your pen back.");
        return option==0?L("先借我的名字把这事定下来。","Put my name behind it for now."):option==1?L("你来确认自己的那一项，行吗？","Will you confirm your own part?"):L("我先把这一趟停在这儿。","I'll leave this trip here for now.");
    }
    public static string Feedback(ConversationRecord r)
    {
        if(r.TooExhaustedToListen)return L("“喂？你还在听吗？”\n你只记下了一截。","Hello? Are you still there?\nYou only caught part of it.");
        if(r.Phase==2&&(r.Voice==1&&CaseInfo.Active==CaseId.Office?r.Option==2:r.Option==1))return r.VoiceConsentsAfter?L("“我这项没问题。另一项去问本人。”","My part's fine. Ask them about theirs."):L("“我还没说行，别先把我印进日程。”","I haven't said yes. Don't print me into the schedule yet.");
        if(CaseInfo.Active==CaseId.Office&&r.Voice==1&&r.Option==1)return L("“我带的是草稿，不是病历。”\n那张纸又被翻回空白的一面。","I'm bringing a draft, not my medical records.\nThey turn the paper back to the blank side.");
        if(CaseInfo.Active==CaseId.Office&&r.Voice==1&&r.Option==2)return L("“草稿，时间。就这两项。\n纸够大，别急着写满。”","Draft. Time. Those two things.\nIt's a big piece of paper. No need to fill it.");
        if(r.Option==0)return r.Phase==0?L("“就这两句。信封要是鼓起来了，别怪我。”","Those two pieces. If the envelope swells up, don't blame me."):L("“那我先按你这份来。到时出入找谁？”","Then I'll work from your version. Who do I call if it doesn't match?");
        if(r.Option==1)return r.Voice==0?L("“我就是想知道，这个电话他还愿不愿意接。”","I just want to know whether they still want to take the call."):L("“安排我能确认，别的先不往上写。”","I can confirm the plan. Leave the rest off for now.");
        return r.Phase==1?L("“行，至少这回有人替我把门顶住了。”","All right. At least someone's holding the door for me this time."):L("“那把纸放这儿。电话我自己拿。”","Leave the paper here, then. I'll hold the phone myself.");
    }
    public static string Objective(RelationshipRun run)
    {
        if(run.DeckRules)return CaseDeckText.Objective(run);
        if(run.Ended)return L("V 回读长卷 / R 再跑一趟","V READ THE STRIP / R MAKE ANOTHER RUN");
        if(run.AllVoicesHeard)return L("去 03 收这一趟。还可以送遗漏的话，再回本人门前按 F 确认安排。","Finish at 03, or deliver missing words and use F at the speaker to confirm the plan again.");
        return L("第 ","ROUND ")+(run.Phase+1)+" / 3\n"+L("E 接话 / F 门前组句 / 00 休息 / 03 收工","E HEAR THEM / F COMPOSE AT A DOOR / 00 REST / 03 FINISH");
    }
    public static string Tag(RelationshipRun run,int voice,int option)
    {
        if(run.Case==CaseId.Office&&voice==1&&option==1)return L("再问私下的原因","ASK FOR PRIVATE REASONS");
        if(run.Case==CaseId.Office&&voice==1&&option==2)return L("只谈草稿和时间","DRAFT AND TIME ONLY");
        if(run.Phase==0)return option==0?L("带上这两句","TAKE THE TWO PIECES"):option==1?L("再听一句","HEAR A LITTLE MORE"):L("留在门口","LEAVE IT HERE");
        if(run.Phase==1)return option==0?L("先排上再说","PENCIL IT IN"):option==1?L("把这一项对清楚","CHECK THEIR PART"):L("替这一边开口","SPEAK FOR THIS SIDE");
        return option==0?L("签上你的名字","PUT YOUR NAME ON IT"):option==1?L("请本人确认","ASK THEM TO CONFIRM"):L("这一趟到此为止","END THIS TRIP HERE");
    }
    public static string EndingBody(RelationshipRun run,RelationshipEnding ending)
    {
        switch(ending)
        {
            case RelationshipEnding.SharedTable:return run.Case==CaseId.Home?L("六点半，她拨了电话。他把十分钟的闹钟放在手边。\n铃响的时候，她刚说到天气。他笑了一声：下周再报。","At 6:30 she calls. He puts a ten-minute timer beside him.\nIt rings just as she reaches the weather. He laughs: save the forecast for next week."):L("周五的草稿摆在桌上，评审改到了白天。\n负责人拿笔圈出下一次修改。同事把事由栏翻到背面，用来垫咖啡。","The Friday draft is on the table. The review is in daylight.\nThe manager circles the next change. Your colleague uses the back of the reason field as a coaster.");
            case RelationshipEnding.FalsePeace:return L("日程填好了，字迹全是你的。\n两扇门里几乎同时传来一句：“那就照你保证的办。”","The diary is full, all in your handwriting.\nFrom both doors, almost together: 'Then we'll go with what you promised.'");
            case RelationshipEnding.Loop:return L("你把最后一张纸放下，又听见一声：“顺便再问一句。”\n电话还在你手里。","You put down the last piece of paper. 'One more thing, while you're there.'\nYou're still holding the phone.");
            case RelationshipEnding.Alliance:return L("一扇门给你留了椅子。另一扇门不再往外递纸。\n你坐下时，才发现椅子正好挡住出口。","One door has a chair waiting for you. The other stops sending notes.\nWhen you sit down, you notice the chair is across the exit.");
            case RelationshipEnding.Exhaustion:return L("你张了张嘴，忘了下一句是替谁说的。\n纸先留在长椅上。外面有风。","You open your mouth and forget whose words come next.\nLeave the papers on the bench. There's a breeze outside.");
            case RelationshipEnding.Boundary:return L("你把剩下的纸放回各自门口。\n走到外面时，听见有人拿起了电话，还没听见拨号。","You leave the remaining papers at their own doors.\nOutside, you hear a receiver being lifted. No dialling yet.");
            case RelationshipEnding.Distance:return L("你走的时候，两扇门都没再叫住你。\n有张便签从信封里滑出来，停在门槛内侧。","Neither door calls you back as you leave.\nA note slips from an envelope and stops just inside the threshold.");
            default:return "";
        }
    }
    public static string ArchiveBody(RelationshipEnding ending)
    {
        switch(ending)
        {
            case RelationshipEnding.SharedTable:return CaseInfo.Active==CaseId.Home?L("六点半，十分钟。\n天气留到下周再报。","6:30. Ten minutes.\nThe weather can wait until next week."):L("周五草稿，白天评审。\n事由栏仍然空着。","Friday draft. Daytime review.\nThe reason field stays blank.");
            case RelationshipEnding.Boundary:return L("纸放回各自门口。\n有人拿起了电话。","The papers are back at their own doors.\nSomeone lifts a receiver.");
            case RelationshipEnding.Distance:return L("有些话还没带到。\n今天没有新的便签了。","Some words are still undelivered.\nNo more notes arrive today.");
            case RelationshipEnding.Loop:return L("“顺便再问一句。”\n电话仍在你手里。","One more thing, while you're there.\nYou're still holding the phone.");
            case RelationshipEnding.Alliance:return L("一扇门留了椅子。\n另一扇门不再递纸。","One door has a chair waiting for you.\nThe other stops sending notes.");
            case RelationshipEnding.Exhaustion:return L("下一句先不说了。\n纸留在长椅上。","Leave the next sentence unsaid.\nThe papers can stay on the bench.");
            case RelationshipEnding.FalsePeace:return L("日程里全是你的笔迹。\n“那就照你保证的办。”","The diary is all in your handwriting.\nThen we'll go with what you promised.");
            default:return "";
        }
    }
    public static string NotebookMemory(RelationshipRun run,int voice)
    {
        var m=run.MessageFrom(voice);if(m==null)return L("还没有这人的话。","No words from this person yet.");
        string status=L("事实 ","FACT ")+((m.PublicProgress&1)!=0?"●":"○")+L(" / 安排 "," / PLAN ")+((m.PublicProgress&2)!=0?"●":"○")+(m.Assumed?L(" · 铅笔暂定"," · PENCILLED IN"):"");
        string privacy=m.HasNeed?L("私下的话已听到，详见传话夹。","Private words heard; see the envelope."):L("私下的原因没有记进来。","Private reasons aren't in these notes.");
        string answer=run.HasUnrepairedDisclosure(voice)?L("还没向本人承认擅自转述。","Sharing without permission is unacknowledged."):(voice==0?run.RedConsents:run.BlueConsents)?L("本人确认了自己的安排。","They've confirmed their own part."):L("本人还没有确认安排。","They haven't confirmed their own part.");
        return status+"\n"+privacy+"\n"+answer;
    }
}
