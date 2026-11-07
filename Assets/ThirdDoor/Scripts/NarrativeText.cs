using System;

public static class NarrativeText
{
    public static string L(string cn, string en) => LocalizationManager.Current == Language.Chinese ? cn : en;
    public static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
    public static string PhaseName(int phase) => CaseDeckText.Active?CaseDeckText.Phase(phase):CaseInfo.Active==CaseId.Office?OfficeNarrative.Phase(phase):phase == 0 ? L("借来的声音", "BORROWED VOICES") : phase == 1 ? L("被改写的句子", "WORDS IN TRANSIT") : L("谁来开口", "WHO SPEAKS NEXT");
    public static string Speaker(int voice) => CaseInfo.Active==CaseId.Office?OfficeNarrative.Speaker(voice):voice == 0 ? L("01 / 牵挂", "01 / THE WEIGHT") : L("02 / 距离", "02 / THE DISTANCE");

    private static string DisclosureLine()=>L("“那句话，我没让你告诉别人。\n这次能不能先问问我？”","I didn't say you could tell them that.\nCould you ask me first this time?");
    public static bool DispatchActive=>GameStateManager.Instance!=null&&GameStateManager.Instance.Run.DispatchRules;
    public static string Line(RelationshipRun run, int voice) => run.DeckRules?CaseDeckText.Incoming(run.Phase,voice,run.ReceiptState(voice)):run.DispatchRules?DispatchText.Incoming(run.Case,run.Phase,voice,run.ReceiptState(voice),run.HasUnrepairedDisclosure(voice)):run.HasUnrepairedDisclosure(voice)?DisclosureLine():LineAt(run.Phase,voice,run.Trust(voice),run.ManagedMessages,run.Alliances);
    public static string RecordedLine(ConversationRecord record) => record.DeckRules?CaseDeckText.Incoming(record.Phase,record.Voice,record.PriorReceipt):record.DispatchRules?DispatchText.Incoming(CaseInfo.Active,record.Phase,record.Voice,record.PriorReceipt,record.PriorDisclosure):record.PriorDisclosure?DisclosureLine():LineAt(record.Phase,record.Voice,record.VoiceTrustBefore,record.PriorManagedMessages,record.PriorAlliances);
    public static string LineAt(int phase,int voice,int trust=1,int managed=0,int alliances=0)
    {
        if(CaseInfo.Active==CaseId.Office)return OfficeNarrative.Line(phase,voice,trust,managed,alliances);
        if(phase==1&&trust==0)return voice==0?L("“我都不太敢跟你说了。\n你这次是来听我的，还是替他讲道理？”","I'm not sure I can tell you things anymore.\nAre you here to listen, or to take his side?"):L("“你真的想听我说吗？\n别听到一半，又替她来劝我。”","Do you actually want to hear me?\nPlease don't turn this into another talk on her behalf.");
        if(phase==2&&trust>=3&&(managed>0||alliances>0))return L("“还是你来安排吧。\n你替我们说，总比我们一开口就吵强。”","Why don't you arrange it?\nYou can say it for us. We always end up arguing.");
        if(phase==2&&trust>=3)return L("“我还是有点怕。不过，我愿意试试。\n你在旁边听着就好，让我们自己说。”","I'm still nervous, but I'll try.\nYou can stay and listen. Let us do the talking.");
        if (phase == 0)
            return voice == 0
                ? L("“你帮我跟他说说，别总躲着我。\n我问问他怎么了，也有错吗？”", "Ask him to stop shutting me out.\nI ask how he's doing, and he pulls away.")
                : L("“你跟她说，我想自己做决定。\n我只是想喘口气，她就觉得我不要她了。”", "Tell her I want to make my own decisions.\nI ask for some space, and she thinks I don't want her around.");
        if (phase == 1)
            return voice == 0
                ? L("“你跟他说了没有？他怎么说？\n你帮我说软一点，他兴许听得进去。”", "Did you tell him? What did he say?\nMaybe he'll listen if you put it more gently.")
                : L("“她又让你来劝我了？\n你知道我不是那个意思。你会帮我的吧？”", "Did she send you to talk me round again?\nYou know what I mean. You'll back me up, won't you?");
        return voice == 0
            ? L("“我去说了，他还是不理我怎么办？\n你能不能保证，这回别再闹僵了？”", "What if I talk to him and he still won't listen?\nCan you promise we won't fall out again?")
            : L("“我说了，她照样生气怎么办？\n要不你先帮我应付一下？”", "What if I tell her what I want and she gets angry anyway?\nCould you deal with her for me first?");
    }

