using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Explicit QA only. It uses the real player, real buttons and real colliders.
public partial class DemoValidation : MonoBehaviour
{
    private static bool attached;
    private string scenario,output;
    private readonly List<string> checks=new List<string>();
    private PlayerController player;
    private DialogueUI ui;
    private GameStateManager state;
    private DoorDialogue red,blue,exit;
    private bool detailed;
    public static void AttachIfRequested(GameObject root)
    {
        if(attached)return;
        foreach(string arg in Environment.GetCommandLineArgs())
        {
            if(!arg.StartsWith("--validate-demo="))continue;
            var probe=root.AddComponent<DemoValidation>();probe.scenario=arg.Substring("--validate-demo=".Length);attached=true;
        }
    }
    private void Start(){StartCoroutine(GuardedRun());}
    private IEnumerator GuardedRun()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            bool moved=false;object current=null;Exception failure=null;
            try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
            catch(Exception error){failure=error;}
            if(failure!=null)
            {
                Check(false,"uncaught test error: "+failure.Message);
                Debug.LogException(failure);
                if(output!=null)
                {
                    File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"failure.png"));
                }
                yield return new WaitForSecondsRealtime(.5f);Application.Quit(2);yield break;
            }
            if(!moved){stack.Pop();continue;}
            var nested=current as IEnumerator;
            if(nested!=null)stack.Push(nested);else yield return current;
        }
    }
    private void Check(bool ok,string name){checks.Add((ok?"PASS / ":"FAIL / ")+name);Debug.Log(checks[checks.Count-1]);}
    private IEnumerator Shot(string name)
    {
        yield return new WaitForSecondsRealtime(.16f);
        Canvas.ForceUpdateCanvases();
        yield return null;
        yield return new WaitForEndOfFrame();
        if(scenario.Contains("font"))
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(output,name+"-unrefreshed.png"));
            yield return new WaitForSecondsRealtime(.1f);
            foreach(var label in FindObjectsOfType<ExhibitionText>())if(label.Rendered!=null)label.Rendered.ForceMeshUpdate();
            yield return null;yield return new WaitForEndOfFrame();
        }
        ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
        yield return new WaitForSecondsRealtime(.15f);
    }
    private IEnumerator Run()
    {
        output=Path.Combine(Application.dataPath,"..","QA",scenario);Directory.CreateDirectory(output);
        detailed=scenario.Contains("table")||scenario.Contains("cards")||scenario.Contains("aspect")||scenario.StartsWith("v3-")||scenario.StartsWith("v4-")||scenario.StartsWith("v5-")||scenario.StartsWith("v7-");
        if(scenario.StartsWith("v4-")||scenario.StartsWith("v5-")||scenario.StartsWith("v7-")||scenario.StartsWith("v8-")||scenario.StartsWith("v9-")||scenario.StartsWith("v10-"))CheckpointStore.Clear();
        yield return new WaitForSecondsRealtime(.8f);
        player=FindObjectOfType<PlayerController>();ui=FindObjectOfType<DialogueUI>();state=GameStateManager.Instance;
        Check(FindObjectsOfType<PlayerController>().Length==1&&FindObjectsOfType<AudioListener>().Length==1,"one player and one listener");
        Check(FindObjectsOfType<DoorDialogue>().Length==3&&FindObjectOfType<RestStation>()!=null,"three voices and a place to rest");
        Check(state.Run.Capacity==6&&state.Run.Load==1&&state.Run.History.Count==0,"new run starts with the intended resources");
        yield return Shot("01-title");
        if(scenario.StartsWith("v5-"))
        {
            var homeArt=IllustrationLibrary.ForCase(CaseId.Home);var officeArt=IllustrationLibrary.ForCase(CaseId.Office);
            Check(IllustrationLibrary.ValidationFallback?(homeArt==null&&officeArt==null):(homeArt!=null&&officeArt!=null&&homeArt!=officeArt&&homeArt.width>=1024&&officeArt.width>=1024),"case images load independently or provide an intentional geometry fallback");
            if(!IllustrationLibrary.ValidationFallback)Check(IllustrationLibrary.Master!=null&&IllustrationLibrary.Master.width>=1600,"key art master travels with the preview");
        }
        var book=FindObjectOfType<NarrativeNotebook>();
        Click("Title ending archive");Check(NarrativeNotebook.Open&&!player.ControlsEnabled,"archive can open from title");
        if(detailed)yield return Shot("01b-archive");Click("Close notebook");
        if(scenario.Contains("office")||scenario.Contains("save-resume"))
        {
            Click("Case Office");yield return null;
            Check(CaseSession.Current==CaseId.Office,"second case can be selected without completing the first");
            CheckTextLayout("case picker");yield return Shot("01c-office-title");
        }
        FindObjectOfType<IntroSequence>().Begin(scenario.EndsWith("-en")?Language.English:Language.Chinese);
        if(scenario.StartsWith("v5-")){yield return null;yield return Shot("01d-illustrated-introduction");CheckTextLayout("illustrated introduction");}
        bool archiveDuringIntro=scenario.Contains("cards");
        if(archiveDuringIntro){book.Show(true);yield return null;}
        float introWait=0;
        while(IntroSequence.IsActive&&introWait<16){introWait+=Time.unscaledDeltaTime;yield return null;}
        yield return null;
        if(archiveDuringIntro)
        {
            Check(NarrativeNotebook.Open&&!player.ControlsEnabled,"introduction cannot steal controls from an open archive");
            Click("Close notebook");yield return null;
        }
        Check(player.ControlsEnabled&&!IntroSequence.IsActive,"introduction returns control");
        // Previous editions are compatibility fixtures; V10 exercises production new-game rules.
        if(scenario.StartsWith("v8-"))state.RestoreRun(new RelationshipRun(state.Run.Case));
        else if(scenario.StartsWith("v9-"))state.RestoreRun(RelationshipRun.PreviousDeckCase());
        else if(!scenario.StartsWith("v10-"))state.RestoreRun(RelationshipRun.Legacy(state.Run.Case));
        if(scenario.Contains("office")||scenario.Contains("save-resume"))Check(state.Run.Case==CaseId.Office,"selected case owns the new model");
        float performanceTime=Time.realtimeSinceStartup;int calmFrames=0;
        while(Time.realtimeSinceStartup-performanceTime<1){calmFrames++;yield return null;}
        Debug.Log("PERFORMANCE / calm frames per second: "+calmFrames+" / focused="+Application.isFocused+" / vsync="+QualitySettings.vSyncCount);
        if(scenario.Contains("perf"))yield return PerformanceProbe();
        yield return Shot("02-arrival");
        if(scenario.StartsWith("v5-"))
        {
            var gallery=FindObjectOfType<IllustrationStation>();
            if(IllustrationLibrary.ValidationFallback)Check(!gallery.CanInteract(player),"unavailable art cannot leave an invisible interaction target");
            else
            {
                yield return Approach(new Vector3(6.3f,.05f,-3.9f),gallery.transform.position+Vector3.up*2.6f);
                Physics.SyncTransforms();var camera=player.PlayerCamera;
                bool sight=Physics.SphereCast(camera.transform.position,.28f,camera.transform.forward,out RaycastHit artHit,5.5f,~0,QueryTriggerInteraction.Collide);
                Check(sight&&artHit.collider.GetComponentInParent<IllustrationStation>()==gallery,"collage is reachable through the real interaction ray");
                yield return Shot("02c-gallery-collage");
                string modelBeforeArt=JsonUtility.ToJson(state.Run.ExportSave());gallery.Interact(player);yield return null;
                Check(ui.IsReflection&&ui.ReflectionArtwork==IllustrationLibrary.ForCase(state.Run.Case)&&!player.ControlsEnabled,"physical art opens its matching enlarged print");
                yield return Shot("02d-visual-notes");CheckTextLayout("visual notes");
                LocalizationManager.ToggleLanguage();yield return null;yield return new WaitForEndOfFrame();CheckTextLayout("translated visual notes");LocalizationManager.ToggleLanguage();yield return null;
                Click("Continue feedback");yield return null;
                Check(player.ControlsEnabled&&!ui.IsDialogueOpen&&JsonUtility.ToJson(state.Run.ExportSave())==modelBeforeArt,"reading and translating art does not spend resources, cards or a response");
            }
        }
        if(scenario.Contains("office-agreement"))
        {
            yield return Approach(new Vector3(-7.6f,.05f,-1.2f),new Vector3(-9.4f,2.5f,1.6f));
            yield return Shot("02b-workplan");
        }
        var hud=FindObjectOfType<ExhibitionHUD>();hud.SetPaused(true);
        Check(ExhibitionHUD.Paused&&Time.timeScale==0&&!player.ControlsEnabled,"pause freezes controls and time");
        if(detailed)yield return Shot("03-pause");hud.SetPaused(false);
        Check(!ExhibitionHUD.Paused&&player.ControlsEnabled,"resume restores controls");
        foreach(var door in FindObjectsOfType<DoorDialogue>())
        {if(door.Actor==DialogueActor.RedDoor)red=door;else if(door.Actor==DialogueActor.BlueDoor)blue=door;else exit=door;}
        if(scenario.StartsWith("v9-")||scenario.StartsWith("v10-")){yield return RunCaseDeckDemo();yield break;}
        if(scenario.StartsWith("v8-")){yield return RunDispatchDemo();yield break;}
        if(scenario.StartsWith("v7-"))yield return ProbeRibbon("00-empty-strip");
        string route="122122";int restMask=1,final=1;RelationshipEnding expected=RelationshipEnding.SharedTable;
        if(scenario.Contains("distance")){route="";restMask=0;final=2;expected=RelationshipEnding.Distance;}
        else if(scenario.Contains("boundary")){route="33";restMask=0;final=2;expected=RelationshipEnding.Boundary;}
        else if(scenario.Contains("exhaustion")){route="111111";restMask=0;final=0;expected=RelationshipEnding.Exhaustion;}
        else if(scenario.Contains("falsepeace")){route="211111";restMask=7;final=0;expected=RelationshipEnding.FalsePeace;}
        else if(scenario.Contains("alliance")){route="123311";restMask=7;final=0;expected=RelationshipEnding.Alliance;}
        else if(scenario.Contains("loop")){route="112221";restMask=3;final=0;expected=RelationshipEnding.Loop;}
        else if(scenario.Contains("refusal")){route="331122";restMask=4;final=1;expected=RelationshipEnding.Distance;}
        else if(scenario.Contains("cards")){route="222222";restMask=6;final=1;expected=RelationshipEnding.SharedTable;}
        if(scenario.StartsWith("v3-")){route=scenario.Contains("return")?"112222":"222222";restMask=7;final=scenario.Contains("return")?2:1;expected=scenario.Contains("return")?RelationshipEnding.Boundary:RelationshipEnding.SharedTable;}
        if(scenario.Contains("office")||scenario.Contains("save-resume")){route="232323";restMask=7;final=1;expected=RelationshipEnding.SharedTable;}
        if(scenario.Contains("intrusion")){route="222222";expected=RelationshipEnding.Loop;}
        if(scenario=="v4-office-alliance-cn"){route="213111";final=0;expected=RelationshipEnding.Alliance;}
        if(scenario.Contains("session-nav")){route="";final=2;expected=RelationshipEnding.Distance;}
        bool cardRun=scenario.Contains("cards");
        for(int turn=0;turn<route.Length;turn++)
        {
            int voice=turn%2;
            if(voice==0&&(restMask&(1<<state.Run.Phase))!=0)yield return RestAtBench(turn);
            var door=voice==0?red:blue;
            yield return Approach(voice==0?new Vector3(-3.3f,.05f,1.8f):new Vector3(3.3f,.05f,1.8f),door.transform.position+Vector3.up*1.8f);
            CheckSight(door,"voice "+(voice+1)+" round "+(state.Run.Phase+1));
            door.Interact(player);yield return null;yield return new WaitForEndOfFrame();
            Check(ui.IsChoosing,"voice opens three responses");
            if(scenario.Contains("office-agreement")&&turn==4)
            {
                Check(!state.Run.OfficePublicRequestDelivered,"last-round warning corresponds to a request still in hand");
                int beforeCancel=state.Run.History.Count;
                Check(ui.CancelChoice()&&state.Run.History.Count==beforeCancel&&player.ControlsEnabled&&state.Run.CanTalk(0),"returning from an unchosen response spends nothing");
                yield return HandleEnvelope(0,1,MessagePayload.Request,MessageAction.Deliver,"13-office-public-request");
                red.Interact(player);yield return null;
                Check(ui.IsChoosing&&state.Run.OfficePublicRequestDelivered,"player can deliver the missing request and reopen the same choice");
            }
            if(cardRun)
            {
                ConversationCard[] selected={ConversationCard.Pause,ConversationCard.Quote,ConversationCard.Listen,ConversationCard.Reframe,ConversationCard.TakeOver,ConversationCard.Boundary};
                var card=selected[turn];Click("Card / "+card);
                Check(state.Run.SelectedCard==card,"card selected: "+card);
            }
            if(turn==0&&detailed)
            {
                int historyBeforeLanguage=state.Run.History.Count;var selectedCard=state.Run.SelectedCard;
                LocalizationManager.ToggleLanguage();yield return null;
                Check(ui.IsChoosing&&state.Run.History.Count==historyBeforeLanguage&&state.Run.SelectedCard==selectedCard,"live language change preserves dialogue and equipped card");
                CheckTextLayout("live language choice");LocalizationManager.ToggleLanguage();yield return null;
            }
            int oldPhase=state.Run.Phase;
            int option=route[turn]-'1';int[] preview=state.Run.Preview(voice,option);
            int breath=state.Run.Capacity,load=state.Run.Load,bridge=state.Run.Bridge;
            if(turn==0||detailed)yield return Shot("04-choice-"+turn);
            CheckTextLayout("choice "+turn);
            Click("Option "+(option+1));yield return null;yield return new WaitForEndOfFrame();
            Check(ui.AwaitingContinue,"feedback waits for the reader");
            Check(state.Run.Capacity-breath==preview[0]&&state.Run.Load-load==preview[1]&&state.Run.Bridge-bridge==preview[2],"shown costs match resolution");
            if(scenario.Contains("refusal")&&turn>=4)
                Check(!state.LastRecord.VoiceConsentsAfter&&state.LastFeedback().Contains(NarrativeText.L("还没准备好","I'm not ready")),"unaccepted invitation is reported as hesitation");
            if(detailed)yield return Shot("05-feedback-"+turn);
            CheckTextLayout("feedback "+turn);
            int count=state.Run.History.Count;ui.Choose(option);
            Check(state.Run.History.Count==count,"a result cannot be selected twice");
            Click("Continue feedback");yield return null;
            Check(player.ControlsEnabled&&!ui.IsDialogueOpen,"continue returns to the gallery");
            if((scenario.Contains("agreement")||scenario.Contains("save-resume")||(scenario.StartsWith("v5-")&&state.Run.Case==CaseId.Office))&&turn==1)
            {
                yield return HandleEnvelope(1,1,MessagePayload.Need,MessageAction.AskPermission,"12-office-private-no");
                Check(state.Run.MessageFrom(1).Permission==MessagePermission.Declined,"office scope agreement does not waive private words");
                if(scenario.Contains("save-resume")||(scenario.StartsWith("v5-")&&state.Run.Case==CaseId.Office))
                {
                    yield return Approach(new Vector3(-3.3f,.05f,1.8f),red.transform.position+Vector3.up*1.8f);
                    yield return HandleEnvelope(0,1,MessagePayload.Request,MessageAction.Deliver,"13-office-public-request");
                    Check(state.Run.OfficePublicRequestDelivered&&!state.Run.HasUnrepairedDisclosure(1),"actual public request arrives while private words stay private");
                }
            }
            if(scenario.Contains("save-resume")&&turn==3)
            {
                var resumeProbe=new GameObject("Checkpoint journey verification").AddComponent<DemoSessionValidation>();
                resumeProbe.Configure(output,checks,true);yield break;
            }
            if(scenario.StartsWith("v3-")&&scenario.Contains("return")&&turn<2)
                yield return HandleEnvelope(voice,voice,MessagePayload.Request,MessageAction.Return,"12-return-"+voice);
            if(scenario.StartsWith("v3-")&&scenario.Contains("consent"))
            {
                if(turn==0)yield return HandleEnvelope(0,0,MessagePayload.Need,MessageAction.AskPermission,"12-permission");
                if(turn==1)
                {
                    yield return HandleEnvelope(1,0,MessagePayload.Need,MessageAction.Deliver,"13-allowed-delivery");
                    yield return HandleEnvelope(1,1,MessagePayload.Need,MessageAction.AskPermission,"14-a-trusted-no");
                    Check(state.Run.BlueTrust>=2&&state.Run.MessageFrom(1).Permission==MessagePermission.Declined,"high trust does not turn a private need into permission");
                    yield return HandleEnvelope(1,1,MessagePayload.Need,MessageAction.Return,"14-kept-private");
                }
            }
            if(cardRun&&turn==4)
            {
                int capacityAfterTakeover=state.Run.Capacity;
                Check(state.Rewind(),"rewind card reopens the last voice");yield return null;
                Check(ui.IsChoosing&&state.Run.Stock(ConversationCard.TakeOver)==0&&state.Run.Stock(ConversationCard.Rewind)==0,"rewind restores choice without refunding cards");
                Check(state.Run.Capacity>capacityAfterTakeover,"rewind returns the spent breath");
                yield return Shot("05b-revision");Click("Option 2");yield return null;Click("Continue feedback");yield return null;
            }
            if(detailed&&turn==3)
            {
                book.Show(false);Check(NarrativeNotebook.Open&&!player.ControlsEnabled,"notes keep the world from taking input");
                yield return Shot("06-notebook");CheckTextLayout("notebook");book.Close();
                book.Show(false);Click("Read response / 0");yield return null;
                yield return Shot("06b-sentence-trace");CheckTextLayout("sentence trace");
                var behind=GameObject.Find("Ending archive").GetComponent<RectTransform>();
                var modalPointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,behind.TransformPoint(behind.rect.center))};
                var modalHits=new List<RaycastResult>();EventSystem.current.RaycastAll(modalPointer,modalHits);
                Check(modalHits.Count>0&&modalHits[0].gameObject.name=="Sentence focus","sentence reading layer blocks buttons behind it");
                int recordedCount=state.Run.History.Count;LocalizationManager.ToggleLanguage();yield return null;yield return new WaitForEndOfFrame();
                Check(NarrativeNotebook.Open&&state.Run.History.Count==recordedCount,"language change preserves open sentence trace");
                CheckTextLayout("translated trace");LocalizationManager.ToggleLanguage();yield return null;
                Click("Close sentence trace");book.Close();
            }
            if(scenario.StartsWith("v7-")&&turn==3)
            {
                yield return ProbeProofCancellation();
                if(state.Run.Case==CaseId.Home)yield return HandleEnvelope(1,0,MessagePayload.Request,MessageAction.Deliver,"17-public-delivery");
                yield return ProbeRibbon("18-growing-strip");
            }
            if(oldPhase<state.Run.Phase)Check(!state.Run.RedDone&&!state.Run.BlueDone,"new round resets both voices");
        }
        if(cardRun)
        {for(int card=0;card<7;card++)Check(state.Run.Stock((ConversationCard)card)==0,"single-use card spent: "+(ConversationCard)card);}
        if(scenario.StartsWith("v3-")&&scenario.Contains("breach"))
        {
            yield return HandleEnvelope(1,0,MessagePayload.Need,MessageAction.Deliver,"12-unallowed-delivery");
            Check(state.Run.HasUnrepairedDisclosure(0)&&!state.Run.RedConsents&&state.Run.BlueConsents,"a disclosure revokes only the source's prior yes");
            yield return Approach(new Vector3(-3.3f,.05f,1.8f),red.transform.position+Vector3.up*1.8f);
            yield return HandleEnvelope(0,0,MessagePayload.Need,MessageAction.Repair,"13-acknowledge-and-ask-again");
            Check(!state.Run.HasUnrepairedDisclosure(0)&&state.Run.RedConsents&&state.Run.BlueConsents,"repair asks for a fresh yes without speaking for the other voice");
        }
        yield return Approach(new Vector3(0,.05f,8.8f),exit.transform.position+Vector3.up*1.8f);
        CheckSight(exit,"your own exit");exit.Interact(player);yield return null;
        Check(ui.IsChoosing&&ui.IsFinalDecision,"exit permits a personal decision at any stage");
        yield return Shot("07-decision");CheckTextLayout("final decision");Click("Option "+(final+1));
        yield return new WaitForSecondsRealtime(3.1f);
        Check(state.Run.Ended&&state.Run.Ending==expected,"expected position: "+expected);
        Check(ui.EndingVisible&&player.ControlsEnabled,"ending can be read and explored");
        Check(!CheckpointStore.HasCheckpoint,"an ending retires its active checkpoint");
        Check(!state.Run.CanTalk(0)&&!state.Run.CanTalk(1)&&!state.Run.CanRest&&!state.Run.CanRewind,"ending locks narrative changes");
        yield return Shot("08-ending");CheckTextLayout("ending");
        if(scenario.StartsWith("v7-"))yield return ProbeRibbon("19-ending-strip");
        if(scenario.StartsWith("v5-")&&!IllustrationLibrary.ValidationFallback&&!scenario.Contains("store"))
        {
            var gallery=FindObjectOfType<IllustrationStation>();
            yield return Approach(new Vector3(6.3f,.05f,-3.9f),gallery.transform.position+Vector3.up*2.6f);
            gallery.Interact(player);yield return null;yield return new WaitForEndOfFrame();
            Check(ui.IsDialogueOpen&&ui.ReflectionArtwork!=null&&GameObject.Find("Case navigation")==null,"art can be reread after an ending without menus covering it");
            Click("Continue feedback");yield return null;
            Check(player.ControlsEnabled&&!ui.IsDialogueOpen&&state.Run.Ending==expected,"closing the post-ending print returns exploration without changing the outcome");
        }
        if(expected==RelationshipEnding.SharedTable&&scenario.StartsWith("v4-"))
        {
            hud.ToggleEndingOverlay();yield return null;
            Check(ui.EndingVisible&&!GameObject.Find("Case navigation"),"ending papers fold away while the completed state remains");
            yield return Approach(new Vector3(0,.05f,8.8f),new Vector3(0,1.2f,5.5f));
            yield return Shot("08b-work-agreement-table");hud.ToggleEndingOverlay();yield return null;
        }
        if(RelationshipRun.OpensExit(expected))
        {
            yield return Approach(new Vector3(0,.05f,14.8f),new Vector3(0,1.8f,16.5f));
            Check(player.transform.position.z>13.7f&&player.transform.position.y>-.5f,"exit is physically walkable with solid floor");
            if(detailed)yield return Shot("09-through-the-door");
        }
        else
        {
            yield return Approach(new Vector3(0,.05f,14.8f),new Vector3(0,1.8f,16.5f),1.2f);
            Check(player.transform.position.z<12.5f,"closed outcome retains a physical door");
            yield return Approach(new Vector3(4,.05f,10.6f),new Vector3(4,1.8f,16.5f),1.5f);
            yield return Approach(new Vector3(4,.05f,14.8f),new Vector3(4,1.8f,16.5f),1.2f);
            Check(player.transform.position.z<12.5f,"closed exit cannot be bypassed around its right side");
        }
        book.Show(true);Check(EndingArchive.Known(expected),"current ending appears in archive without persisting test progress");
        yield return Shot("10-ending-archive");CheckTextLayout("archive");book.Close();
        var frozen=state.Run.Ending;LocalizationManager.Current=LocalizationManager.Current==Language.Chinese?Language.English:Language.Chinese;
        Check(state.Run.Ending==frozen,"language change cannot overwrite an ending");
        if(scenario.Contains("session-nav"))
        {
            var navigationProbe=new GameObject("Case navigation verification").AddComponent<DemoSessionValidation>();
            navigationProbe.Configure(output,checks,false);yield break;
        }
        var restart=new GameObject("Restart verification").AddComponent<DemoRestartValidation>();restart.Configure(output,checks);
    }
    private IEnumerator HandleEnvelope(int atVoice,int source,MessagePayload payload,MessageAction action,string name)
    {
        var book=FindObjectOfType<NarrativeNotebook>();book.ShowCorrespondence(atVoice);
        yield return null;yield return new WaitForEndOfFrame();
        Check(NarrativeNotebook.Open&&!player.ControlsEnabled&&CorrespondenceFolio.NearbyVoice(player)==atVoice,"correspondence opens at the actual addressed door");
        Click("Envelope source / "+source);yield return null;Click("Message payload / "+(int)payload);yield return null;
        var folio=FindObjectOfType<CorrespondenceFolio>();var item=state.Run.MessageFrom(source);
        int count=state.Run.Correspondence.Count;
        var preview=state.Run.PreviewMessage(item.Id,action,payload,atVoice);
        Check(preview!=null,"envelope action is available in its actual context: "+action);
        yield return Shot(name+"-preview");CheckTextLayout("correspondence preview / "+action);
        LocalizationManager.ToggleLanguage();yield return null;yield return new WaitForEndOfFrame();
        Check(folio.SelectedSource==source&&folio.SelectedPayload==payload&&state.Run.Correspondence.Count==count,"live translation preserves envelope selection and action history");
        CheckTextLayout("translated correspondence");LocalizationManager.ToggleLanguage();yield return null;
        Click("Message action / "+action);yield return null;yield return new WaitForEndOfFrame();
        if(action==MessageAction.Deliver)
        {
            Check(folio.ProofOpen&&state.Run.Correspondence.Count==count,"delivery first shows the complete words without resolving them");
            yield return Shot(name+"-proof");CheckTextLayout("delivery proof");
            Click("Confirm dispatch proof");yield return null;yield return new WaitForEndOfFrame();
        }
        Check(state.Run.Correspondence.Count==count+1,"a correspondence action leaves one receipt");
        var actual=state.Run.Correspondence[state.Run.Correspondence.Count-1];
        Check(actual.CapacityDelta==preview.CapacityDelta&&actual.LoadDelta==preview.LoadDelta&&actual.BridgeDelta==preview.BridgeDelta&&actual.SourceTrustDelta==preview.SourceTrustDelta&&actual.RecipientTrustDelta==preview.RecipientTrustDelta,"message preview agrees with all resolved costs and both trust changes");
        Check(!state.Run.CanRewind,"an exchange heard by another person cannot be erased with revision");
        yield return Shot(name+"-receipt");CheckTextLayout("correspondence receipt");
        book.Close();yield return null;
        Check(player.ControlsEnabled,"leaving the folio restores exploration");
    }
    private IEnumerator RestAtBench(int turn)
    {
        var rest=FindObjectOfType<RestStation>();
        yield return Approach(new Vector3(-5.8f,.05f,-3),rest.transform.position+Vector3.up*1.2f);
        Physics.SyncTransforms();var camera=player.PlayerCamera;
        bool hit=Physics.SphereCast(camera.transform.position,.28f,camera.transform.forward,out RaycastHit ray,5.5f,~0,QueryTriggerInteraction.Collide);
        Check(hit&&ray.collider.GetComponentInParent<RestStation>()==rest,"bench reachable by interaction ray");
        int breath=state.Run.Capacity;rest.Interact(player);yield return null;
        Check(state.Run.Capacity>=breath&&!state.Run.CanRest,"rest restores breath only once in a round");
        if(detailed)yield return Shot("03b-rest-"+turn);
        Click("Continue feedback");yield return null;
    }
    private IEnumerator PerformanceProbe()
    {
        int atlasRebuilds=0;System.Action<Font> handler=font=>atlasRebuilds++;
        Font.textureRebuilt+=handler;
        var canvas=FindObjectOfType<Canvas>();var camera=player.PlayerCamera;int mask=camera.cullingMask;
        for(int mode=0;mode<4;mode++)
        {
            QualitySettings.vSyncCount=0;Application.targetFrameRate=mode==0?60:-1;
            canvas.enabled=mode!=2;camera.cullingMask=mode==3?0:mask;
            yield return new WaitForSecondsRealtime(.3f);atlasRebuilds=0;int frames=0;float started=Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup-started<1.5f){frames++;yield return null;}
            Debug.Log("PROFILE / mode="+mode+" fps="+(frames/(Time.realtimeSinceStartup-started)).ToString("0.0")+" atlas="+atlasRebuilds);
        }
        Font.textureRebuilt-=handler;canvas.enabled=true;camera.cullingMask=mask;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
    }
    internal static void Click(string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var button in FindObjectsOfType<Button>())if(button.name==name&&button.gameObject.activeInHierarchy)
        {
            if(!button.IsInteractable())throw new Exception("QA button not interactable: "+name);
            var rect=button.GetComponent<RectTransform>();
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new Exception("QA button occluded: "+name+" / hit="+(hits.Count>0?hits[0].gameObject.name:"none")+" / point="+pointer.position+" / depth="+button.targetGraphic.depth);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);return;
        }
        throw new Exception("QA button missing: "+name);
    }
    private void CheckSight(DoorDialogue target,string label)
    {
        Physics.SyncTransforms();var camera=player.PlayerCamera;
        bool hit=Physics.SphereCast(camera.transform.position,.28f,camera.transform.forward,out RaycastHit ray,5.5f,~0,QueryTriggerInteraction.Collide);
        Check(hit&&ray.collider.GetComponentInParent<DoorDialogue>()==target,label+" has an unobstructed interaction ray");
    }
    private void CheckTextLayout(string stage)
    {
        Canvas.ForceUpdateCanvases();
        var clipped=new List<string>();
        foreach(var text in FindObjectsOfType<ExhibitionText>())
        {
            if(!text.gameObject.activeInHierarchy||string.IsNullOrEmpty(text.text))continue;
            float alpha=1;
            for(Transform p=text.transform;p!=null;p=p.parent){var group=p.GetComponent<CanvasGroup>();if(group!=null)alpha*=group.alpha;}
            if(alpha<.05f)continue;
            var sdf=text as ExhibitionText;
            if(sdf!=null&&sdf.Rendered!=null)
            {
                float preferred=sdf.Rendered.preferredHeight;
                if(preferred>sdf.rectTransform.rect.height+3)clipped.Add("SDF "+text.name+" "+preferred.ToString("0")+"/"+sdf.rectTransform.rect.height.ToString("0"));
                sdf.Rendered.ForceMeshUpdate();
                var glyphs=sdf.Rendered.textInfo;
                bool fullCheck=text.GetComponentInParent<DispatchComposer>()!=null||text.GetComponentInParent<CaseDeckPanel>()!=null||((scenario.StartsWith("v8-")||scenario.StartsWith("v9-")||scenario.StartsWith("v10-"))&&(stage=="all seven new ending summaries"||stage=="translated ending summaries"||stage=="notes before the collage"));
                int checkedGlyphs=fullCheck?glyphs.characterCount:Mathf.Min(6,glyphs.characterCount);
                for(int index=0;index<checkedGlyphs;index++)
                {
                    var glyph=glyphs.characterInfo[index];
                    if(char.IsWhiteSpace(glyph.character)||char.IsControl(glyph.character))continue;
                    if(!glyph.isVisible){clipped.Add("hidden leading glyph / "+text.name);break;}
                    var vertices=glyphs.meshInfo[glyph.materialReferenceIndex].vertices;int first=glyph.vertexIndex;
                    if((vertices[first+2]-vertices[first]).sqrMagnitude<.01f){clipped.Add("empty leading quad / "+text.name);break;}
                }
                if(scenario.Contains("aspect")&&stage=="choice 0"&&(text.name=="Status Text"||text.name=="Card selected name"||text.name=="Controls"))
                {
                    var info=sdf.Rendered.textInfo;string prefix="";
                    for(int i=0;i<Mathf.Min(6,info.characterCount);i++){var c=info.characterInfo[i];prefix+=c.character+":"+c.isVisible+":"+c.bottomLeft+";";}
                    Debug.Log("GLYPH_PROBE / "+text.name+" / source="+text.text+" / "+prefix);
                }
            }
            else if(text.preferredHeight>text.rectTransform.rect.height+3)clipped.Add(text.name+" "+text.preferredHeight.ToString("0")+"/"+text.rectTransform.rect.height.ToString("0"));
        }
        Check(clipped.Count==0,"text fits / "+stage+(clipped.Count>0?" / "+string.Join(", ",clipped):""));
    }
    private IEnumerator Approach(Vector3 destination,Vector3 lookAt,float maximum=5)
    {
        var controller=player.GetComponent<CharacterController>();player.SetControlsEnabled(false);float elapsed=0;
        while(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(destination.x,destination.z))>.09f&&elapsed<maximum)
        {
            Vector3 delta=destination-player.transform.position;delta.y=0;
            controller.Move(delta.normalized*Mathf.Min(delta.magnitude,6*Time.deltaTime)+Vector3.down*2*Time.deltaTime);elapsed+=Time.deltaTime;yield return null;
        }
        player.transform.rotation=Quaternion.Euler(0,Quaternion.LookRotation(lookAt-player.PlayerCamera.transform.position).eulerAngles.y,0);
        player.PlayerCamera.transform.LookAt(lookAt);player.SetControlsEnabled(true);yield return null;
    }
}
