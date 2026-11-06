using UnityEngine;
using UnityEngine.UI;

public sealed class NarrativeRibbonStation : Interactable
{
    private RawImage[] fragments;
    private TextMesh count;
    private int revision=-1;
    public void Configure(RawImage[] images,TextMesh label){fragments=images;count=label;}
    private void LateUpdate()
    {
        var state=GameStateManager.Instance;if(state==null||revision==state.Run.Revision)return;
        revision=state.Run.Revision;var run=state.Run;
        var texture=IllustrationLibrary.Load("ObjectCutouts-v7");
        for(int i=0;i<fragments.Length;i++)
        {
            fragments[i].texture=texture;fragments[i].uvRect=new Rect(0,2/3f,1/3f,1/3f);
            fragments[i].gameObject.SetActive(texture!=null&&i<Mathf.Max(1,run.History.Count));
        }
        count.text=run.History.Count+" RESPONSES\n"+run.Correspondence.Count+" EXCHANGES";
    }
    public override string PromptText=>NarrativeText.L("[ E ]  展开本局拼贴长卷","[ E ]  UNROLL THIS RUN'S COLLAGE STRIP");
    public override bool CanInteract(PlayerController player)=>player!=null&&player.ControlsEnabled&&!IntroSequence.IsActive&&!ExhibitionHUD.Paused&&!NarrativeNotebook.Open;
    public override void Interact(PlayerController player)
    {if(CanInteract(player)){FindObjectOfType<NarrativeNotebook>().ShowRibbon();AudioManager.Instance?.Play(GameSound.ChoiceClick,.2f);}}
}