    public static string Choice(RelationshipRun run, int voice, int option) => run.DeckRules?CaseDeckText.Choice(run,voice,option):run.DispatchRules?DispatchText.Choice(run,voice,option):run.Case==CaseId.Office?OfficeNarrative.Choice(run.Phase,voice,option,run.HasUnrepairedDisclosure(voice)):run.Phase==2&&option==1&&run.HasUnrepairedDisclosure(voice)
        ? L("我不该擅自转述。你还愿意自己开口吗？","I shouldn't have shared that. Will you still speak for yourself?")
        : ChoiceAt(run.Phase,voice,option,run.ManagedMessages,run.Alliances);
    public static string RecordReply(ConversationRecord record) => record.DeckRules?CaseDeckText.ChoiceAt(record.Phase,record.Voice,record.Option):record.DispatchRules?DispatchText.ChoiceAt(CaseInfo.Active,record.Phase,record.Voice,record.Option):CaseInfo.Active==CaseId.Office?OfficeNarrative.Choice(record.Phase,record.Voice,record.Option,record.PriorDisclosure):record.Phase==2&&record.Option==1&&record.PriorDisclosure?L("我不该擅自转述。你还愿意自己开口吗？","I shouldn't have shared that. Will you still speak for yourself?"):ChoiceAt(record.Phase,record.Voice,record.Option,record.PriorManagedMessages,record.PriorAlliances);
    private static string ChoiceAt(int phase,int voice,int option,int managed,int alliances)
    {
        if (phase == 0)
        {
            if (option == 0) return voice == 0 ? L("我会把话带给他。", "I'll carry your words to him.") : L("我会替你挡住她。", "I'll keep her away from you.");
            if (option == 1) return voice == 0 ? L("你这么着急，是在担心什么？", "What are you so worried about?") : L("你想和她断了联系，还是想自己拿主意？", "Do you want to cut ties, or just make your own choices?");
            return L("我现在没法帮你传话。", "I can't pass this on right now.");
        }
        if (phase == 1)
        {
            if (option == 0) return voice == 0 ? L("我会替你润色，让他听不出责备。", "I'll smooth it out so he won't hear the blame.") : L("我会告诉她你没事，让她先放心。", "I'll tell her you're fine so she stops worrying.");
            if (option == 1) return voice == 0 ? L("先不说他该怎么做。你自己想要什么？", "Leave what he should do aside for a moment. What do you want?") : L("我明白你想自己决定。我不替你评判她。", "I hear that you want your own choices. I won't judge her for you.");
            return voice == 0 ? L("是他太冷漠了。我站在你这边。", "He's being cold. I'm on your side.") : L("是她管得太多。我站在你这边。", "She's controlling. I'm on your side.");
        }
        if (option == 0) return L("我来安排。我保证你们不会争吵。", "I'll manage it. I promise you won't fight.");
        if (option == 1)
        {
            if(managed>0)return L("我先把我改过的话说清楚。剩下的，你愿意自己说吗？","I'll explain what I changed. Would you say the rest yourself?");
            if(alliances>0)return L("我不该替你站队。你愿意自己去跟对方说吗？","I shouldn't have taken sides. Would you speak to them yourself?");
            return L("你愿意自己说想要什么，也听对方把话说完吗？", "Would you tell them what you want, and hear them out too?");
        }
        return L("我能听你说。接下来怎么相处，得由你们自己决定。", "I can listen. You'll have to decide how to get along yourselves.");
    }

