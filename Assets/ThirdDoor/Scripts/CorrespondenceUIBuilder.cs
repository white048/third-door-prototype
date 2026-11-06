using UnityEngine;
using UnityEngine.UI;
using static ExhibitionDesign;

public partial class SceneBootstrapper
{
    private void BuildDispatchProof(Transform paper,CorrespondenceFolio folio)
    {
        var veil=Block(paper,"Dispatch proof",Vector2.zero,new Vector2(1920,1080),new Color(.06f,.06f,.055f,.85f));
        Fullscreen(veil.rectTransform);veil.raycastTarget=true;
        var sheet=Block(veil.transform,"Proof paper",new Vector2(175,150),new Vector2(1570,780),new Color(.973f,.969f,.949f));sheet.raycastTarget=true;
        var route=Type(sheet.transform,"Proof route","",new Vector2(55,38),new Vector2(1450,55),31,Ink,chineseUiFont);
        Block(sheet.transform,"Proof red rule",new Vector2(55,110),new Vector2(1460,3),Red);
        var art=Rect(sheet.transform,"Proof cutout",new Vector2(35,185),new Vector2(420,420)).gameObject.AddComponent<RawImage>();art.raycastTarget=false;
        var words=Type(sheet.transform,"Proof actual words","",new Vector2(505,155),new Vector2(1000,255),31,Ink,chineseUiFont);
        var permission=Type(sheet.transform,"Proof permission","",new Vector2(505,430),new Vector2(1000,143),25,Red,chineseUiFont);
        var delta=Type(sheet.transform,"Proof exact cost","",new Vector2(55,590),new Vector2(1460,80),23,Muted,chineseUiFont);
        var cancel=Button(sheet.transform,"Cancel dispatch proof","",new Vector2(55,687),new Vector2(630,65),Ink,Paper,chineseUiFont);
        var confirm=Button(sheet.transform,"Confirm dispatch proof","",new Vector2(725,687),new Vector2(790,65),Red,Paper,chineseUiFont);
        confirm.GetComponentInChildren<ExhibitionText>().fontSize=24;
        folio.ConfigureProof(veil.gameObject,route,words,permission,delta,confirm,cancel,art);
    }
    private void BuildCorrespondenceUI(Transform paper,NarrativeNotebook notebook,PlayerController controller,DialogueUI dialogue)
    {
        var tab=Button(paper,"Correspondence tab","",new Vector2(930,213),new Vector2(470,54),Ink,Paper,chineseUiFont);
        var page=Rect(paper,"Correspondence page",new Vector2(100,290),new Vector2(1720,625));
        var sources=new Button[2];
        for(int i=0;i<2;i++)
        {
            sources[i]=Button(page,"Envelope source / "+i,"",new Vector2(0,i*181),new Vector2(510,164),Ink,Paper,chineseUiFont);
            var label=sources[i].GetComponentInChildren<ExhibitionText>();label.fontSize=23;label.alignment=TextAnchor.MiddleLeft;
        }
        Type(page,"Dispatch ledger heading","WORDS THAT HAVE LEFT YOUR HANDS",new Vector2(0,376),new Vector2(510,34),22,Muted);
        var ledger=Type(page,"Dispatch ledger","",new Vector2(0,422),new Vector2(510,180),21,Ink,chineseUiFont);
        var sheet=Block(page,"Open envelope",new Vector2(550,0),new Vector2(1170,625),new Color(.86f,.84f,.77f));
        var heading=Type(sheet.transform,"Envelope heading","",new Vector2(24,18),new Vector2(1120,44),25,Ink,chineseUiFont);
        var payloads=new Button[2];
        for(int i=0;i<2;i++)
        {
            payloads[i]=Button(sheet.transform,"Message payload / "+i,"",new Vector2(24+i*568,80),new Vector2(548,166),Paper,Ink,chineseUiFont);
            var label=payloads[i].GetComponentInChildren<ExhibitionText>();label.fontSize=21;label.alignment=TextAnchor.MiddleLeft;
        }
        var details=Type(sheet.transform,"Dispatch consequence","",new Vector2(24,252),new Vector2(1120,153),21,Ink,chineseUiFont);
        var reason=Type(sheet.transform,"Dispatch context","",new Vector2(24,408),new Vector2(1120,37),18,Red,chineseUiFont);
        var actions=new Button[4];
        for(int i=0;i<4;i++)
        {
            actions[i]=Button(sheet.transform,"Message action / "+(MessageAction)i,"",new Vector2(24+(i%2)*568,456+(i/2)*80),new Vector2(548,62),Ink,Paper,chineseUiFont);
            actions[i].GetComponentInChildren<ExhibitionText>().fontSize=23;
            actions[i].GetComponentInChildren<ExhibitionText>().supportRichText=true;
        }
        var folio=page.gameObject.AddComponent<CorrespondenceFolio>();
        folio.Configure(controller,dialogue,sources,payloads,actions,heading,details,reason,ledger);
        notebook.ConfigureCorrespondence(page.gameObject,tab,folio);
    }
}
