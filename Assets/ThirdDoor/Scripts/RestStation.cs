using UnityEngine;

public class RestStation : Interactable
{
    public override string PromptText => NarrativeText.L("[ E ]  坐下休息 / 每轮恢复一次","[ E ]  REST / ONCE EACH ROUND");
    public override bool CanInteract(PlayerController player) => GameStateManager.Instance!=null&&!GameStateManager.Instance.Run.Ended;
    public override void Interact(PlayerController player)
    {
        var state=GameStateManager.Instance;if(state==null||state.Run.Ended)return;
        int breath=state.Run.Capacity,load=state.Run.Load;
        bool restored=state.Rest();
        int restoredBreath=state.Run.Capacity-breath,restoredLoad=state.Run.Load-load;
        FindObjectOfType<DialogueUI>().ShowLocalizedReflection(()=>NarrativeText.L("00 / 留给自己","00 / A PLACE FOR YOU"),()=>restored?NarrativeText.L("你坐了一会儿，缓过些劲来。\n","You sit for a while and catch your breath.\n")+NarrativeText.Delta(restoredBreath,restoredLoad,0):state.Run.RestUsedThisRound?NarrativeText.L("这一轮已经休息过，不会再次恢复资源。\n想多坐一会儿也可以。","You've already rested this round, so your resources won't change.\nYou can stay a little longer if you like."):NarrativeText.L("你现在余力充足，也没有承重。\n这次先不使用休息机会。","Your breath is full and you're carrying no load.\nThis won't use your rest for the round."));
        AudioManager.Instance?.Play(GameSound.ChoiceClick,.2f);
    }
}
