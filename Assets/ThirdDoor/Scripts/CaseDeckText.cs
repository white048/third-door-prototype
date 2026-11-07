using static NarrativeText;

public static class CaseDeckText
{
    public static bool Active=>GameStateManager.Instance!=null&&GameStateManager.Instance.Run.DeckRules;
    public static bool Tuned=>GameStateManager.Instance!=null&&GameStateManager.Instance.Run.TunedDeck;
    public static string Phase(int phase)=>phase==0?L("先报安排","THE FIRST PLAN"):phase==1?L("你拿的是哪一版","WHICH EDITION?"):L("下一步谁来做","WHO TAKES IT FROM HERE?");
    public static string Name(DispatchTool tool)=>tool==DispatchTool.Check?L("核对","VERIFY"):tool==DispatchTool.Receipt?L("回执","RECEIPT"):tool==DispatchTool.Handoff?L("交接","HANDOFF"):tool==DispatchTool.TakeOver?L("代办","DO A TASK"):DispatchText.ToolNameLegacy(tool);
    public static string Detail(DispatchTool tool)
    {
        if(Tuned)
        {
            if(tool==DispatchTool.Pause)return L("恢复 2 余力，接话长度不变。接着翻译，才会留出两段工夫。","Restore 2 BREATH. Same hearing window. Follow with REFRAME to make room for two pieces.");
            if(tool==DispatchTool.Receipt)return L("凭真实回执返还本次余力，接话长度不变。对面已送齐时也核对来源。","Refund this attempt with a real receipt. Same hearing window. A full opposite receipt checks the source.");
            if(tool==DispatchTool.TakeOver)return L("整理清单，承重 +3。未核对时多花 1 余力。跨过打断，送达后须交接。","Compile the list: LOAD +3. Unchecked: 1 extra BREATH. Bypass interruptions; hand it back later.");
            if(tool==DispatchTool.Handoff)return L("送达后空格使用：承重 -3。对方已等着确认的那一项，也一并再问。","After delivery: LOAD -3. Use empty slots. Also ask any pending eligible confirmation from the recipient.");
        }
        switch(tool)
        {
            case DispatchTool.Quote:return L("原话两段都能听完；已核对时，不增加承重。需要了解说话人。","Hear both pieces, even through doubt. A checked source avoids extra LOAD. Know the source first.");
            case DispatchTool.Pause:return L("恢复 2 余力，留两段工夫。为这封话下一次翻译铺垫。","Restore 2 BREATH. Room for two pieces. Set up this envelope's next REFRAME.");
            case DispatchTool.Listen:return L("先问要听什么，再说两段。不受开头顺序限制。","Ask what matters first. Hear two pieces in either order.");
            case DispatchTool.Reframe:return L("承重 -1，收信人信任 +1。接在停顿后，省 1 余力并说完两段。","Ease LOAD and doubt. After PAUSE: save 1 BREATH and make room for both pieces.");
            case DispatchTool.Boundary:return L("承重 -2，留两段工夫。需要先了解收信人。","LOAD -2. Room for two pieces. Know the recipient first.");
            case DispatchTool.TakeOver:return L("多花 1 余力整理清单，承重 +3。跨过打断与版本要求，送达后须交接。","Spend 1 extra BREATH compiling the list. LOAD +3. Bypass order and edition checks; hand it back later.");
            case DispatchTool.Check:return L("核对这一封的版本，增加对说话人的了解。可空着两格使用。","Check this edition and learn the source. Works with an empty packet.");
            case DispatchTool.Receipt:return L("带回真实回执，返还本次接话余力。另一封已送达时，也能核对来源。","Use an actual receipt; refund this attempt's BREATH. A full opposite receipt checks the source.");
            case DispatchTool.Handoff:return L("代办清单送达后，归还这件事：承重 -3。两格留空即可。","After delivering your list, return the task. LOAD -3. Leave both slots empty.");
            default:return L("每次接话消耗 1 余力。先后顺序、版本和对方的工夫都会影响结果。","Each attempt costs 1 BREATH. Order, edition and the recipient's time all matter.");
        }
    }
    public static string Preset(int id)=>id==0?L("留证 / 核对与回执","PAPER TRAIL"):id==1?L("周旋 / 留出接话工夫","MAKE ROOM"):L("承接 / 代办与交接","LEND A HAND");
    public static string Plan(int mask)
    {
        System.Func<DispatchTool,bool> has=t=>(mask&(1<<((int)t-1)))!=0;
        string text=L("这副手牌可以怎样配合\n\n","PLANS IN THIS HAND\n\n");
        if(has(DispatchTool.Quote))text+=(has(DispatchTool.Check)?L("核对 → 原话","VERIFY → QUOTE"):L("基本核对 → 原话","BASIC CHECK → QUOTE"))+L("\n核对后免额外承重。\n","\nChecked words avoid extra load.\n");
        if(has(DispatchTool.Pause)&&has(DispatchTool.Reframe))text+=L("停顿 → 翻译\n同一封话省余力，留两段工夫。\n","PAUSE → REFRAME\nSave breath on this envelope.\n");
        if(has(DispatchTool.TakeOver))text+=(has(DispatchTool.Handoff)?L("代办 → 交接","TASK → HANDOFF"):L("代办 → 基本交接","TASK → BASIC HANDOFF"))+L("\n清单送到后，把任务交回。\n","\nDeliver, then return the task.\n");
        if(has(DispatchTool.Receipt))text+=Tuned?L("真实回执\n回程省余力，仍须留意接话长度。\n","ACTUAL RECEIPT\nSave breath; check the hearing window.\n"):L("真实回执\n对面已送到，回程可省余力。\n","ACTUAL RECEIPT\nDelivered words help the return trip.\n");
        if(!has(DispatchTool.Quote)&&!has(DispatchTool.TakeOver)&&!has(DispatchTool.Receipt)&&!(has(DispatchTool.Pause)&&has(DispatchTool.Reframe)))text+=L("单张工具也可应急。\n基本核对与交接不用牌。","Single tools can cover a gap.\nBasic checking and handoff need no card.");
        return text;
    }
    public static string Fragment(int phase,int source,DispatchFragment part,DispatchTool tool)
    {
        if(part==DispatchFragment.None)return "";
        if(part==DispatchFragment.Need)return L("私人原因不在这份清单里。","Private reasons aren't on this list.");
        if(part==DispatchFragment.Guess)return tool==DispatchTool.Reframe?L("这句还没核对：能不能先按这份安排试一次？","This isn't confirmed: could we try this arrangement once?"):source==0?L("负责人保证，这次不会再改任何安排。","The manager guarantees there will be no more changes."):L("同事保证，以后的安排都由他配合。","Your colleague guarantees they'll fit every future arrangement.");
        if(phase==0)return part==DispatchFragment.Fact?(source==0?L("草稿周五下午三点前要到。","The draft is due before 3 pm on Friday."):L("同事周五一点能上传草稿。","Your colleague can upload the draft at 1 pm on Friday.")):(source==0?L("请报一个白天能参加的评审时间。","Please propose a daytime review you can attend."):L("同事提议两点半评审，只谈草稿。","Your colleague proposes a 2:30 review, about the draft only."));
        if(phase==1)return part==DispatchFragment.Fact?(source==0?L("新通知用 B 会议室，旧的 A 室单已作废。","The new notice says room B. The old room A slip is void."):L("同事手里的新版也写 B 会议室。","Your colleague's new copy also says room B.")):(source==0?L("请按新版确认两点半的评审。","Please confirm the 2:30 review using the new notice."):L("同事想在回信里标明这是新版。","Your colleague asks you to label the reply as the new edition."));
        return part==DispatchFragment.Fact?(source==0?L("B 会议室已预留到两点半。","Room B is reserved for 2:30."):L("一点上传草稿，两点半能到。","The draft goes up at 1. They can attend at 2:30.")):(source==0?L("负责人发正式邀请，同事负责上传草稿。","The manager sends the invitation; your colleague uploads the draft."):L("后续变更请负责人直接通知，同事自己确认。","The manager should send later changes directly; your colleague will confirm them."));
    }
    public static string Packet(int phase,int source,DispatchFragment first,DispatchFragment second,DispatchTool tool,bool confirmationQuestion=false)
    {
        if(first==DispatchFragment.None&&tool==DispatchTool.Handoff&&confirmationQuestion)return L("清单已经送到，我把这件事交回。\n刚才留待确认的安排，你自己的那一项，现在可以确认吗？","The list has arrived. I'm handing the task back.\nAbout the plan we left open: can you confirm your own part now?");
        if(first==DispatchFragment.None)return tool==DispatchTool.Check?L("我先把刚才记的版本核对一遍，别把两张通知混在一起。","Let me check the edition I noted down. I don't want to mix up the two notices."):tool==DispatchTool.Handoff?L("这张清单已经送到。接下来由你们各自确认，我把这部分交回。","The list has arrived. You can each confirm your own part. I'm handing this task back."):tool==DispatchTool.Receipt?L("我带回刚才实际听到的回执。这里只记已听到的内容，不替本人答应。","Here's the receipt for what was actually heard. It records the words, without answering for anyone."):L("你现在最急着听哪一项？我先按这个顺序来。","Which part do you need first? I'll start there.");
        string text=Fragment(phase,source,first,tool);if(second!=DispatchFragment.None)text+="\n"+Fragment(phase,source,second,tool);
        string prefix=tool==DispatchTool.TakeOver?L("我先把这份安排整理成清单：\n","I'll compile these arrangements into a list:\n"):tool==DispatchTool.Receipt?L("接着刚才的实际回执，这一边说的是：\n","Following the actual receipt, this side says:\n"):tool==DispatchTool.Quote?L("按记录里的原话：\n","As recorded:\n"):tool==DispatchTool.Pause?L("等一等，给这两句留点工夫：\n","Give me a moment for these pieces:\n"):tool==DispatchTool.Boundary?L("这趟我只带这几句：\n","This trip, I'm carrying these words only:\n"):"";
        return prefix+text;
    }
    public static string Actor(RelationshipRun r,int source)
    {
        if(r.Phase==1)return L("“你拿的是 A 室那张，还是 B 室这张？先把版本对上。纸可不会自己认错。”","Is that the room A slip or the room B one? Check the edition first. Paper never admits it's wrong.");
        if(r.Phase==2)return source==0?L("“电梯快到了，先说一件。别把后面的责任也顺手塞进来。”","The lift's nearly here. One thing first. Don't slip another job in after it."):L("“我手里的笔还在。先说能不能到，再说谁发邀请。”","I've still got my pen. First, can they make it? Then tell me who sends the invitation.");
        return source==0?L("“先说怎么排。期限我已经贴在屏幕上了，再贴就看不见草稿了。”","Start with the arrangement. The deadline's on my screen. One more note and I won't see the draft."):L("“先报能交的时间。我的日程暂时不收形容词。”","Start with when it can be delivered. My diary isn't accepting adjectives today.");
    }
    public static string Incoming(int phase,int voice,int receipt)
    {
        if(phase==0)return voice==0?L("“周五三点前要草稿。评审放白天，让他报个时间。\n我的咖啡可以隔夜，草稿不行。”","Draft before three on Friday. Daytime review. Ask them for a time.\nMy coffee can wait overnight. The draft can't."):L("“一点交草稿，两点半评审可以。只谈草稿。\n原因栏空着，它不影响上传速度。”","Draft at one. A review at 2:30 works. About the draft only.\nLeave the reason field blank. It won't make the upload faster.");
        if(phase==1)return voice==0?L("“刚换了 B 会议室，A 室那张作废。时间没变。\n这回先核对版本，别让旧纸带着大家跑。”","We've moved to room B. The room A slip is void. Same time.\nCheck the edition first. Don't let an old piece of paper lead the meeting."):L("“我也拿到 B 室的通知了。回信写明新版。\n抽屉里两张纸都说自己是最终版。”","I've got the room B notice too. Label the reply as the new edition.\nBoth papers in my drawer insist they're the final one.");
        return voice==0?L("“B 室留好了，我发邀请，他上传草稿。\n我赶下一场。清单若由你先整理，记得把后续交回来。”","Room B is held. I send the invite; they upload the draft.\nI'm due elsewhere. If you compile the list, hand the follow-up back."):L("“一点上传，两点半能到。变更让负责人直接发，我自己确认。\n这次不用第三个人的名字当订书钉。”","Upload at one. I can make 2:30. Have the manager send changes directly; I'll confirm.\nNo need to staple another person's name to it.");
    }
    public static string Choice(RelationshipRun r,int voice,int option)=>ChoiceAt(r.Phase,voice,option);
    public static string ChoiceAt(int phase,int voice,int option)
    {
        if(phase==2)return voice==1?(option==0?L("这一份也先算我来担保。","Put this one under my name for now."):option==1?L("原因再补一句，也许就能定。","One more reason might settle it."):L("你确认自己的安排，下一步你自己接。","Confirm your part and take the next step yourself.")):(option==0?L("最后也先用我的名字定下来。","Settle this last part under my name."):option==1?L("这一项你来确认，接下来谁来做也写清楚。","Will you confirm this part, including who does what next?"):L("这一趟我先停在这里。","I'll stop this trip here."));
        if(voice==1)return option==0?L("两项我带过去，先不加理由。","I'll carry those two items. No extra reasons."):option==1?L("为什么不能晚上开？给我个解释。","Why not meet in the evening? Give me an explanation."):L("核对草稿、时间和版本，其他不写。","Check the draft, time and edition. Leave the rest off.");
        return option==0?L("先接下这两项，再去另一边。","I'll take these two items to the other side."):option==1?L("我再核对一遍，免得带错版本。","Let me check it again, so I carry the right edition."):L("这趟先不替你接。","I'll leave this trip with you for now.");
    }
    public static string Feedback(ConversationRecord e)
    {
        if(e.TooExhaustedToListen)return L("“等你坐下再核对吧，这页我先留着。”","Sit down before you check it. I'll keep this page here.");
        if(e.Phase==2&&(e.Voice==0?e.Option==1:e.Option==2))return e.VoiceConsentsAfter?L("“我这一格可以。另一格去问本人，别替他签。”","My box is fine. Ask them about theirs; don't sign it for them."):L("“另一边的话还没对上。先别把我印进日程。”","The other side's words aren't settled. Don't print me into the diary yet.");
        if(e.Voice==1&&e.Option==1)return L("“我带的是草稿，不是原因申请表。”","I'm bringing a draft, not an application to have a reason.");
        if((e.Voice==0&&e.Option==1)||(e.Voice==1&&e.Option==2))return L("“对，就是这份。你把版本记在边上，免得它又长出一张兄弟。”","Yes, this one. Note the edition in the margin before it grows a twin.");
        return e.Option==0?L("“就这两项。信封鼓起来了我可不认。”","Those two things. If the envelope swells up, that's yours."):L("“行，纸先留这儿。”","All right. Leave the paper here.");
    }
    public static string Combo(DispatchCombo combo)=>combo==DispatchCombo.CheckedQuote?L("核对 → 原话：免去额外承重","VERIFY → QUOTE: no extra load"):combo==DispatchCombo.PacedReframe?L("停顿 → 翻译：省余力，两段听完","PAUSE → REFRAME: breath saved, both pieces heard"):combo==DispatchCombo.ReceiptChain?L("实际回执：返还本次接话余力","ACTUAL RECEIPT: attempt breath refunded"):combo==DispatchCombo.Handoff?L("代办 → 交接：责任已归还","TASK → HANDOFF: the job is returned"):"";
    public static string Reaction(CorrespondenceEvent e)
    {
        if(e.CheckMissing)return L("“先别往下读。这是新通知吗？把来源核对了再来，原话卡也替不了这一步。”","Don't read on yet. Is this the new notice? Check the source first. QUOTE can't do that for you.");
        if(e.Combo==DispatchCombo.Handoff)return L("“清单留这儿。下一版我自己发。”\n你手里少了一件待办。","Leave the list here. I'll send the next edition myself.\nOne less task in your hands.")+(e.ConsentRenewed?L("\n你再问了一次，对方确认了自己的那一项。","\nYou ask again. They confirm their own part."):"");
        if(e.First==DispatchFragment.None&&e.Tool==DispatchTool.Check)return L("你把刚才听到的版本重新对了一遍，在角上记了日期。","You check the edition you heard and put the date in the corner.");
        if(e.First==DispatchFragment.None&&e.Tool==DispatchTool.Receipt)return L("“这项对方听到了，我记下。答应没答应，另问本人。”","I'll note that they heard this. Ask them separately whether they agree.");
        string text=e.Completed?(e.Phase==0?L("“一点的稿，两点半的评审，记下了。”","Draft at one. Review at 2:30. Got it."):e.Phase==1?L("“B 室，新版。旧纸可以翻过来垫杯子了。”","Room B, new edition. The old slip can be a coaster now."):L("“谁上传，谁发邀请，这次各有一格。”","One box for the upload, one for the invitation. A name in each.")):DispatchText.ReactionLegacy(e);
        if(e.Completed&&e.Tool==DispatchTool.TakeOver)text+="\n"+L("你整理的清单还在自己名下，记得交接。","The list you compiled is still under your name. Hand it back.");
        if(e.Combo!=DispatchCombo.None)text+="\n"+Combo(e.Combo);
        return text;
    }
    public static string Objective(RelationshipRun r)
    {
        if(r.Ended)return L("V 回看这趟 / R 再组一副牌","V REVISIT THIS RUN / R BUILD ANOTHER HAND");
        if(!r.DeckReady)return L("工作案例 ","WORK CASE ")+(r.Phase+1)+" / 3\n"+(r.Phase==0?L("E 门前选五张牌，再接话。Tab 也可查看。","E AT A DOOR: choose five cards, then listen. TAB also opens the hand."):L("下一场可换一张牌。E 门前打开牌组。","Swap up to one card for the next scene. E at a door opens your hand."));
        if(r.Phase==2&&r.BothPlansVerified&&!r.CurrentTasks)return r.RedConsents&&r.BlueConsents?L("两边确认了自己的安排。去 03 收工，V 可回看这趟。","Both confirmed their own part. Finish at 03; V revisits this run."):L("两份原话已到。回本人门前按 F，分别再问一次安排。","Both messages arrived. F at each speaker's door to ask for their own confirmation.");
        if(r.RedDone&&r.BlueDone)return r.CurrentTasks?L("清单送到后还要交接：F / 交接卡或交接按钮。","After the list arrives, hand it back: F / HANDOFF card or task button."):r.Phase<2&&r.CanAdvanceEncounter?L("两边都对上了。去 03，进入下一场。","Both sides match. Go to 03 for the next scene."):L("F 补送缺的项；最后还须各自确认。03 可收这一趟。","F to deliver missing items; each person still confirms their own part. Finish at 03.");
        return L("第 ","SCENE ")+(r.Phase+1)+" / 3 · "+Phase(r.Phase)+"\n"+L("E 接话 / F 组句 / 00 休息 / Tab 查看五张手牌","E LISTEN / F COMPOSE / 00 REST / TAB YOUR FIVE CARDS");
    }
    public static string FinalLine(RelationshipRun r)=>r.Phase<2?(r.CanAdvanceEncounter?L("这一场的两份话已送到，代办也已交接。\n收起旧纸，下一场会有新的条件。","Both messages arrived and your tasks are handed back.\nPut the old papers away. The next scene has a new condition."):L("这一场还没对齐。\n先听两边，把公开两项送到；代办清单也要交接。你也可以离开。","This scene isn't settled.\nHear both sides, deliver both public items and hand back any task. You can also leave.")):L("最后一版在手里。谁上传，谁发邀请，各自确认。\n还有哪件事留在你名下，也可以现在交接。","The final edition is here. Each person confirms who uploads and who sends the invite.\nYou can still hand back a task that's under your name.");
    public static string FinalChoice(RelationshipRun r,int option)=>option==0?(r.Phase<2?L("回门前，继续这一场。","Return to the doors; continue this scene."):L("先照当前这份安排收工。","Finish with the arrangement as it stands.")):option==1?(r.Phase<2?L("收这一场，进入下一场。","Close this scene and start the next."):L("确认各自安排；未齐的先回去补。","Confirm each part; return for anything missing.")):L("这趟先停，我先离开。","Stop this trip here. I'll leave for now.");
    public static string Hand(RelationshipRun r)
    {
        if(!r.DeckReady)return L("先在门前按 E 选五张牌。\n下一场可换一张。","E at a door to choose five cards.\nSwap up to one next scene.");
        string text="";for(int i=1;i<=9;i++)if(r.InDeck((DispatchTool)i))text+=Name((DispatchTool)i)+"  ×"+r.DeckStock((DispatchTool)i)+"\n";
        return text+L("\nF 组句时出牌。\n基本核对与交接不用牌。","\nPlay cards while composing with F.\nBasic checking and handoff need no card.");
    }
    public static string Memory(RelationshipRun r,int voice)
    {
        var m=r.MessageFrom(voice);if(m==null)return L("这一场还没接到这人的话。","No message from them in this scene yet.");
        return L("事实 ","FACT ")+((m.PublicProgress&1)!=0?"●":"○")+L(" / 安排 "," / PLAN ")+((m.PublicProgress&2)!=0?"●":"○")+(m.Assumed?L(" · 暂定"," · PENCILLED"):"")+"\n"+(m.SourceChecked?L("版本已核对","EDITION CHECKED"):L("版本未核对","EDITION UNCHECKED"))+(m.TaskActive?L(" · 清单待交接"," · TASK STILL YOURS"):m.TaskHandedOff?L(" · 清单已交接"," · TASK HANDED BACK"):"")+"\n"+((voice==0?r.RedConsents:r.BlueConsents)?L("本人确认了自己的那一项。","They confirmed their own part."):L("本人还没确认安排。","They haven't confirmed their part."));
    }
    public static string Ending(RelationshipRun r)
    {
        if(r.Ending==RelationshipEnding.SharedTable)return L("旧通知翻到了背面，B 会议室的回执夹在新版清单上。\n谁上传草稿，谁发邀请，各自签在自己的那一格。\n你把空信封折好。再有改动，电话会直接拨过去。","The old notice is turned over. Room B's receipt is clipped to the new list.\nOne name for the upload, another for the invitation. Each signs their own box.\nYou fold the empty envelope. The next change gets a direct call.");
        if(r.Ending==RelationshipEnding.FalsePeace)return L("清单已经排好了，来源那格却仍是你的名字。\n“那就照你保证的办。”两边都这样回了你。","The list is scheduled, but your name is still in the source box.\nThen we'll go with what you promised. Both sides send the same reply.");
        return DispatchText.EndingBody(r,r.Ending);
    }
}
