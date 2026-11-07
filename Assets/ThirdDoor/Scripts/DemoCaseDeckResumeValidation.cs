using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class DemoCaseDeckResumeValidation : MonoBehaviour
{
    private string output;private List<string> checks;
    public void Configure(string path,List<string> results){output=path;checks=results;DontDestroyOnLoad(gameObject);}
    private void Start(){StartCoroutine(Guard());}
    private void Check(bool ok,string text){checks.Add((ok?"PASS / ":"FAIL / ")+text);Debug.Log(checks[checks.Count-1]);}
    private IEnumerator Guard()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            bool more=false;object current=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception error){failure=error;}
            if(failure!=null){Check(false,"deck continuation: "+failure.Message);Debug.LogException(failure);break;}
            if(!more){stack.Pop();continue;}var nested=current as IEnumerator;if(nested!=null)stack.Push(nested);else yield return current;
        }
        File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);bool ok=checks.TrueForAll(s=>s.StartsWith("PASS"));Debug.Log("DEMO_VALIDATION / "+(ok?"PASS":"FAIL"));yield return new WaitForSecondsRealtime(.3f);Application.Quit(ok?0:2);
    }
    private IEnumerator Run()
    {
        var r=GameStateManager.Instance.Run;string expected=JsonUtility.ToJson(r.ExportSave());
        Check(r.Phase==1&&r.DeckReady&&r.MessageFrom(0).LastTool==DispatchTool.Pause&&r.MessageFrom(0).PublicProgress==2&&r.DeckStock(DispatchTool.Pause)==0,"checkpoint includes a primed combination and a spent card in scene two");
        CaseSession.ReturnToTitle();yield return new WaitForSecondsRealtime(.9f);
        Check(IntroSequence.IsActive&&CheckpointStore.HasCheckpoint,"suspended deck run appears at the real title");
        DemoValidation.Click("Continue saved journey");yield return new WaitForSecondsRealtime(.9f);
        r=GameStateManager.Instance.Run;Check(!IntroSequence.IsActive&&r.DeckRules&&JsonUtility.ToJson(r.ExportSave())==expected,"real Continue restores every deck, card, scene and envelope field");
        Check(FindObjectOfType<PlayerController>().ControlsEnabled,"continued deck run restores exploration");
        var m=r.MessageFrom(0);var result=r.SendPacket(m.Id,DispatchFragment.Fact,DispatchFragment.None,DispatchTool.Reframe,1);
        Check(result!=null&&result.Combo==DispatchCombo.PacedReframe&&result.Completed&&r.DeckStock(DispatchTool.Pause)==0&&r.DeckStock(DispatchTool.Reframe)==0,"the saved setup still powers reframe without refunding either card");
        GameStateManager.Instance.RefreshAfterCorrespondence();var book=FindObjectOfType<NarrativeNotebook>();book.Show(false);yield return null;DemoValidation.Click("Rewind response");yield return null;
        Check(FindObjectOfType<CaseDeckPanel>().IsOpen,"continued hand opens with its actual remaining uses");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"38-continued-hand.png"));book.Close();
        book.ShowRibbon();yield return null;var ribbon=FindObjectOfType<NarrativeRibbon>();
        Check(ribbon.ResponseCount==r.History.Count&&ribbon.ReceiptCount==r.Correspondence.Count,"continued strip rebuilds both scenes and every card receipt");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"39-continued-deck-strip.png"));yield return new WaitForSecondsRealtime(.3f);book.Close();
    }
}
