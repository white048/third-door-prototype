using UnityEngine;
using static NarrativeText;

public sealed class WorkplanStation : Interactable
{
    public static bool Focused { get; private set; }
    private bool focused;
    public override string PromptText=>L("[ E ]  读当前工作计划","[ E ]  READ THE CURRENT WORK PLAN");
    public override bool CanInteract(PlayerController player)=>CaseInfo.Active==CaseId.Office&&player!=null&&player.ControlsEnabled&&!IntroSequence.IsActive&&!ExhibitionHUD.Paused&&!NarrativeNotebook.Open;
    public override void SetFocused(bool value){base.SetFocused(value);focused=value;Focused=value;}
    private void OnDisable(){if(focused){focused=false;Focused=false;}}
    public override void Interact(PlayerController player)
    {
        if(!CanInteract(player))return;
        FindObjectOfType<DialogueUI>().ShowLocalizedReflection(()=>L("工作计划 / ","WORK PLAN / ")+CaseDeckText.Phase(GameStateManager.Instance.Run.Phase),Read);
        AudioManager.Instance?.Play(GameSound.ChoiceClick,.2f);
    }
    private string Read()
    {
        var r=GameStateManager.Instance.Run;
        if(!r.DeckRules)return L("周五交草稿，评审放在白天。\n事由栏留白，公开安排仍可确认。","Friday draft. A daytime review.\nThe reason stays blank; the work plan can still be confirmed.");
        string plan=r.Phase==0?L("周五一点交稿，两点半评审。","FRIDAY / DRAFT AT 1 / REVIEW AT 2:30."):r.Phase==1?L("B 会议室，两点半。旧 A 室单已作废。","ROOM B / 2:30. THE OLD ROOM A SLIP IS VOID."):L("同事上传草稿，负责人发邀请。","COLLEAGUE UPLOADS. MANAGER SENDS THE INVITE.");
        return plan+"\n01 "+(r.PublicPlanVerified(0)?L("已送齐","DELIVERED"):L("未送齐","PENDING"))+" / 02 "+(r.PublicPlanVerified(1)?L("已送齐","DELIVERED"):L("未送齐","PENDING"))+L(" / 待交接 "," / TASKS ")+r.CurrentTaskCount+"\n"+L("事由：__________","REASON: __________");
    }
}
