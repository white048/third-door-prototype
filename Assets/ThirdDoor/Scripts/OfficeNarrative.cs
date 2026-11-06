using static NarrativeText;

// An agreement about work does not require access to somebody's private life.
public static class OfficeNarrative
{
    public static string Phase(int phase)=>phase==0?L("空着的事由","THE BLANK REASON"):phase==1?L("可以交付的部分","WHAT CAN BE DELIVERED"):L("有限的协议","A LIMITED AGREEMENT");
    public static string Speaker(int voice)=>voice==0?L("01 / 负责人","01 / THE MANAGER"):L("02 / 同事","02 / THE COLLEAGUE");
    public static string Line(int phase,int voice,int trust,int managed,int alliances)
    {
        if(trust==0&&phase>0)return voice==1?L("“又是来问原因的？\n每次改个安排都得交代私事，那我没什么可说的了。”","Are you here to ask why again?\nIf I have to explain my private life every time, I'd rather say nothing."):L("“哪些是对方答应的，哪些是你替人保证的？\n我得知道这份安排到底算不算数。”","Which parts did they agree to, and which did you promise for them?\nI need to know whether this plan means anything.");
        if(phase==0)return voice==0?L("“草稿不能再拖了。你去问问，\n为什么连晚上的会都参加不了？”","The draft can't be late again. Go and ask\nwhy they can't even make the evening meeting."):L("“帮我说一声，评审能不能改到白天？草稿我周五给。\n原因那一栏，就先空着吧。”","Could you ask for a daytime review? I'll have the draft on Friday.\nI'd rather leave the reason blank.");
        if(phase==1)return voice==0?L("“我也不想问私事，怕的是东西又交不出来。\n这回你能保证吗？”","I don't want to pry. I'm worried the work won't turn up again.\nCan you guarantee it this time?"):L("“我能做什么，可以一条条写清楚。\n别因为我不说私事，就觉得我不负责。”","I can write down exactly what I can do.\nPlease don't call me irresponsible because I won't discuss my private life.");
        if(managed>0||alliances>0)return L("“你替我们答应了这么多。\n这份安排，有谁亲口确认过吗？”","You've agreed to a lot on our behalf.\nHas either of us actually confirmed this plan?");
        return voice==0?L("“先把对方提的安排告诉我。\n只谈工作也行，别替人作保证。”","Tell me what they've actually proposed.\nWe can stick to the work. Don't promise anything for them."):L("“工作我愿意谈，私事就算了。\n这样行吗？”","I'm happy to discuss the work. I'd like to keep my reasons private.\nCan we do that?");
    }
    public static string Choice(int phase,int voice,int option,bool disclosure=false)
    {
        if(voice==1)
        {
            if(option==0)return phase==0?L("我去替你说，一直说到他们答应。","I'll keep talking to them until they agree."):phase==1?L("我先替你答应，细节以后再补。","I'll agree for you. We'll fill in the details later."):L("我保证他们不再问了，后面都交给我。","I'll make sure they stop asking. I'll handle the rest.");
            if(option==1)return phase==0?L("到底什么私事？说清楚了，我才好帮你解释。","What's the reason? I need to know so I can explain it for you."):phase==1?L("告诉我细节，我会让他们体谅你。","Tell me the details. I'll make them understand."):L("你就再解释一句，他们肯定就答应了。","Just explain a little more. Then they'll agree.");
            if(phase==2&&disclosure)return L("我不该擅自说出去。你还愿意只谈工作吗？","I shouldn't have shared that. Will you still discuss only the work?");
            return phase==0?L("我只转告工作安排。原因你不用跟我说。","I'll pass on the work request. You don't have to tell me why."):phase==1?L("哪些是你能做到的？我们只核对这些。","What can you commit to? Let's check those parts."):L("这次只谈工作范围，你愿意亲自确认吗？","Would you agree to discuss just the scope of the work?");
        }
        if(option==0)return phase==0?L("我去问清楚，一定给你个解释。","I'll find out and get you an explanation."):phase==1?L("我替他们保证，原来的期限不会变。","I guarantee they'll meet the original deadline."):L("后面我来盯着，保证不出问题。","I'll keep an eye on everything. I promise it'll be fine.");
        if(option==1)return phase==0?L("先说要交什么、什么时候交，私事先放一边。","What needs doing, and by when? We can leave their reasons aside."):phase==1?L("我们先核对必须完成的工作，不追问私事。","Let's check the work and leave private reasons out."):disclosure?L("那句私下话，我不该说出去。我们再核对工作清单。","I shouldn't have shared that. Let's go over the work plan again."):L("你愿意按对方提的清单，只谈工作吗？","Would you discuss just the work plan they proposed?");
        return phase==1?L("他们就是不负责。我站在你这边。","They're being irresponsible. I'm on your side."):L("我能帮忙核对，结果得由各自负责。","I can help check the plan. Each of you is responsible for your part.");
    }
    public static string Tag(RelationshipRun run,int voice,int option)
    {
        if((option==1||(voice==1&&option==2))&&run.Capacity==0&&run.SelectedCard!=ConversationCard.Pause&&run.SelectedCard!=ConversationCard.Boundary)return L("余力不足，无法认真回应","NO BREATH TO FOLLOW THROUGH");
        if(voice==1)return option==2?(run.Phase==2?L("只对工作征询同意","CONSENT TO WORK SCOPE"):L("工作范围 +1 / 私事保留","SCOPE +1 / PRIVACY KEPT")):option==1?L("追问私事 / 信任下降","PRESS FOR REASONS / TRUST FALLS"):L("替别人承担结果","PROMISE FOR SOMEONE ELSE");
        if(option==1)return run.Phase==2?run.OfficePublicRequestDelivered?L("已带到公开请求 / 征询同意","PUBLIC REQUEST ARRIVED / ASK CONSENT"):L("须先按 F 交付 02 的公开请求","FIRST DELIVER 02'S PUBLIC REQUEST WITH F"):L("厘清交付 +1","DELIVERABLES +1");
        return option==0?L("结果由你担保","YOU GUARANTEE THE OUTCOME"):run.Phase==1?L("站队 / 另一方信任下降","ALLEGIANCE / OTHER TRUST FALLS"):L("划清自己的责任","NAME YOUR OWN LIMIT");
    }
    public static string Feedback(ConversationRecord r)
    {
        if(r.TooExhaustedToListen)return L("你已经累得听不进去了，这次没能问清工作范围。\n先去 00 长椅歇歇。","You're too tired to follow this through. You learn nothing new about the scope.\nRest at bench 00 first.");
        string s;
        if(r.Voice==1&&r.Option==1)s=L("“我说的是工作。原因我不想谈。”\n对方收住了话，也更不信任你了。","I'm talking about work. I don't want to get into why.\nThey pull back and trust you less.");
        else if(r.Voice==1&&r.Option==2)s=r.Phase==2?(r.VoiceConsentsAfter?L("“行。谈工作范围和交付，原因就别问了。”\n对方答应了谈工作，仍没同意公开私事。","All right. Scope and delivery. I'd rather leave the reasons out.\nThey agree to discuss the work. Private words still need permission."):L("“我还是不太放心，先别把我算进去。”\n对方还没答应。","I'm still not sure. Don't count me in yet.\nThey haven't agreed.")):L("“白天评审，周五交草稿。这些你可以转告。”\n工作安排说清了，私人原因仍没被公开。","A daytime review and a Friday draft. You can tell them that.\nYou have the work request. The private reasons stay private.");
        else if(r.Voice==0&&r.Option==1)s=r.Phase==2?(r.VoiceConsentsAfter?L("“可以，就谈这份清单。”\n负责人收到了公开请求，也答应只谈工作。","Yes. Let's go over that plan.\nThe manager has the public request and agrees to discuss only the work."):L("“我还没法答应，别先替我定下来。”\n公开请求、工作范围和本人的确认，还没有齐备。","I can't agree to this yet. Don't settle it for me.\nThe public request, a clear scope and personal agreement aren't all in place.")):L("“我要的是周五能看到草稿，再找个白天评审。”\n这次谈清了要做什么。","I need the draft on Friday and a review during the day.\nYou've clarified what work needs doing.");
        else if(r.Option==0)s=L("对方等着你把结果办妥。\n之后再有变动，也会先来找你。","They're counting on you to make it work.\nIf anything changes, they'll come to you first.");
        else s=r.Phase==1?L("你站到这一边，另一人更不信任你了。","You take this person's side. The other trusts you less."):L("你说清能帮到哪里，把结果留给当事人负责。","You say how much you can help and leave them responsible for the outcome.");
        if(r.RepairedDisclosure)s+="\n"+L("你也承认了擅自转述。对方是否原谅，还不能替人决定。","You also acknowledge sharing without permission. Forgiveness is up to them.");
        if(r.CreatedMessageId>=0)s+="\n"+L("门前 F：公开请求可以交付；私下需要仍需许可。","F AT A DOOR: carry a public request; private needs still require permission.");
        return s;
    }
    public static string Objective(RelationshipRun run)
    {
        if(run.Ended)return L("Tab 回看记录。N 切换案例。","TAB to read your notes. N to switch cases.");
        if(!run.OfficePublicRequestDelivered&&run.MessageFrom(1)!=null&&run.MessageFrom(1).Status==MessageStatus.Held)return L("02 的公开请求还没送到\n去 01 门前按 F 转告工作安排，私下的话可以留下。","02'S PUBLIC REQUEST HASN'T ARRIVED\nAt 01, press F to deliver the work request. Keep private words separate.");
        if(run.AllVoicesHeard)return L("去 03 作决定。只谈工作，也得双方分别答应。","Go to 03. Even a work-only discussion needs both people to agree.");
        return L("第 ","ROUND ")+(run.Phase+1)+" / 3 · "+Phase(run.Phase)+"\n"+L("E 倾听 / 门前 F 交付 / 00 休息 / 03 可随时离开","E LISTEN / F AT A DOOR TO DELIVER / 00 REST / 03 YOUR EXIT");
    }
    public static string FinalChoice(RelationshipRun run,int option)=>option==0?(run.AllVoicesHeard?L("我留下，继续替他们保证结果。","I'll stay and guarantee the outcome for them."):L("先回去核对工作安排。","Return to check the work arrangement.")):option==1?L("请他们自己谈工作范围，事由先空着。","Ask them to discuss the work and leave the reason blank."):L("我先走了，后面的事请各自确认。","I'll head out. Each of you will need to confirm your part.");
    public static string ShortAction(ConversationRecord r)=>r.Voice==1?(r.Option==2?L("保留事由，只核对工作","KEPT REASONS PRIVATE"):r.Option==1?L("追问私人原因","PRESSED FOR PRIVATE REASONS"):L("替别人保证结果","GUARANTEED FOR SOMEONE ELSE")):r.Option==1?L("确认公开交付","CHECKED PUBLIC DELIVERABLES"):r.Option==0?L("接下代办责任","TOOK RESPONSIBILITY"):r.Phase==1?L("接受站队邀请","TOOK A SIDE"):L("说清自己的限度","NAMED A LIMIT");
    public static string EndingTitle(RelationshipEnding e)
    {
        switch(e)
        {
            case RelationshipEnding.SharedTable:return L("01 / 有限的工作协议","01 / A LIMITED AGREEMENT");
            case RelationshipEnding.Boundary:return L("02 / 责任的边界","02 / THE EDGE OF RESPONSIBILITY");
            case RelationshipEnding.Distance:return L("03 / 暂停代办","03 / STEPPING BACK");
            case RelationshipEnding.Loop:return L("04 / 永久的协调人","04 / THE PERMANENT COORDINATOR");
            case RelationshipEnding.Alliance:return L("05 / 办公室同盟","05 / AN OFFICE ALLIANCE");
            case RelationshipEnding.Exhaustion:return L("06 / 超出承载","06 / OVER CAPACITY");
            case RelationshipEnding.FalsePeace:return L("07 / 完美的进度表","07 / THE PERFECT SCHEDULE");
            default:return "";
        }
    }
    public static string EndingBody(RelationshipEnding e)
    {
        switch(e)
        {
            case RelationshipEnding.SharedTable:return L("他们愿意自己谈周五的草稿和白天的评审。\n私人原因要不要说，仍由本人决定。","They agree to discuss the Friday draft and a daytime review.\nPrivate reasons are still theirs to share or withhold.");
            case RelationshipEnding.Boundary:return L("你说清自己能帮到哪里，先走出了房间。\n后面的安排，留给他们各自确认。","You say how much you can help and step outside.\nThey'll have to confirm the rest themselves.");
            case RelationshipEnding.Distance:return L("安排还没谈妥，你先离开了。\n余下的事，让他们准备好后再谈。","The plan is still unsettled. You leave it with them.\nThey can return to it when they're ready.");
            case RelationshipEnding.Loop:return L("改时间要找你，问进度也要找你。\n工作照常往下排，你还在等下一句要转交的话。","They come to you to change the time, then to check progress.\nWork moves on. You're still waiting for the next message to pass along.");
            case RelationshipEnding.Alliance:return L("一人把你当成了自己人，另一人更不信任你。\n下次再谈工作，得先过这一关。","One person sees you as an ally. The other trusts you less.\nThat will be there between you the next time you discuss work.");
            case RelationshipEnding.Exhaustion:return L("进度还在催，你已经没力气继续协调了。\n先出去歇歇，剩下的事可以等。","The deadlines keep coming. You're too tired to keep coordinating.\nStep outside and rest. The rest can wait.");
            case RelationshipEnding.FalsePeace:return L("进度表上写得很齐全，有些事却只有你替人答应过。\n往后要兑现这些承诺，还得靠你来回协调。","The schedule looks complete, but some promises came only from you.\nKeeping them will mean more trips between the two.");
            default:return "";
        }
    }
    public static string Memory(RelationshipRun run,int voice)
    {
        if(run.Clarity(voice)==0)return L("还没问清对方能做哪些工作。","You haven't checked what work this person can commit to.");
        string s=voice==0?L("需要周五草稿与白天评审。","Needs a Friday draft and a daytime review."):L("愿意谈工作，不想交代私人原因。","Will discuss the work, but wants to keep the reasons private.");
        if(run.HasUnrepairedDisclosure(voice))return s+"\n"+L("你还没向本人承认那次擅自转述。","You haven't acknowledged sharing their words without permission.");
        return s+"\n"+((voice==0?run.RedConsents:run.BlueConsents)?L("已同意只谈工作安排。","Has agreed to discuss only the work."):L("本人还没答应只谈工作。","Hasn't agreed to a work-only discussion yet."));
    }
}