    public static string Feedback(ConversationRecord record)
    {
        if(record.DeckRules)return CaseDeckText.Feedback(record);
        if(record.DispatchRules)return DispatchText.Feedback(record)+CardAddition(record);
        if(CaseInfo.Active==CaseId.Office)return OfficeNarrative.Feedback(record)+CardAddition(record);
        if (record.TooExhaustedToListen) return L("你还想问，已经没力气听下去了。\n这次没能多了解对方。\n去 00 长椅歇歇，或者先说清自己能帮到哪里。", "You try to ask more, but you're too worn out to listen.\nYou learn nothing new this time.\nRest at bench 00, or say how much you can manage.")+CardAddition(record);
        int phase = record.Phase, option = record.Option, voice = record.Voice;
        string text;
        if (phase == 0)
        {
            if (option == 0) text = L("对方等着你把话带过去。\n你接下了这件事，自己的负担也重了。", "They're waiting for you to pass it on.\nYou've taken the job, and more of the weight.");
            else if (option == 1) text = voice == 0
                ? L("“我怕他长大了，真的就不再需要我了。”\n她总算说出了自己担心的事。", "I'm scared he won't need me when he's grown up.\nShe finally says what's been worrying her.")
                : L("“我还是想跟她来往。可总得让我自己做决定吧。”", "I still want her in my life. I just need to make my own decisions.");
            else text = L("对方没接话。\n你也没有继续解释，给自己留了点力气。", "They don't answer.\nYou leave it there and save some strength.");
        }
        else if (phase == 1)
        {
            if (option == 0) text = L("你把话说得好听了些。\n对方暂时放心了，却不知道你改了哪里。", "You make it sound easier to accept.\nThey seem reassured. They don't know what you've changed.");
            else if (option == 1) text = voice == 0
                ? L("“我就是想知道，他心里还有没有我。”", "I just want to know I still have a place in his life.")
                : L("“我想自己拿主意，也想和她好好来往。”\n你听着，没有答应替他站队。", "I want to make my own choices and still get along with her.\nYou listen without agreeing to take his side.");
            else text = L("“还是你懂我。”\n对方更亲近你了，另一人却更不信任你。", "At least you get it.\nThey trust you more. The other person trusts you less.");
        }
        else
        {
            if (option == 0) text = L("他们等着你安排。\n既然保证了不吵，往后的麻烦也得由你接着。", "They leave the arrangements to you.\nYou promised there wouldn't be an argument. Now it's yours to prevent.");
            else if (option == 1) text = record.VoiceConsentsAfter
                ? L("“我愿意试试。对方肯不肯，还得你去问。”\n这一人已经答应，另一人仍要单独确认。", "I'll try. You'll have to ask whether they're willing too.\nThis person has agreed. The other still needs to answer.")
                : L("“我还没准备好。先别把我算进去。”\n这次邀请没有得到同意。", "I'm not ready for this. Don't count me in yet.\nThey haven't accepted the invitation.");
            else text = L("你说清了自己能帮到哪里。\n接下来要不要谈，由他们决定。你可以先走。", "You say how much you can help.\nWhether they talk is up to them. You can leave.");
        }
        text += CardAddition(record);
        if(record.RepairedDisclosure)text+="\n"+L("你也承认了那次擅自转述，没有要求对方立刻原谅。","You also acknowledge sharing without permission, without asking for immediate forgiveness.");
        if(record.CreatedMessageId>=0)text+="\n"+L("Tab 传话夹：选原话或需要；到门前按 F 处理。","CORRESPONDENCE: TAB to inspect. F at a door to handle this message.");
        if(phase==2&&option==1&&(record.PriorManagedMessages>0||record.PriorAlliances>0))text+="\n"+L("你澄清了之前的改写或站队，不再替对方担保这件事。","You correct your earlier edit or promise to take sides. One less commitment rests on you.");
        return text;
    }
    private static string CardAddition(ConversationRecord record) => record.Card == ConversationCard.None ? "" : "\n" + L("你补上一句：", "YOU ADD: ") + CardUtterance(record.Card,record.Voice);

