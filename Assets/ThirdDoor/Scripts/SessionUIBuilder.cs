using UnityEngine;
using static ExhibitionDesign;

public partial class SceneBootstrapper
{
    private void BuildCaseNavigation(Transform canvas,ExhibitionHUD hud,UnityEngine.UI.Button pauseTitle)
    {
        var paper=Block(canvas,"Case navigation",new Vector2(1280,590),new Vector2(580,375),Paper);
        Bottom(paper.rectTransform);
        Block(paper.transform,"Case navigation rule",Vector2.zero,new Vector2(580,4),Red);
        var caption=Type(paper.transform,"Case navigation caption","",new Vector2(24,23),new Vector2(532,68),24,Ink,chineseUiFont);
        var replay=Button(paper.transform,"Replay case","",new Vector2(24,110),new Vector2(532,62),Ink,Paper,chineseUiFont);
        var next=Button(paper.transform,"Next case","",new Vector2(24,190),new Vector2(532,62),Red,Paper,chineseUiFont);
        var title=Button(paper.transform,"Ending return to title","",new Vector2(24,270),new Vector2(532,62),Ink,Paper,chineseUiFont);
        var hint=Type(paper.transform,"Case navigation hint","",new Vector2(24,345),new Vector2(532,24),16,Muted,chineseUiFont);
        replay.GetComponentInChildren<ExhibitionText>().fontSize=22;
        next.GetComponentInChildren<ExhibitionText>().fontSize=22;
        title.GetComponentInChildren<ExhibitionText>().fontSize=22;
        hud.ConfigureSession(paper.gameObject,caption,hint,replay,next,title,pauseTitle);
        paper.gameObject.SetActive(false);
    }
}
