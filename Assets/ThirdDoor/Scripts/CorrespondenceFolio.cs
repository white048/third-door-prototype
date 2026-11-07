using UnityEngine;
using UnityEngine.UI;

public class CorrespondenceFolio : MonoBehaviour
{
    private PlayerController player;
    private DialogueUI dialogue;
    private Button[] sources,payloads,actions;
    private ExhibitionText heading,details,reason,ledger;
    private int selectedSource,revision=-1,lastContext=-3;
    private Language language;
    private MessagePayload payload;
    private CorrespondenceEvent feedback;
    private GameObject proof;
    private ExhibitionText proofRoute,proofWords,proofPermission,proofDelta;
    private Button proofConfirm,proofCancel;
    private UnityEngine.UI.RawImage proofArt;
    private int proofRevision,proofSource,proofContext,proofOpenedFrame;
    private MessagePayload proofPayload;
    private DispatchComposer composer;
    private bool packetProof;
    private DispatchFragment packetFirst,packetSecond;
    private DispatchTool packetTool;
    public bool ProofOpen=>proof!=null&&proof.activeSelf;
    public bool ComposerOpen=>composer!=null&&composer.IsOpen;
    public void ConfigureComposer(DispatchComposer value){composer=value;}
    public void CloseComposer(){if(composer!=null)composer.Close();}
    public int SelectedSource => selectedSource;
    public MessagePayload SelectedPayload => payload;
    public void Configure(PlayerController p,DialogueUI ui,Button[] speakers,Button[] fragments,Button[] controls,ExhibitionText title,ExhibitionText description,ExhibitionText why,ExhibitionText events)
    {
        player=p;dialogue=ui;sources=speakers;payloads=fragments;actions=controls;heading=title;details=description;reason=why;ledger=events;
        for(int i=0;i<2;i++){int voice=i;sources[i].onClick.AddListener(()=>SelectSource(voice));int fragment=i;payloads[i].onClick.AddListener(()=>SelectPayload((MessagePayload)fragment));}
        for(int i=0;i<4;i++){int action=i;actions[i].onClick.AddListener(()=>Act((MessageAction)action));}
    }
    public static int NearbyVoice(PlayerController p)
    {
        if(p==null||p.PlayerCamera==null)return -1;
        var camera=p.PlayerCamera;
        if(!Physics.SphereCast(camera.transform.position,.28f,camera.transform.forward,out RaycastHit hit,5.5f,~0,QueryTriggerInteraction.Collide))return -1;
        var door=hit.collider.GetComponentInParent<DoorDialogue>();
        if(door==null)return -1;
        return door.Actor==DialogueActor.RedDoor?0:door.Actor==DialogueActor.BlueDoor?1:-1;
    }
    public void FocusDoor(int voice)
    {
        var run=GameStateManager.Instance.Run;
        if(voice>=0)
        {
            var incoming=run.MessageFrom(1-voice);
            selectedSource=incoming!=null&&(incoming.Status==MessageStatus.Held||incoming.Assumed||incoming.TaskActive)?1-voice:voice;
        }
        else if(run.MessageFrom(selectedSource)==null&&run.Messages.Count>0)selectedSource=run.Messages[0].Source;
        payload=MessagePayload.Request;feedback=null;revision=-1;
    }
    public void SelectSource(int voice){selectedSource=voice;payload=MessagePayload.Request;feedback=null;Refresh();}
    public void SelectPayload(MessagePayload value){payload=value;feedback=null;Refresh();}
    public void Act(MessageAction action)
    {
        if(dialogue.IsDialogueOpen||IntroSequence.IsActive||ExhibitionHUD.Paused||!NarrativeNotebook.Open)return;
        var state=GameStateManager.Instance;var item=state.Run.MessageFrom(selectedSource);if(item==null)return;
        action=MappedAction(action,state.Run,item);
        if(action==MessageAction.Deliver)
        {
            if(state.Run.DispatchRules)
            {if(!ProofOpen&&state.Run.CanDraft(item.Id,NearbyVoice(player)))composer.Show(selectedSource);return;}
            if(proof==null||ProofOpen||!state.Run.CanActOnMessage(item.Id,action,payload,NearbyVoice(player)))return;
            packetProof=false;
            proofRevision=state.Run.Revision;proofSource=selectedSource;proofPayload=payload;proofContext=NearbyVoice(player);
            proofOpenedFrame=Time.frameCount;proof.SetActive(true);RefreshProof();return;
        }
        Commit(action);
    }
    private void Commit(MessageAction action)
    {
        if(dialogue.IsDialogueOpen||IntroSequence.IsActive||ExhibitionHUD.Paused||!NarrativeNotebook.Open)return;
        var state=GameStateManager.Instance;var item=state.Run.MessageFrom(selectedSource);if(item==null)return;
        var result=state.Run.ActOnMessage(item.Id,action,payload,NearbyVoice(player));if(result==null)return;
        feedback=result;state.RefreshAfterCorrespondence();AudioManager.Instance?.Play(GameSound.ChoiceClick,.3f);Refresh();
    }
    private static MessageAction MappedAction(MessageAction action,RelationshipRun run,MessageEnvelope item)
    {
        if(run.DeckRules&&action==MessageAction.AskPermission)return MessageAction.CheckFacts;
        if(action==MessageAction.Repair&&run.DeckRules&&item.TaskActive)return MessageAction.HandOffTask;
        if(action==MessageAction.Repair&&run.DispatchRules&&!run.HasUnrepairedDisclosure(item.Source))return MessageAction.Reconfirm;
        return action;
    }
    public void ConfigureProof(GameObject panel,ExhibitionText route,ExhibitionText words,ExhibitionText permission,ExhibitionText delta,Button confirm,Button cancel,RawImage art)
    {
        proof=panel;proofRoute=route;proofWords=words;proofPermission=permission;proofDelta=delta;proofConfirm=confirm;proofCancel=cancel;proofArt=art;
        confirm.onClick.AddListener(ConfirmProof);cancel.onClick.AddListener(CloseProof);proof.SetActive(false);
    }
    public void CloseProof(){if(proof!=null)proof.SetActive(false);}
    public void OpenPacketProof(int source,DispatchFragment first,DispatchFragment second,DispatchTool tool)
    {
        var run=GameStateManager.Instance.Run;var m=run.MessageFrom(source);
        if(ProofOpen||m==null||!run.CanSendPacket(m.Id,first,second,tool,NearbyVoice(player)))return;
        selectedSource=source;proofRevision=run.Revision;proofSource=source;proofPayload=payload;proofContext=NearbyVoice(player);
        packetProof=true;packetFirst=first;packetSecond=second;packetTool=tool;proofOpenedFrame=Time.frameCount;proof.SetActive(true);RefreshProof();
    }
    public void ConfirmProof()
    {
        if(!ProofOpen)return;
        var run=GameStateManager.Instance.Run;
        bool same=run.Revision==proofRevision&&selectedSource==proofSource&&payload==proofPayload&&NearbyVoice(player)==proofContext;
        CloseProof();if(!same)return;
        if(packetProof)
        {
            var m=run.MessageFrom(proofSource);var result=m==null?null:run.SendPacket(m.Id,packetFirst,packetSecond,packetTool,proofContext);
            if(result==null)return;
            feedback=result;GameStateManager.Instance.RefreshAfterCorrespondence();AudioManager.Instance?.Play(GameSound.ChoiceClick,.3f);Refresh();composer.Resolved(result);
        }
        else Commit(MessageAction.Deliver);
    }
    private void RefreshProof()
    {
        if(!ProofOpen)return;
        var run=GameStateManager.Instance.Run;var item=run.MessageFrom(selectedSource);
        var preview=item==null?null:packetProof?run.PreviewPacket(item.Id,packetFirst,packetSecond,packetTool,NearbyVoice(player)):run.PreviewMessage(item.Id,MessageAction.Deliver,payload,NearbyVoice(player));
        if(preview==null||run.Revision!=proofRevision||selectedSource!=proofSource||payload!=proofPayload||NearbyVoice(player)!=proofContext){CloseProof();return;}
        bool privateWords=packetProof?(packetFirst==DispatchFragment.Need||packetSecond==DispatchFragment.Need):payload==MessagePayload.Need;bool breach=privateWords&&item.Permission!=MessagePermission.Granted;
        proofRoute.text=CorrespondenceText.Source(item.Source)+" → "+CorrespondenceText.Source(1-item.Source)+NarrativeText.L(" / 传话校样"," / DELIVERY PROOF");
        proofWords.text=NarrativeText.L("你将转告：\n\n","YOU WILL SAY:\n\n")+(packetProof?DispatchText.Packet(run.Case,item.Source,packetFirst,packetSecond,packetTool,run.Phase,preview.ConsentRenewed):privateWords?CorrespondenceText.Need(item):CorrespondenceText.Request(item));
        proofPermission.text=breach?CorrespondenceText.Permission(item)+"\n"+NarrativeText.L("对方没有同意转述这句话。先前若答应过直接谈，说出去后也会撤回同意。","The speaker hasn't agreed to sharing this. Any earlier agreement to talk will be withdrawn."):privateWords?CorrespondenceText.Permission(item):NarrativeText.L("这句是请你转告的原话。\n私下听到的内容不会一并带出。","This is the request you were asked to carry.\nThe private part stays out.");
        if(run.DeckRules&&packetFirst==DispatchFragment.None)proofPermission.text=NarrativeText.L("这是核对、回执或交接动作。不会替任何人作出同意。","This checks a record, returns a receipt or hands back a task. It does not give anyone's consent.");
        if(packetProof&&packetTool==DispatchTool.Handoff&&preview.ConsentRenewed)proofPermission.text=NarrativeText.L("交回任务时，再问收信人自己的那一项。\n另一人的安排仍须另外询问本人。","While handing back the task, ask the recipient about their own part again.\nAsk the other person separately about theirs.");
        if(packetProof&&(packetFirst==DispatchFragment.Guess||packetSecond==DispatchFragment.Guess))proofPermission.text=(privateWords?proofPermission.text+"\n":"")+NarrativeText.L("这句保证是你补的，没有向本人核对。","You added this promise. The speaker hasn't confirmed it.");
        proofDelta.text=packetProof?DispatchText.ToolDetail(packetTool):CorrespondenceText.Delta(preview);
        proofConfirm.GetComponentInChildren<ExhibitionText>().text=breach?NarrativeText.L("仍要转告私下内容","SHARE THE PRIVATE WORDS ANYWAY"):NarrativeText.L("说出这句话","SAY THESE WORDS");
        proofCancel.GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("返回改选 / ESC","CHANGE SELECTION / ESC");
        var texture=IllustrationLibrary.Load("ObjectCutouts-v7");proofArt.texture=texture;proofArt.gameObject.SetActive(texture!=null);
        proofArt.uvRect=privateWords?new Rect(2/3f,2/3f,1/3f,1/3f):new Rect(0,2/3f,1/3f,1/3f);
    }
    private void Update()
    {
        var state=GameStateManager.Instance;if(state==null)return;
        int context=NearbyVoice(player);
        if(revision!=state.Run.Revision||language!=LocalizationManager.Current||lastContext!=context)Refresh();
        if(ProofOpen&&Time.frameCount>proofOpenedFrame&&(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)))ConfirmProof();
    }
    private static void Style(Button button,bool selected)
    {
        var colors=button.colors;colors.normalColor=selected?ExhibitionDesign.Red:new Color(.84f,.82f,.75f);colors.highlightedColor=Color.Lerp(colors.normalColor,ExhibitionDesign.Red,.3f);colors.selectedColor=colors.normalColor;colors.disabledColor=new Color(.76f,.75f,.7f);button.colors=colors;
        button.GetComponentInChildren<ExhibitionText>().color=selected?ExhibitionDesign.Paper:ExhibitionDesign.Ink;
    }
    public void Refresh()
    {
        var state=GameStateManager.Instance;if(state==null)return;var run=state.Run;
        int context=NearbyVoice(player);revision=run.Revision;language=LocalizationManager.Current;lastContext=context;
        for(int voice=0;voice<2;voice++)
        {
            var envelope=run.MessageFrom(voice);sources[voice].interactable=envelope!=null;
            sources[voice].GetComponentInChildren<ExhibitionText>().text=NarrativeText.Speaker(voice)+" → "+CorrespondenceText.Source(1-voice)+"\n\n"+(envelope==null?NarrativeText.L("还没有这人的话。先去门前听听。","No message yet. Go and listen at this door first."):CorrespondenceText.Status(envelope)+"\n"+CorrespondenceText.Permission(envelope));
            Style(sources[voice],selectedSource==voice&&envelope!=null);
        }
        var item=run.MessageFrom(selectedSource);
        if(feedback==null&&item!=null&&item.Status!=MessageStatus.Held)
            for(int i=run.Correspondence.Count-1;i>=0;i--)if(run.Correspondence[i].MessageId==item.Id){feedback=run.Correspondence[i];break;}
        heading.text=item==null?NarrativeText.L("先听原话，再决定转告什么。","LISTEN, THEN CHOOSE WHAT TO PASS ON."):CorrespondenceText.Source(item.Source)+" → "+CorrespondenceText.Source(1-item.Source)+"  /  "+CorrespondenceText.Status(item);
        payloads[0].GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("01 / 要转告的原话\n\n","01 / THE REQUEST\n\n")+(item==null?NarrativeText.L("听到对方的请求后，原话会记在这里。","The request appears here once you have heard it."):CorrespondenceText.Request(item));
        payloads[1].GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("02 / 私下听见的需要\n\n","02 / THE PRIVATE NEED\n\n")+(item==null?NarrativeText.L("多了解对方后，私下的话会记在这里。","Hear more from this person to reveal their private need."):CorrespondenceText.Need(item));
        payloads[0].interactable=item!=null;payloads[1].interactable=item!=null&&item.HasNeed;
        Style(payloads[0],payload==MessagePayload.Request&&item!=null);Style(payloads[1],payload==MessagePayload.Need&&item!=null);
        for(int i=0;i<4;i++)
        {
            var action=item==null?(MessageAction)i:MappedAction((MessageAction)i,run,item);
            bool compose=run.DispatchRules&&action==MessageAction.Deliver;
            actions[i].interactable=item!=null&&!dialogue.IsDialogueOpen&&(compose?run.CanDraft(item.Id,context):run.CanActOnMessage(item.Id,action,payload,context));
            string label=i==0?NarrativeText.L("交给 ","DELIVER TO ")+(item==null?"…":CorrespondenceText.Source(1-item.Source)):CorrespondenceText.ActionName(action);
            if(compose)label=NarrativeText.L("拆开组句 → ","COMPOSE FOR → ")+(item==null?"…":CorrespondenceText.Source(1-item.Source));
            if(i==2&&item!=null&&!item.Promised)label=NarrativeText.L("保密，不转告","KEEP THIS PRIVATE");
            var actionPreview=item==null?null:run.PreviewMessage(item.Id,action,payload,context);
            if(actionPreview!=null)label+="\n<size=14>"+CompactDelta(actionPreview)+"</size>";
            actions[i].GetComponentInChildren<ExhibitionText>().text=label;
        }
        if(feedback!=null)details.text=CorrespondenceText.Outcome(feedback)+"\n"+CorrespondenceText.Delta(feedback);
        else if(item==null)details.text=NarrativeText.L("每人保留一封话，可转告原话或私下需要。\n私下内容须先问本人；是否愿意直接谈，要另外确认。","Each person has one envelope: a request and a private need.\nAsk before sharing the private part. Ask separately about talking directly.");
        else if(item.Status!=MessageStatus.Held)
            details.text=run.HasUnrepairedDisclosure(item.Source)?NarrativeText.L("这句话已经传出去了，你还没向本人承认。\n回到说话的人门前，可以说明你擅自转述了什么。","The words have been shared. You haven't acknowledged doing it without permission.\nReturn to the speaker to put that right."):NarrativeText.L("这封话已经处理，结果已记下，不能重复使用。","This envelope has been dealt with. Its result is recorded and cannot be repeated.");
        else
        {
            var preview=run.PreviewMessage(item.Id,MessageAction.Deliver,payload,context);
            details.text=CorrespondenceText.Permission(item)+"\n"+CorrespondenceText.Readiness(run,item)+"\n"+(preview==null?NarrativeText.L("到收信人的门前，可预览并交付。","Approach the recipient to preview and deliver."):CorrespondenceText.Delta(preview));
            if(payload==MessagePayload.Need&&item.Permission!=MessagePermission.Granted&&item.Status==MessageStatus.Held)details.text+="\n"+NarrativeText.L("未经本人允许：其信任 -2；需要当面承认擅自转述。","WITHOUT PERMISSION: speaker TRUST -2. Return to acknowledge what you shared.");
        }
        reason.text=dialogue.IsDialogueOpen?NarrativeText.L("先读完这段对话，再处理传话。","Finish this conversation before handling a message."):context<0?NarrativeText.L("门前 F：问说话的人许可、退回承诺；去另一人那里转告。","F AT A DOOR: ask or return at the source; deliver at the recipient."):NarrativeText.L("当前在 ","AT ")+CorrespondenceText.Source(context)+NarrativeText.L(" 门前 / 处理过的交谈不能用 G 抹去。"," / These exchanges cannot be erased with G.");
        if(run.DeckRules&&item!=null)
        {
            if(feedback==null)details.text=(item.SourceChecked?NarrativeText.L("公开版本已核对。","Public edition checked."):NarrativeText.L("可回说话人门前核对版本，不需要核对卡。","You can check the source's edition without a VERIFY card."))+"\n"+DispatchText.Progress(item)+(item.TaskActive?NarrativeText.L("\n代办清单还在你名下；送达后可交接。","\nYour list is still under your name. Hand it back after delivery."):"");
            payloads[1].GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("私事不在工作清单里\n\n这一趟只处理草稿、时间、通知版本和交接。","PRIVATE REASONS STAY OFF THE LIST\n\nThis trip covers the draft, time, notice edition and handoff.");
            reason.text=NarrativeText.L("F：原主处核对；收信人处组句、交接。03 收这一场。","F: check at the source; compose or hand back at the recipient. 03 closes this scene.");
        }
        ledger.text=NarrativeText.L("在手 ","HELD ")+run.HeldMessages+" / 2\n";
        int first=Mathf.Max(0,run.Correspondence.Count-3);
        for(int i=first;i<run.Correspondence.Count;i++)
        {
            var entry=run.Correspondence[i];ledger.text+=CorrespondenceText.Source(entry.Source)+" / "+CorrespondenceText.ActionName(entry.Action)+(entry.Breach?NarrativeText.L(" · 擅自转述"," · PRIVATE WORDS SHARED"):"")+"\n";
        }
        RefreshProof();
    }
    private static string CompactDelta(CorrespondenceEvent result)
    {
        string value="";
        if(result.CapacityDelta!=0)value+=NarrativeText.L("余力 ","BREATH ")+NarrativeText.Signed(result.CapacityDelta)+" / ";
        if(result.LoadDelta!=0)value+=NarrativeText.L("承重 ","LOAD ")+NarrativeText.Signed(result.LoadDelta)+" / ";
        if(result.BridgeDelta!=0)value+=NarrativeText.L("直连 ","BRIDGE ")+NarrativeText.Signed(result.BridgeDelta)+" / ";
        if(result.SourceTrustDelta!=0)value+=CorrespondenceText.Source(result.Source)+NarrativeText.L("信任 "," TRUST ")+NarrativeText.Signed(result.SourceTrustDelta)+" / ";
        if(result.RecipientTrustDelta!=0)value+=CorrespondenceText.Source(1-result.Source)+NarrativeText.L("信任 "," TRUST ")+NarrativeText.Signed(result.RecipientTrustDelta)+" / ";
        return value.Length==0?NarrativeText.L("保密，资源不变","KEPT PRIVATE; NO RESOURCE CHANGE"):value.Substring(0,value.Length-3);
    }
}