    public static string ChoiceTag(RelationshipRun run,int voice,int option)
    {
        if(run.DispatchRules)return DispatchText.Tag(run,voice,option);
        if(run.Case==CaseId.Office)return OfficeNarrative.Tag(run,voice,option);
        if(option==1)return run.Capacity==0&&run.SelectedCard!=ConversationCard.Pause&&run.SelectedCard!=ConversationCard.Boundary?L("余力不足，无法深入倾听","NO BREATH FOR INSIGHT"):run.Phase==2?L("征询同意 / 归还承诺","CONSENT / RETURN A PROMISE"):L("理解需要 +1","INSIGHT +1");
        if(run.Phase==0)return option==0?L("暂得信任","SHORT-TERM TRUST"):run.Clarity(voice)==0?L("划界 +1 / 信任暂降","BOUNDARY +1 / TRUST FALLS"):L("划界 +1","BOUNDARY +1");
        if(run.Phase==1)return option==0?L("原话被改写","EDITED WORDS"):L("站队 / 另一方信任下降","ALLEGIANCE / OTHER TRUST FALLS");
        return option==0?L("结果由你担保","YOU GUARANTEE THE OUTCOME"):L("划界 +1","BOUNDARY +1");
    }

    public static string Delta(int capacity, int load, int bridge) => L("余力 ", "BREATH ") + Signed(capacity) + "   /   " + L("承重 ", "LOAD ") + Signed(load) + "   /   " + L("直连 ", "BRIDGE ") + Signed(bridge);
    public static string CardUtterance(ConversationCard card,int voice)
    {
        if(CaseInfo.Active==CaseId.Office&&card==ConversationCard.Listen)return L("“工作上还有什么要先说清楚的？”","What else do we need to clear up about the work?");
        if(CaseInfo.Active==CaseId.Office&&card==ConversationCard.Reframe)return L("“先说你能做什么，私事先放着。”","Let's start with what you can do. Leave the private part aside.");
        switch(card)
        {
            case ConversationCard.Pause:return L("“等一下，让我想想怎么说。”","Give me a moment to think.");
            case ConversationCard.Quote:return L("“我按你原来的意思说，不改。”","I'll keep your original meaning.");
            case ConversationCard.Boundary:return L("“我只能帮这一次，接下来请你亲口说。”","I can help this once. After that, please speak for yourself.");
            case ConversationCard.Listen:return L("“还有什么是我没听明白的？”","Is there something I've missed?");
            case ConversationCard.Reframe:return L("“先说‘我需要什么’，你会怎么讲？”","How would you say it starting with 'I need'?");
            case ConversationCard.TakeOver:return L("“如果谈崩了，所有后果都由我来负责。”","If it goes wrong, I'll be responsible for all of it.");
            default:return "";
        }
    }
    public static string ShortAction(ConversationRecord record)
    {
        if(CaseInfo.Active==CaseId.Office)return OfficeNarrative.ShortAction(record);
        if(record.Phase==0)return record.Option==0?L("接过传话","CARRIED THE MESSAGE"):record.Option==1?L("听见需要","ASKED ABOUT THE NEED"):L("说出自己的界限","NAMED A LIMIT");
        if(record.Phase==1)return record.Option==0?L("替对方润色","EDITED THE MESSAGE"):record.Option==1?L("留下真实需要","KEPT THE NEED"):L("接受站队的邀请","TOOK A SIDE");
        return record.Option==0?L("保证对话结果","PROMISED AN OUTCOME"):record.Option==1?L("询问直接对话的同意","ASKED FOR CONSENT"):L("把责任还回去","RETURNED RESPONSIBILITY");
    }
    public static string CardName(ConversationCard card)
    {
        switch (card)
        {
            case ConversationCard.Pause: return L("停顿", "PAUSE");
            case ConversationCard.Quote: return L("原话", "QUOTE");
            case ConversationCard.Boundary: return L("界限", "BOUNDARY");
            case ConversationCard.Listen: return L("追问", "LISTEN");
            case ConversationCard.Reframe: return L("翻译", "REFRAME");
            case ConversationCard.TakeOver: return L("代偿", "TAKE OVER");
            case ConversationCard.Rewind: return L("重述", "SAY IT AGAIN");
            default: return L("不出牌", "NO CARD");
        }
    }
    public static string CardDescription(ConversationCard card)
    {
        switch (card)
        {
            case ConversationCard.Pause: return L("想一想再答。\n余力 +2 / 承重 -1\n每局一张，随本次回应使用。", "Take a moment before answering.\nBREATH +2 / LOAD -1\nOne per run. Use with a response.");
            case ConversationCard.Quote: return L("复述时保留对方原意。\n直连 +1 / 承重 +1\n先了解另一人；私下内容仍须先问。", "Keep their original meaning.\nBRIDGE +1 / LOAD +1\nHear the other person first. Ask before sharing private needs.");
            case ConversationCard.Boundary: return L("说清自己能帮到哪里。\n余力 +1 / 承重 -2\n记下一次主动划界。", "Say how much you can help.\nBREATH +1 / LOAD -2\nAdds one personal boundary.");
            case ConversationCard.Listen: return L("再问一句，听对方说完。\n余力 -1 / 直连 +1\n对这个人的理解增加 1。", "Ask another question and listen.\nBREATH -1 / BRIDGE +1\nAdds 1 insight into this person.");
            case ConversationCard.Reframe: return L("把责备改成对方自己的诉求。\n余力 -1 / 承重 -1 / 直连 +1\n须已了解此人；信任 -1。", "Rephrase blame as what they need.\nBREATH -1 / LOAD -1 / BRIDGE +1\nRequires insight. TRUST -1.");
            case ConversationCard.TakeOver: return L("“这件事我来负责。”\n双方信任 +1 / 余力 -2 / 承重 +2\n多接下一份保证结果的责任。", "I'll take care of it.\nBOTH TRUST +1 / BREATH -2 / LOAD +2\nAdds a promise to manage the outcome.");
            case ConversationCard.Rewind: return L("G / 撤回上一句，每局一次。\n恢复回应前的资源，用过的卡不返还。\n休息、处理传话或结局后不能撤回。", "G / Revise your last response once per run.\nRestore resources, but not spent cards.\nUnavailable after rest, message actions or an ending.");
            default: return L("可以直接回应，也可以加用一张卡。\n每种卡只有一张。\nQ 切换；再点同一张可取消。", "Answer without a card, or add one.\nYou have one of each card.\nQ cycles cards. Click again to deselect.");
        }
    }

