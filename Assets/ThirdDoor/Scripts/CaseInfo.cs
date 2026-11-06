using static NarrativeText;

public static class CaseInfo
{
    public static CaseId Active => GameStateManager.Instance!=null&&!IntroSequence.IsActive?GameStateManager.Instance.Run.Case:CaseSession.Current;
    public static string Title(CaseId id)=>id==CaseId.Home?L("01 / 借来的声音","01 / BORROWED VOICES"):L("02 / 事由的空白","02 / A REASON LEFT BLANK");
    public static string Brief(CaseId id)=>id==CaseId.Home?L("家人之间 · 想靠近，也想留点空间","Family · wanting closeness and room to choose"):L("同事之间 · 先谈安排，私事由本人决定","At work · agree on a plan, leave private reasons private");
    public static string Intro(CaseId id)=>id==CaseId.Home?L("周日六点半，一通十分钟的电话。\n两个人各有条件，都把话交给了你。\n去门前听，再按 F 拆信组句。先说哪句，由你决定。","Sunday, 6:30. A ten-minute call.\nTwo people, two sets of conditions, one messenger.\nListen at a door. Press F to compose. Choose what goes first."):L("周五要交草稿，评审却定在晚上。\n同事想改到白天，不愿解释私事。\n去门前听，再按 F 拆信组句。对方未必让你一口气说完。","A Friday draft, but the review is in the evening.\nYour colleague wants a daytime slot and won't discuss why.\nListen, then press F to compose. They may not hear you out.");
}
