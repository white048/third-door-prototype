using System.Collections;
using System.IO;
using UnityEngine;

public partial class DemoValidation
{
    private IEnumerator DispatchTalk(int voice,int option)
    {
        var door=voice==0?red:blue;yield return Approach(voice==0?new Vector3(-3.3f,.05f,1.8f):new Vector3(3.3f,.05f,1.8f),door.transform.position+Vector3.up*1.8f);
        CheckSight(door,"dispatch voice "+voice);door.Interact(player);yield return null;
        Check(ui.IsChoosing,"new-rule conversation has three playable responses");CheckTextLayout("dispatch choices");
        Click("Option "+(option+1));yield return null;Check(ui.AwaitingContinue,"new response waits for the player");
        CheckTextLayout("dispatch feedback");Click("Continue feedback");yield return null;
    }
    private IEnumerator OpenComposer(int source)
    {
        var recipient=1-source;var door=recipient==0?red:blue;
        yield return Approach(recipient==0?new Vector3(-3.3f,.05f,1.8f):new Vector3(3.3f,.05f,1.8f),door.transform.position+Vector3.up*1.8f);
        FindObjectOfType<NarrativeNotebook>().ShowCorrespondence(recipient);yield return null;Click("Envelope source / "+source);yield return null;
        Click("Message action / Deliver");yield return null;
        Check(FindObjectOfType<DispatchComposer>().IsOpen&&!player.ControlsEnabled,"real delivery button opens the ordered composer and holds world input");
    }
    private IEnumerator ComposePacket(DispatchFragment first,DispatchFragment second,DispatchTool tool,string shot)
    {
        var composer=FindObjectOfType<DispatchComposer>();var folio=FindObjectOfType<CorrespondenceFolio>();
        Click("Clear dispatch order");yield return null;
        if(first!=DispatchFragment.None)Click("Dispatch fragment / "+first);
        if(second!=DispatchFragment.None)Click("Dispatch fragment / "+second);
        Click("Dispatch tool / "+tool);yield return null;
        Check(composer.First==first&&composer.Second==second&&composer.Tool==tool,"click order becomes spoken order and retains the selected tool");
        string before=JsonUtility.ToJson(state.Run.ExportSave());
        LocalizationManager.ToggleLanguage();yield return null;CheckTextLayout("translated composer");LocalizationManager.ToggleLanguage();yield return null;
        Check(composer.First==first&&composer.Second==second&&composer.Tool==tool&&JsonUtility.ToJson(state.Run.ExportSave())==before,"translation preserves the exact draft and all model fields");
        yield return Shot(shot+"-composer");CheckTextLayout("composer");
        Click("Proof composed packet");yield return null;Check(folio.ProofOpen,"composed packet still requires a full-word proof");
        if(state.Run.TunedDeck&&state.Run.Phase==2&&tool==DispatchTool.Handoff)
            Check(GameObject.Find("Proof actual words").GetComponent<ExhibitionText>().text.Contains(NarrativeText.L("现在可以确认吗","confirm your own part now")),"handoff confirmation is an explicit spoken question in the proof");
        CheckTextLayout("packet proof");Click("Cancel dispatch proof");yield return null;
        Check(!folio.ProofOpen&&composer.IsOpen&&JsonUtility.ToJson(state.Run.ExportSave())==before,"cancelling a packet spends neither a card nor an attempt");
        Click("Proof composed packet");yield return null;yield return Shot(shot+"-proof");
        int count=state.Run.Correspondence.Count;Click("Confirm dispatch proof");yield return null;
        Check(state.Run.Correspondence.Count==count+1&&composer.IsOpen&&!folio.ProofOpen,"one confirmation records one packet and leaves the recipient reaction open");
        folio.ConfirmProof();Check(state.Run.Correspondence.Count==count+1,"stale confirmation cannot send a second packet");
        Check(!state.Run.CanRewind,"heard packet cannot be erased by revising a menu reply");
        yield return Shot(shot+"-reaction");CheckTextLayout("recipient reaction");
    }
    private IEnumerator RunDispatchDemo()
    {
        detailed=true;Check(state.Run.DispatchRules,"production new game uses the v8 dispatch rules");
        bool privacy=scenario.Contains("privacy");
        yield return RestAtBench(0);yield return DispatchTalk(0,privacy?1:0);yield return DispatchTalk(1,0);yield return RestAtBench(2);
        Check(state.Run.MessageFrom(0).HasNeed==privacy&&!state.Run.MessageFrom(1).HasNeed,"private words appear only after actually hearing them");
        bool late=scenario.Contains("late");
        if(late)
        {
            yield return DispatchTalk(0,1);yield return DispatchTalk(1,state.Run.Case==CaseId.Office?2:1);yield return RestAtBench(4);
            yield return DispatchTalk(0,1);yield return DispatchTalk(1,state.Run.Case==CaseId.Office?2:1);
            Check(!state.Run.RedConsents&&!state.Run.BlueConsents,"asking too early doesn't manufacture an agreement before delivery");
        }
        yield return OpenComposer(0);
        string untouched=JsonUtility.ToJson(state.Run.ExportSave());
        Click("Dispatch fragment / Fact");Click("Dispatch fragment / Guess");yield return null;
        Check(!GameObject.Find("Proof composed packet").GetComponent<UnityEngine.UI.Button>().IsInteractable()&&JsonUtility.ToJson(state.Run.ExportSave())==untouched,"an invented guarantee cannot be smuggled into a true packet");
        CheckTextLayout("unconfirmed mixed draft");Click("Clear dispatch order");yield return null;
        if(scenario.Contains("promise")||scenario.Contains("falsepeace"))
        {
            yield return ComposePacket(DispatchFragment.Guess,DispatchFragment.None,DispatchTool.None,"20-provisional");
            Check(state.Run.HasAssumedPlan&&!state.Run.PublicPlanVerified(0),"borrowed promise makes a provisional plan, not a verified one");
            if(scenario.Contains("falsepeace"))
            {
                FindObjectOfType<NarrativeNotebook>().Close();yield return null;
                yield return DispatchTalk(0,1);yield return DispatchTalk(1,state.Run.Case==CaseId.Office?2:1);yield return RestAtBench(4);
                yield return DispatchTalk(0,1);yield return DispatchTalk(1,state.Run.Case==CaseId.Office?2:1);
                Check(!state.Run.RedConsents&&!state.Run.BlueConsents,"a pencilled promise never counts as either person's consent");
                yield return Approach(new Vector3(0,.05f,9.5f),exit.transform.position+Vector3.up*1.8f);exit.Interact(player);yield return null;Click("Option 2");yield return new WaitForSecondsRealtime(3.1f);
                Check(state.Run.Ending==RelationshipEnding.FalsePeace,"unverified promise leads to the pencilled-diary ending");
                yield return Shot("25-provisional-ending");CheckTextLayout("provisional ending");yield return ProbeDispatchArchive();yield return ProbeRibbon("26-provisional-ending-strip");
                var restartPromise=new GameObject("Provisional restart verification").AddComponent<DemoRestartValidation>();restartPromise.Configure(output,checks);yield break;
            }
        }
        else if(privacy)
        {
            yield return ComposePacket(DispatchFragment.Need,DispatchFragment.None,DispatchTool.None,"20-private-fragment");
            Check(state.Run.HasUnrepairedDisclosure(0)&&state.Run.MessageFrom(0).PrivateHeard,"sharing the unpermitted private fragment leaves a named breach");
        }
        else
        {
            yield return ComposePacket(DispatchFragment.Fact,DispatchFragment.Request,DispatchTool.None,"20-interruption");
            Check(state.Run.MessageFrom(0).PublicProgress==0&&state.Run.MessageFrom(0).Inspected,"recipient interruption teaches order without accepting the wrong first piece");
        }
        yield return ComposePacket(DispatchFragment.Request,DispatchFragment.Fact,DispatchTool.Pause,"21-correct-order");
        Check(state.Run.PublicPlanVerified(0)&&state.Run.Stock(ConversationCard.Pause)==0,"correct ordered words verify the plan and spend the shared pause card");
        FindObjectOfType<NarrativeNotebook>().Close();yield return null;
        yield return OpenComposer(1);yield return ComposePacket(DispatchFragment.Fact,DispatchFragment.Request,DispatchTool.None,"22-other-recipient");
        if(!state.Run.PublicPlanVerified(1))
        {
            Check(state.Run.MessageFrom(1).PublicProgress==1,"short hearing retains the actual first piece");
            if(scenario.Contains("resume"))
            {
                FindObjectOfType<NarrativeNotebook>().Close();yield return null;
                var resume=new GameObject("Dispatch continuation verification").AddComponent<DemoSessionValidation>();resume.Configure(output,checks,true);yield break;
            }
            yield return ComposePacket(DispatchFragment.Request,DispatchFragment.None,DispatchTool.None,"23-follow-up");
        }
        Check(state.Run.BothPlansVerified&&state.Run.HasUnrepairedDisclosure(0)==privacy&&!state.Run.HasUnrepairedDisclosure(1),"delivery preserves the actual privacy history");
        string expected=JsonUtility.ToJson(state.Run.ExportSave());RelationshipRun loaded;
        Check(CheckpointStore.TryLoad(out loaded)&&JsonUtility.ToJson(loaded.ExportSave())==expected,"native autosave keeps partial attempts, actual receipts and spent cards");
        FindObjectOfType<NarrativeNotebook>().Close();yield return null;
        yield return ProbeRibbon("24-dispatch-strip");
        if(late)
        {
            for(int voice=0;voice<2;voice++)
            {
                var door=voice==0?red:blue;
                yield return Approach(voice==0?new Vector3(-3.3f,.05f,1.8f):new Vector3(3.3f,.05f,1.8f),door.transform.position+Vector3.up*1.8f);
                FindObjectOfType<NarrativeNotebook>().ShowCorrespondence(voice);yield return null;Click("Envelope source / "+voice);yield return null;
                Check(state.Run.CanReconfirmPlan(voice),"late delivery allows a fresh individual confirmation");Click("Message action / Repair");yield return null;
                Check(voice==0?state.Run.RedConsents:state.Run.BlueConsents,"real confirmation button records this person's answer once");
                Check(!state.Run.CanReconfirmPlan(voice),"confirmation cannot be farmed");yield return Shot("24b-late-confirmation-"+voice);CheckTextLayout("late confirmation");FindObjectOfType<NarrativeNotebook>().Close();yield return null;
            }
        }
        else
        {
            bool publicOnly=scenario.Contains("public-only");
            yield return DispatchTalk(0,publicOnly?0:1);yield return DispatchTalk(1,publicOnly?0:state.Run.Case==CaseId.Office?2:1);yield return RestAtBench(4);
            Check(!state.Run.CanEquip(ConversationCard.Pause,0),"a card used in dispatch is unavailable in a later conversation");
            yield return DispatchTalk(0,1);yield return DispatchTalk(1,state.Run.Case==CaseId.Office?2:1);
        }
        Check(state.Run.RedConsents&&state.Run.BlueConsents,"each person confirms their own part after the actual exchange");
        if(scenario.Contains("public-only"))Check(!state.Run.MessageFrom(0).HasNeed&&!state.Run.MessageFrom(1).HasNeed,"agreement is playable without ever revealing either private need");
        Check(!state.Run.HasUnrepairedDisclosure(0)&&!state.Run.HasUnrepairedDisclosure(1),"consent is reached only after outstanding disclosure is acknowledged");
        yield return Approach(new Vector3(0,.05f,9.5f),exit.transform.position+Vector3.up*1.8f);exit.Interact(player);yield return null;
        Check(ui.IsFinalDecision,"exit opens an independent final decision");Click("Option 2");yield return new WaitForSecondsRealtime(3.1f);
        Check(state.Run.Ending==RelationshipEnding.SharedTable&&ui.EndingVisible,"concrete public exchange reaches an agreement");
        yield return Shot("25-dispatch-ending");CheckTextLayout("new ending");yield return ProbeDispatchArchive();yield return ProbeRibbon("26-dispatch-ending-strip");
        var restart=new GameObject("Dispatch restart verification").AddComponent<DemoRestartValidation>();restart.Configure(output,checks);
    }
    private IEnumerator ProbeDispatchArchive()
    {
        string before=JsonUtility.ToJson(state.Run.ExportSave());
        // Seed only the in-memory archive cache. Record deliberately does not write during QA.
        // This is a layout fixture, not a claim that this player journey reached seven endings.
        var cache=(int[])typeof(EndingArchive).GetField("masks",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
        int slot=(int)state.Run.Case,previous=cache[slot];cache[slot]=127;
        Check(EndingArchive.IsValidationRun&&EndingArchive.Count==7,"isolated archive fixture actually reveals all seven endings");
        var book=FindObjectOfType<NarrativeNotebook>();book.Show(true);yield return null;yield return new WaitForEndOfFrame();
        yield return Shot("27-all-ending-summaries");CheckTextLayout("all seven new ending summaries");
        LocalizationManager.ToggleLanguage();yield return null;CheckTextLayout("translated ending summaries");LocalizationManager.ToggleLanguage();yield return null;
        Check(JsonUtility.ToJson(state.Run.ExportSave())==before,"reading all ending summaries cannot change this run");cache[slot]=previous;book.Close();yield return null;
    }
}
