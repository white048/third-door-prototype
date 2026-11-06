using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoRestartValidation : MonoBehaviour
{
    private string output;
    private List<string> checks;
    public void Configure(string folder,List<string> results)
    {output=folder;checks=results;DontDestroyOnLoad(gameObject);}
    private IEnumerator Start()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        yield return new WaitForSecondsRealtime(1);
        var state=GameStateManager.Instance;
        Check(state!=null&&!state.redDoorDone&&!state.blueDoorDone&&!state.gameEnded&&state.directConnection==0,"restart resets narrative state");
        Check(FindObjectsOfType<PlayerController>().Length==1&&FindObjectsOfType<AudioListener>().Length==1,"restart does not duplicate player or listener");
        Check(!IntroSequence.IsActive&&FindObjectOfType<PlayerController>().ControlsEnabled,"restart resumes play without repeating introduction");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(output,"09-restarted.png"));
        yield return new WaitForSecondsRealtime(.25f);
        File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);
        bool passed=checks.TrueForAll(s=>s.StartsWith("PASS"));
        Debug.Log("DEMO_VALIDATION / "+(passed?"PASS":"FAIL"));
        Application.Quit(passed?0:2);
    }
    private void Check(bool ok,string message){checks.Add((ok?"PASS / ":"FAIL / ")+message);Debug.Log(checks[checks.Count-1]);}
}
