using static NarrativeText;

public static class CaseInfo
{
    public static CaseId Active => GameStateManager.Instance!=null&&!IntroSequence.IsActive?GameStateManager.Instance.Run.Case:CaseSession.Current;
    public static string Title(CaseId id)=>id==CaseId.Home?L("01 / 借来的声音","01 / BORROWED VOICES"):L("02 / 事由的空白","02 / A REASON LEFT BLANK");
    public static string Brief(CaseId id)=>id==CaseId.Home?L("家人之间 · 想靠近，也想留点空间","Family · wanting closeness and room to choose"):L("工作小牌组 · 九选五，三场传话","Work with a hand of five · three scenes");
    public static string Intro(CaseId id)=>id==CaseId.Home?L("周日六点半，一通十分钟的电话。\n两个人各有条件，都把话交给了你。\n去门前听，再按 F 拆信组句。先说哪句，由你决定。","Sunday, 6:30. A ten-minute call.\nTwo people, two sets of conditions, one messenger.\nListen at a door. Press F to compose. Choose what goes first."):L("一点上传，两点半评审，先定下白天的安排。\n后面还有新版通知和清单交接。\n门前 E 先选五张牌；接话后 F 组句，03 结束每一场。","Upload at one. Review at 2:30. Start with a daytime plan.\nA revised notice and a handoff will follow.\nE at a door: choose five cards. F composes; 03 closes each scene.");
}