    public static string CardUnavailable(RelationshipRun run, ConversationCard card, int voice)
    {
        if (!run.Acquired(card)) return L("下一轮获得", "NEXT ROUND");
        if (run.Stock(card) == 0) return L("已用完", "SPENT");
        if (card == ConversationCard.Quote && run.Clarity(1 - voice) == 0) return L("先听另一人的需要", "HEAR OTHER NEED");
        if (card == ConversationCard.Reframe && run.Clarity(voice) == 0) return L("先理解此人的需要", "NEEDS INSIGHT FIRST");
        if ((card == ConversationCard.Listen || card == ConversationCard.Reframe || card == ConversationCard.TakeOver) && run.Capacity < 2) return L("至少需要 2 余力", "NEEDS 2 BREATH");
        return "";
    }

    public static string Objective(RelationshipRun run)
    {
        if(run.DispatchRules)return DispatchText.Objective(run);
        if(run.Case==CaseId.Office)return OfficeNarrative.Objective(run);
        if (run.Ended) return L("Tab 回看记录。R 重新开始。", "TAB to read your notes. R to play again.");
        if (run.AllVoicesHeard) return L("去 03，决定接下来怎么办。", "Go to 03 and choose what to do next.");
        if(run.Phase==0&&run.RedDone!=run.BlueDone)
        {
            int source=run.RedDone?0:1;var message=run.MessageFrom(source);
            if(message!=null&&message.Status==MessageStatus.Held)
                return L("第 1 / 3 · 借来的声音\n走向 ","ROUND 1 / 3 · BORROWED VOICES\nAT ")+(source==0?"02":"01")+L("：E 先听，F 决定怎么交付。03 可离开。",": E TO LISTEN, F TO HANDLE THE MESSAGE. 03 IS YOUR EXIT.");
        }
        string target = !run.RedDone && !run.BlueDone ? L("01 或 02", "01 OR 02") : run.RedDone ? "02" : "01";
        return L("第 ", "ROUND ") + (run.Phase + 1) + " / 3  ·  " + PhaseName(run.Phase) + "\n" + L("走向 ", "APPROACH ") + target + L("。也可随时去 03 离开。", ". You may also leave through 03.");
    }
    public static string FinalLine(RelationshipRun run) => run.DeckRules?CaseDeckText.FinalLine(run):run.Case==CaseId.Office?L("安排已经谈过了，私事仍由本人决定说不说。\n你要接着替他们处理、请他们自己谈工作，还是先走？","You've discussed the work. They still choose whether to share private reasons.\nWill you keep managing it, ask them to discuss the plan, or leave?"):run.AllVoicesHeard
        ? L("两边的话，你都听过了。\n接着传话、请他们自己谈，还是先离开？", "You've heard both sides.\nWill you keep passing messages, ask them to talk, or leave?")
        : L("你现在就可以离开。\n如果还想听，也能回去继续这一轮。", "You can leave now.\nOr go back and hear the rest of this round.");
    public static string FinalChoice(RelationshipRun run, int option) => run.DeckRules?CaseDeckText.FinalChoice(run,option):run.Case==CaseId.Office?OfficeNarrative.FinalChoice(run,option):option == 0
        ? (run.AllVoicesHeard ? L("我留下，继续替他们传话。", "I'll stay and keep carrying their words.") : L("先回到对话中。", "Return to the conversation."))
        : option == 1 ? L("问问他们，愿不愿意坐下来自己谈。", "Ask whether they're willing to sit down and talk.")
        : L("我先歇歇。这些话得由你们自己说。", "I need a break. You'll have to take this from here.");

