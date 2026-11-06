using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Lives through the real title / continue / case scene transitions during explicit QA.
public sealed class DemoSessionValidation : MonoBehaviour
{
    private string output;private List<string> checks;private bool resume;
    public void Configure(string folder,List<string> results,bool resumeCheckpoint)
    {output=folder;checks=results;resume=resumeCheckpoint;DontDestroyOnLoad(gameObject);}
    private void Start(){StartCoroutine(Guard());}
    private IEnumerator Guard()
    {
        var stack=new Stack<IEnumerator>();stack.Push(resume?Resume():Navigation());
        while(stack.Count>0)
        {
            bool moved=false;object current=null;Exception error=null;
            try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){error=e;}
            if(error!=null){Check(false,"session exception: "+error.Message);Debug.LogException(error);break;}
            if(!moved){stack.Pop();continue;}var nested=current as IEnumerator;if(nested!=null)stack.Push(nested);else yield return current;
        }
        File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);
        bool passed=checks.TrueForAll(s=>s.StartsWith("PASS"));Debug.Log("DEMO_VALIDATION / "+(passed?"PASS":"FAIL"));
        yield return new WaitForSecondsRealtime(.3f);Application.Quit(passed?0:2);
    }
    private IEnumerator Resume()
    {
        if(GameStateManager.Instance.Run.DispatchRules){yield return ResumeDispatch();yield break;}
        var run=GameStateManager.Instance.Run;string expected=JsonUtility.ToJson(run.ExportSave());
        Check(run.Case==CaseId.Office&&run.Phase==2&&run.CanRewind&&run.OfficePublicRequestDelivered,"checkpoint covers second case, correspondence and available rewind");
        Check(CheckpointStore.Save(run),"native JSON checkpoint saves");RelationshipRun loaded;
        Check(CheckpointStore.TryLoad(out loaded)&&JsonUtility.ToJson(loaded.ExportSave())==expected,"native JSON roundtrip preserves every model field");
        Check(CheckpointStore.Save(run)&&File.Exists(CheckpointStore.StoragePath+".bak"),"atomic replacement retains a backup");
        File.WriteAllText(CheckpointStore.StoragePath,"{truncated");
        Check(CheckpointStore.TryLoad(out loaded)&&CheckpointStore.LastStatus==CheckpointStatus.RecoveredBackup&&JsonUtility.ToJson(loaded.ExportSave())==expected,"damaged primary recovers the complete backup");
        File.WriteAllText(CheckpointStore.StoragePath+".bak","damaged backup");
        Check(!CheckpointStore.TryLoad(out loaded)&&loaded==null,"two corrupt files are rejected without an exception");
        Check(CheckpointStore.Clear()&&!CheckpointStore.HasCheckpoint,"clear retires primary and backup");
        Check(CheckpointStore.Save(run),"active journey can be saved again");
        CaseSession.ReturnToTitle();yield return WaitForScene(true);
        Check(FindObjectsOfType<PlayerController>().Length==1&&FindObjectsOfType<AudioListener>().Length==1,"return to title keeps a single player and listener");
        Check(CheckpointStore.HasCheckpoint&&IntroSequence.IsActive,"title offers the suspended journey");
        yield return Shot("15-continue-title");DemoValidation.Click("Continue saved journey");yield return WaitForScene(false);
        var restored=GameStateManager.Instance.Run;
        Check(restored.Case==CaseId.Office&&JsonUtility.ToJson(restored.ExportSave())==expected,"real Continue button restores the selected case and full checkpoint");
        Check(FindObjectOfType<PlayerController>().ControlsEnabled&&restored.CanRewind,"continue returns control with the same revision opportunity");
        var book=FindObjectOfType<NarrativeNotebook>();book.Show(false);yield return Shot("16-restored-notebook");
        DemoValidation.Click("Collage ribbon tab");yield return null;
        var ribbon=FindObjectOfType<NarrativeRibbon>();
        Check(ribbon!=null&&ribbon.ResponseCount==restored.History.Count&&ribbon.ReceiptCount==restored.Correspondence.Count,"continued save rebuilds every response and exchange in its collage strip");
        ribbon.JumpToChapter(1);yield return Shot("17-restored-collage");
        Check(JsonUtility.ToJson(restored.ExportSave())==expected,"reading the restored collage preserves the complete undo snapshot");book.Close();
        var rewind=restored.ExportSave();
        Check(restored.Rewind()&&restored.History.Count==3&&restored.Stock(ConversationCard.Rewind)==0,"a restored undo snapshot works without refunding its card");
        RelationshipRun replay;Check(RelationshipRun.TryRestoreSave(rewind,out replay),"saved pre-revision journey stays independent");
        GameStateManager.Instance.RestoreRun(replay);
    }
    private IEnumerator ResumeDispatch()
    {
        var run=GameStateManager.Instance.Run;string expected=JsonUtility.ToJson(run.ExportSave());
        Check(run.DispatchRules&&run.MessageFrom(1).PublicProgress==1&&run.Stock(ConversationCard.Pause)==0,"new checkpoint includes a partial hearing and a spent shared card");
        Check(CheckpointStore.Save(run),"new-rule checkpoint writes native JSON");
        CaseSession.ReturnToTitle();yield return WaitForScene(true);yield return Shot("27-continue-dispatch-title");
        DemoValidation.Click("Continue saved journey");yield return WaitForScene(false);
        var restored=GameStateManager.Instance.Run;
        Check(restored.DispatchRules&&JsonUtility.ToJson(restored.ExportSave())==expected,"real Continue restores every new-rule field without changing the rules");
        Check(FindObjectOfType<PlayerController>().ControlsEnabled&&!restored.CanRewind,"continuation restores exploration and preserves heard exchanges");
        var book=FindObjectOfType<NarrativeNotebook>();book.Show(false);yield return null;DemoValidation.Click("Collage ribbon tab");yield return null;
        var ribbon=FindObjectOfType<NarrativeRibbon>();
        Check(ribbon.ResponseCount==restored.History.Count&&ribbon.ReceiptCount==restored.Correspondence.Count,"continued collage reconstructs all packet attempts");yield return Shot("28-continued-dispatch-strip");book.Close();
        var m=restored.MessageFrom(1);var result=restored.SendPacket(m.Id,DispatchFragment.Request,DispatchFragment.None,DispatchTool.None,0);
        Check(result!=null&&result.Completed&&restored.BothPlansVerified&&restored.Stock(ConversationCard.Pause)==0,"remaining original fragment resolves after continuation without refunding a spent card");
        GameStateManager.Instance.RefreshAfterCorrespondence();
    }
    private IEnumerator Navigation()
    {
        Check(GameStateManager.Instance.Run.Case==CaseId.Home&&GameStateManager.Instance.Run.Ended,"navigation begins after a completed first case");
        FindObjectOfType<PlayerController>().SetControlsEnabled(false);yield return null;
        DemoValidation.Click("Next case");yield return WaitForScene(false);
        Check(GameStateManager.Instance.Run.Case==CaseId.Office&&!GameStateManager.Instance.Run.Ended&&GameStateManager.Instance.Run.History.Count==0,"Next case starts an independent second case");
        Check(GameStateManager.Instance.Run.Capacity==6&&GameStateManager.Instance.Run.Stock(ConversationCard.Pause)==1,"new case resets resources and cards");
        var hud=FindObjectOfType<ExhibitionHUD>();hud.SetPaused(true);yield return null;
        DemoValidation.Click("Return to title");yield return WaitForScene(true);
        Check(CaseSession.Current==CaseId.Office&&IntroSequence.IsActive,"pause returns to the selected case title");
        DemoValidation.Click("Case Home");yield return null;
        FindObjectOfType<IntroSequence>().Begin(LocalizationManager.Current);yield return WaitForScene(false);
        Check(GameStateManager.Instance.Run.Case==CaseId.Home&&GameStateManager.Instance.Run.History.Count==0,"title case switch does not carry the previous case's state");
        hud=FindObjectOfType<ExhibitionHUD>();hud.SetPaused(true);yield return null;hud.Restart();yield return WaitForScene(false);
        Check(GameStateManager.Instance.Run.Case==CaseId.Home&&!GameStateManager.Instance.Run.Ended&&FindObjectsOfType<PlayerController>().Length==1,"replay resets the selected case without duplicating the player");
        yield return Shot("15-case-navigation");
    }
    private IEnumerator WaitForScene(bool title)
    {
        yield return new WaitForSecondsRealtime(.8f);float elapsed=0;
        while(elapsed<18&&(GameStateManager.Instance==null||IntroSequence.IsActive!=title))
        {elapsed+=Time.unscaledDeltaTime;yield return null;}
        Check(GameStateManager.Instance!=null&&IntroSequence.IsActive==title,"scene transition completes within its limit");
        yield return null;
    }
    private IEnumerator Shot(string name)
    {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.25f);}
    private void Check(bool ok,string text){checks.Add((ok?"PASS / ":"FAIL / ")+text);Debug.Log(checks[checks.Count-1]);}
}
