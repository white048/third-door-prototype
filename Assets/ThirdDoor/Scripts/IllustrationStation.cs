using UnityEngine;

public sealed class IllustrationStation : Interactable
{
    private GameObject display;
    private Renderer printSurface;
    private BoxCollider target;
    private TextMesh caption;
    private Material material;
    private CaseId shown=(CaseId)(-1);
    public Texture2D Artwork { get; private set; }
    public void Configure(GameObject frame,Renderer surface,BoxCollider interaction,Material ink,TextMesh label)
    {display=frame;printSurface=surface;target=interaction;material=ink;caption=label;Refresh();}
    private void LateUpdate(){if(shown!=CaseInfo.Active)Refresh();}
    private void Refresh()
    {
        shown=CaseInfo.Active;Artwork=IllustrationLibrary.ForCase(shown);
        display.SetActive(Artwork!=null);target.enabled=Artwork!=null;
        if(Artwork==null)return;
        if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",Artwork);
        if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",Artwork);
        printSurface.sharedMaterial=material;
        caption.text=shown==CaseId.Office?"PLATE 02 / KEEP A SPACE PRIVATE":"PLATE 01 / CARE WITHOUT CARRYING";
    }
    public override string PromptText=>NarrativeText.L("[ E ]  读这幅拼贴 / 图像手记","[ E ]  READ THE COLLAGE / VISUAL NOTES");
    public override bool CanInteract(PlayerController player)=>Artwork!=null&&player!=null&&player.ControlsEnabled&&!IntroSequence.IsActive&&!ExhibitionHUD.Paused&&!NarrativeNotebook.Open;
    public override void Interact(PlayerController player)
    {
        if(!CanInteract(player))return;
        var ui=FindObjectOfType<DialogueUI>();CaseId selected=shown;
        ui.ShowArtwork(Artwork,()=>CaseInfo.Title(selected),()=>selected==CaseId.Office
            ?NarrativeText.L("纸上两条工作安排连着红线，事由栏却空着。\n旁边的黑信封也没有拆开。\n这次要转告的工作，已经写清楚了。","Red thread connects the two tasks. The reason field is blank.\nBeside it, the black envelope stays closed.\nThe work request is already clear.")
            :NarrativeText.L("钥匙系在信封上，红线松松地绕过两只手。\n线头停在门边，没有把那条路封住。\n你可以接过信，也能把手松开。","A key hangs from an envelope. Loose thread links two hands.\nIt ends beside the doorway, leaving a way through.\nYou can take the message. You can also let go."));
        AudioManager.Instance?.Play(GameSound.ChoiceClick,.2f);
    }
}
