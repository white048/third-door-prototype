using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoValidation
{
    private IEnumerator DeckApproach(int voice)
    {
        var door=voice==0?red:voice==1?blue:exit;
        var at=voice==0?new Vector3(-3.3f,.05f,1.8f):voice==1?new Vector3(3.3f,.05f,1.8f):new Vector3(0,.05f,9.5f);
        yield return Approach(at,door.transform.position+Vector3.up*1.8f);
        CheckSight(door,"deck scene door "+voice);
    }
    private IEnumerator DeckTalk(int voice,int option,int preset)
    {
        yield return DeckApproach(voice);var door=voice==0?red:blue;bool prepare=!state.Run.DeckReady;
        string before=JsonUtility.ToJson(state.Run.ExportSave());door.Interact(player);yield return null;yield return new WaitForEndOfFrame();
        if(prepare)
        {
            var panel=FindObjectOfType<CaseDeckPanel>();Check(panel!=null&&panel.IsOpen&&NarrativeNotebook.Open&&!player.ControlsEnabled,"E at a door opens loadout preparation before the conversation");
            Check(JsonUtility.ToJson(state.Run.ExportSave())==before,"opening the loadout spends nothing");
            if(state.Run.Phase==0){Click("Deck preset / "+preset);yield return null;}
            else if(scenario.Contains("swap")&&state.Run.Phase==1)
            {
                Click("Deck choice / Pause");Click("Deck choice / Listen");yield return null;
                Check(RelationshipRun.BitCount(panel.DraftMask^state.Run.DeckMask)==2,"between scenes the player replaces one selected card");
            }
            int draft=panel.DraftMask;LocalizationManager.ToggleLanguage();yield return null;CheckTextLayout("translated case deck");LocalizationManager.ToggleLanguage();yield return null;
            Check(panel.DraftMask==draft&&JsonUtility.ToJson(state.Run.ExportSave())==before,"translation keeps the draft without preparing or refilling it");
            CheckTextLayout("case deck");yield return Shot("30-deck-scene-"+state.Run.Phase);
            Click("Confirm case deck");yield return null;
            Check(state.Run.DeckReady&&!NarrativeNotebook.Open&&ui.IsChoosing,"confirming five cards saves the hand and opens the addressed speaker");
            Check(RelationshipRun.BitCount(state.Run.DeckMask)==5,"exactly five strategy cards are equipped");
            if(scenario.Contains("swap")&&state.Run.Phase==1)Check(state.Run.InDeck(DispatchTool.Listen)&&!state.Run.InDeck(DispatchTool.Pause),"the chosen replacement is used by the new scene");
        }
        Check(ui.IsChoosing,"deck character offers real responses");CheckTextLayout("deck dialogue");
        Click("Option "+(option+1));yield return null;yield return new WaitForEndOfFrame();Check(ui.AwaitingContinue,"character feedback waits for the reader");CheckTextLayout("deck character response");Click("Continue feedback");yield return null;
    }
    private IEnumerator DeckOpen(int source)
    {yield return DeckApproach(1-source);FindObjectOfType<NarrativeNotebook>().ShowCorrespondence(1-source);yield return null;Click("Envelope source / "+source);yield return null;Click("Message action / Deliver");yield return null;Check(FindObjectOfType<DispatchComposer>().IsOpen,"current envelope opens the five-card composer");}
    private IEnumerator DeckBasic(int source,MessageAction action)
    {
        int at=action==MessageAction.HandOffTask?1-source:source;
        yield return DeckApproach(at);var book=FindObjectOfType<NarrativeNotebook>();book.ShowCorrespondence(at);yield return null;Click("Envelope source / "+source);yield return null;
        var m=state.Run.MessageFrom(source);int count=state.Run.Correspondence.Count;
        Check(state.Run.CanActOnMessage(m.Id,action,MessagePayload.Request,at),"basic action available without a matching strategy card: "+action);
        Click("Message action / "+(action==MessageAction.CheckFacts?MessageAction.AskPermission:MessageAction.Repair));yield return null;
        Check(state.Run.Correspondence.Count==count+1&&state.Run.Correspondence[count].Action==action,"contextual button resolves the actual basic action: "+action);
        CheckTextLayout("deck basic action");book.Close();yield return null;
    }
    private IEnumerator DeckReadHand()
    {
        var book=FindObjectOfType<NarrativeNotebook>();string before=JsonUtility.ToJson(state.Run.ExportSave());book.Show(false);yield return null;
        CheckTextLayout("notes before the collage");Click("Rewind response");yield return null;
        Check(FindObjectOfType<CaseDeckPanel>().IsOpen&&!GameObject.Find("Deck choice / Quote").GetComponent<Button>().IsInteractable(),"prepared hand is read-only during a scene");
        Check(JsonUtility.ToJson(state.Run.ExportSave())==before,"viewing a prepared hand cannot refund a card");yield return Shot("34-spent-hand-"+state.Run.Phase);book.Close();yield return null;
    }
    private IEnumerator DeckNext(int phase)
    {
        yield return DeckApproach(2);exit.Interact(player);yield return null;Check(ui.IsFinalDecision,"03 offers scene closure");CheckTextLayout("deck scene closure");yield return Shot("35-scene-closure-"+phase);
        Click("Option 2");yield return null;
        Check(state.Run.Phase==phase+1&&!state.Run.Ended&&!state.Run.DeckReady,"closure advances only after both plans and tasks are complete");
        Check(state.Run.Messages.Count==(phase+1)*2,"previous envelopes remain attached to their actual scene");
        RelationshipRun loaded;Check(CheckpointStore.TryLoad(out loaded)&&JsonUtility.ToJson(loaded.ExportSave())==JsonUtility.ToJson(state.Run.ExportSave()),"scene break autosaves the pending one-card swap");
    }
    private IEnumerator RunCaseDeckDemo()
    {
        detailed=true;Check(state.Run.Case==CaseId.Office&&state.Run.DeckRules&&!state.Run.DeckReady,"production work case starts the new deck campaign");
        bool basic=scenario.Contains("basic");int preset=scenario.Contains("room")||scenario.Contains("resume")?1:scenario.Contains("task")?2:0;
        for(int phase=0;phase<3;phase++)
        {
            yield return RestAtBench(phase*2);
            if(basic&&phase==2)
            {
                yield return DeckTalk(1,2,preset);yield return DeckOpen(1);
                yield return ComposePacket(DispatchFragment.Fact,DispatchFragment.Request,DispatchTool.None,"31-basic-first");
                if(!state.Run.PublicPlanVerified(1))yield return ComposePacket(DispatchFragment.Request,DispatchFragment.None,DispatchTool.None,"32-basic-first-follow");
                FindObjectOfType<NarrativeNotebook>().Close();yield return null;yield return DeckTalk(0,1,preset);
                Check(state.Run.RedConsents,"a person can confirm after the actual incoming words arrive");
            }
            else
            {yield return DeckTalk(0,basic&&phase==1?1:phase==2?1:0,preset);yield return DeckTalk(1,basic&&phase==1?2:phase==2?2:0,preset);}
            Check(state.Run.Phase==phase&&state.Run.RedDone&&state.Run.BlueDone,"dialogue alone cannot skip the scene's transmission");
            if(phase==1&&!basic)
            {
                yield return DeckOpen(0);yield return ComposePacket(DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.None,"31-unchecked-notice");
                Check(state.Run.Correspondence[state.Run.Correspondence.Count-1].CheckMissing,"new notice rejects unverified edition instead of inventing trust");FindObjectOfType<NarrativeNotebook>().Close();yield return null;
            }
            yield return DeckOpen(0);
            if(basic)
            {
                yield return ComposePacket(DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.None,"32-basic-plan");
                if(!state.Run.PublicPlanVerified(0))yield return ComposePacket(DispatchFragment.Fact,DispatchFragment.None,DispatchTool.None,"33-basic-follow");
            }
            else if(preset==0)
            {
                if(!state.Run.MessageFrom(0).SourceChecked)yield return ComposePacket(DispatchFragment.None,DispatchFragment.None,DispatchTool.Check,"32-verify");
                yield return ComposePacket(DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.Quote,"33-checked-quote");
                Check(state.Run.Correspondence[state.Run.Correspondence.Count-1].Combo==DispatchCombo.CheckedQuote,"verified source activates the quote combination");
            }
            else if(preset==1)
            {
                if(!state.Run.MessageFrom(0).SourceChecked)yield return ComposePacket(DispatchFragment.None,DispatchFragment.None,DispatchTool.Check,"32-verify");
                yield return ComposePacket(DispatchFragment.Request,DispatchFragment.None,DispatchTool.Pause,"32-pause");
                if(scenario.Contains("resume")&&phase==1)
                {
                    FindObjectOfType<NarrativeNotebook>().Close();yield return null;
                    var resume=new GameObject("Case deck continue verification").AddComponent<DemoCaseDeckResumeValidation>();resume.Configure(output,checks);yield break;
                }
                yield return ComposePacket(DispatchFragment.Fact,DispatchFragment.None,DispatchTool.Reframe,"33-paced-reframe");
                Check(state.Run.Correspondence[state.Run.Correspondence.Count-1].Combo==DispatchCombo.PacedReframe,"pause state activates the later reframe combination");
            }
            else
            {
                yield return ComposePacket(DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.TakeOver,"32-own-list");
                Check(state.Run.MessageFrom(0).TaskActive&&!state.Run.CanAdvanceEncounter,"delivering a list leaves a real task in the courier's name");
                if(phase==0)
                {
                    FindObjectOfType<NarrativeNotebook>().Close();yield return null;yield return DeckBasic(0,MessageAction.HandOffTask);
                }
                else yield return ComposePacket(DispatchFragment.None,DispatchFragment.None,DispatchTool.Handoff,"33-handoff");
                Check(!state.Run.MessageFrom(0).TaskActive&&state.Run.MessageFrom(0).TaskHandedOff,"card or basic handoff actually returns the task");
            }
            FindObjectOfType<NarrativeNotebook>().Close();yield return null;
            if(!(basic&&phase==2))
            {
                if(preset==1&&!basic&&phase==1)yield return DeckBasic(1,MessageAction.CheckFacts);
                yield return DeckOpen(1);
                var t=basic?DispatchTool.None:preset==1?DispatchTool.Listen:DispatchTool.Receipt;
                yield return ComposePacket(DispatchFragment.Fact,DispatchFragment.Request,t,"33-return-message");
                if(!state.Run.PublicPlanVerified(1))yield return ComposePacket(DispatchFragment.Request,DispatchFragment.None,DispatchTool.None,"33-return-follow");
                FindObjectOfType<NarrativeNotebook>().Close();yield return null;
            }
            Check(state.Run.BothPlansVerified&&!state.Run.CurrentTasks,"both current messages are real and no task is silently dropped");
            Check(!state.Run.MessageFrom(0).HasNeed&&!state.Run.MessageFrom(1).HasNeed,"the work campaign never requires private reasons");
            yield return DeckReadHand();
            if(phase<2)yield return DeckNext(phase);
        }
        for(int source=0;source<2;source++)if(state.Run.CanReconfirmPlan(source))yield return DeckBasic(source,MessageAction.Reconfirm);
        Check(state.Run.RedConsents&&state.Run.BlueConsents,"each person confirms their own arrangement");
        if(basic)for(int i=1;i<=9;i++)if(state.Run.InDeck((DispatchTool)i))Check(state.Run.DeckStock((DispatchTool)i)==1,"basic route consumes no strategy card");
        Check(NarrativeText.RecordedLine(state.Run.History[0]).Contains(NarrativeText.L("咖啡","coffee"))&&CaseDeckText.Packet(0,0,DispatchFragment.Fact,DispatchFragment.Request,DispatchTool.None)!=CaseDeckText.Packet(1,0,DispatchFragment.Fact,DispatchFragment.Request,DispatchTool.None),"history retains the actual edition and original character scene");
        yield return DeckApproach(2);exit.Interact(player);yield return null;Click("Option 2");yield return new WaitForSecondsRealtime(3.1f);
        Check(state.Run.Ending==RelationshipEnding.SharedTable&&ui.EndingVisible,"the full three-scene deck campaign reaches its agreement");
        Check(state.Run.Messages.Count==6&&state.Run.History.Count==6,"all six envelopes and responses survive to the ending");
        yield return Shot("36-deck-ending");CheckTextLayout("deck ending");yield return ProbeDispatchArchive();yield return ProbeRibbon("37-deck-ending-strip");
        var restart=new GameObject("Case deck restart verification").AddComponent<DemoRestartValidation>();restart.Configure(output,checks);
    }
}
