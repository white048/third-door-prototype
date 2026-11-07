using UnityEngine;
using UnityEngine.UI;
using static ExhibitionDesign;

public sealed class CaseDeckArt : MonoBehaviour
{
    private Image[] tabs=new Image[3];
    private ExhibitionText[] captions=new ExhibitionText[3];
    public void Configure(Transform paper,Font body)
    {
        for(int i=0;i<3;i++)
        {
            tabs[i]=Block(paper,"Deck scene tab / "+i,new Vector2(1190+i*180,145),new Vector2(166,55),Ink);
            captions[i]=Type(tabs[i].transform,"Scene tab label","",new Vector2(12,0),new Vector2(142,55),19,Paper,body);captions[i].alignment=TextAnchor.MiddleLeft;
        }
        var texture=IllustrationLibrary.Load("ObjectCutouts-v7");
        if(texture!=null)
        {
            var rect=Rect(paper,"Hand binder clip",new Vector2(1500,230),new Vector2(215,215));rect.localRotation=Quaternion.Euler(0,0,-8);
            var photo=rect.gameObject.AddComponent<RawImage>();photo.texture=texture;photo.uvRect=new Rect(1/3f,1/3f,1/3f,1/3f);photo.raycastTarget=false;
        }
        else ToolMark.Create(paper,DispatchTool.Receipt,new Vector2(1540,250),new Vector2(120,120),Ink);
        // Crop marks and the offset register borrow the language of the physical printed installation.
        foreach(var corner in new[]{new Vector2(18,18),new Vector2(1748,18),new Vector2(18,969),new Vector2(1748,969)})
        {Block(paper,"Deck crop horizontal",corner,new Vector2(24,2),Red);Block(paper,"Deck crop vertical",corner,new Vector2(2,14),Red);}
    }
    public void Refresh(int phase)
    {
        for(int i=0;i<3;i++)
        {
            tabs[i].color=i==phase?Red:Ink;
            captions[i].text="0"+(i+1)+" / "+(i==0?NarrativeText.L("安排","PLAN"):i==1?NarrativeText.L("版本","EDITION"):NarrativeText.L("交接","HANDOFF"));
        }
    }
}