    public static string EndingTitle(RelationshipEnding ending)
    {
        if(DispatchActive&&ending==RelationshipEnding.FalsePeace)return L("07 / 铅笔填满的日程","07 / A DIARY IN PENCIL");
        if(CaseInfo.Active==CaseId.Office)return OfficeNarrative.EndingTitle(ending);
        switch (ending)
        {
            case RelationshipEnding.SharedTable: return L("01 / 共同的桌子", "01 / A SHARED TABLE");
            case RelationshipEnding.Boundary: return L("02 / 门的这一边", "02 / THIS SIDE OF THE DOOR");
            case RelationshipEnding.Distance: return L("03 / 先有距离", "03 / ROOM TO BREATHE");
            case RelationshipEnding.Loop: return L("04 / 永远的信使", "04 / THE MESSENGER");
            case RelationshipEnding.Alliance: return L("05 / 借来的同盟", "05 / BORROWED ALLIANCE");
            case RelationshipEnding.Exhaustion: return L("06 / 耗尽的语言", "06 / WORDS WORN OUT");
            case RelationshipEnding.FalsePeace: return L("07 / 漂亮的和解", "07 / A PERFECTLY QUIET ROOM");
            default: return "";
        }
    }
    public static string EndingBody(RelationshipEnding ending)
    {
        if(CaseDeckText.Active)return CaseDeckText.Ending(GameStateManager.Instance.Run);
        if(DispatchActive)return DispatchText.EndingBody(GameStateManager.Instance.Run,ending);
        if(CaseInfo.Active==CaseId.Office)return OfficeNarrative.EndingBody(ending);
        switch (ending)
        {
            case RelationshipEnding.SharedTable: return L("他们都答应坐下来，说说自己的想法，也听对方说完。\n你可以旁听，接下来的话由他们自己说。", "They both agree to talk and hear each other out.\nYou can stay and listen. They can speak for themselves.");
            case RelationshipEnding.Boundary: return L("你说清自己能帮到哪里，先走了。\n他们要不要谈下去，不会把你留在这里。", "You say how much you can help, then leave.\nWhether they keep talking doesn't have to keep you here.");
            case RelationshipEnding.Distance: return L("有些话还没说清，你先离开了。\n以后要怎么相处，留待以后再谈。", "Some things are still unsaid. You leave for now.\nYou can decide what comes next another day.");
            case RelationshipEnding.Loop: return L("他们还在等你传话。\n下次有事，两个人大概还是会来找你。", "They're still waiting for you to pass things on.\nNext time something happens, they'll probably come to you again.");
            case RelationshipEnding.Alliance: return L("你站到一边，得到了信任。\n另一人更难相信你，下次想开口也更难了。", "Taking a side earned you trust.\nThe other person has less reason to trust you, or to speak freely next time.");
            case RelationshipEnding.Exhaustion: return L("你累得说不下去了。\n先出去歇歇，不必等他们给出答复。", "You're too tired to keep talking for them.\nStep outside and rest. You don't need to wait for an answer.");
            case RelationshipEnding.FalsePeace: return L("改过的那些话，暂时压住了争执。\n可他们仍没弄清对方的意思，再有分歧还得等你处理。", "Your edits have settled the argument for now.\nThey still don't understand each other. The next disagreement may need you again.");
            default: return "";
        }
    }
    public static string EndingText(RelationshipRun run) => EndingTitle(run.Ending) + "\n" + EndingBody(run.Ending) + (run.Ending==RelationshipEnding.SharedTable?(run.Case==CaseId.Office?L("\n桌上的事由栏，还空着。","\nThe reason field on the table is still blank."):L("\n回头看看。他们有了自己的桌子。","\nLook back. They have a table of their own.")):"") + "\n\n" + L("Tab 回看回应 / R 重新体验", "TAB TO REVISIT / R TO REPLAY");

    public static string Memory(RelationshipRun run, int voice)
    {
        if(run.DeckRules)return CaseDeckText.Memory(run,voice);
        if(run.DispatchRules)return DispatchText.NotebookMemory(run,voice);
        if(run.Case==CaseId.Office)return OfficeNarrative.Memory(run,voice);
        int clarity = run.Clarity(voice);
        if (clarity == 0) return L("你知道对方要你做什么，还没问清为什么。", "You know what they want you to do, but not why.");
        string need = voice == 0 ? L("她担心他以后不再需要自己。", "She's afraid he won't need her anymore.") : L("他想自己拿主意，也想和她保持联系。", "He wants to make his own choices and stay in touch.");
        bool consent = voice == 0 ? run.RedConsents : run.BlueConsents;
        if(run.HasUnrepairedDisclosure(voice))return need+"\n"+L("你还没向本人承认那次擅自转述。","You haven't acknowledged sharing their words without permission.");
        return need + "\n" + (consent ? L("本人已答应试着直接谈。", "Has agreed to try talking directly.") : L("本人还没答应直接谈。", "Hasn't agreed to talk directly yet."));
    }
}
