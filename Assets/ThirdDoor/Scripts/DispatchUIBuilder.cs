using UnityEngine;
using UnityEngine.UI;
using static ExhibitionDesign;

public partial class SceneBootstrapper
{
    private void BuildDispatchComposer(Transform paper,CorrespondenceFolio folio,PlayerController controller)
    {
        var veil=Block(paper,"Dispatch composer",Vector2.zero,new Vector2(1920,1080),new Color(.04f,.04f,.035f,.95f));Fullscreen(veil.rectTransform);veil.raycastTarget=true;
        var sheet=Block(veil.transform,"Composer paper",new Vector2(65,45),new Vector2(1790,990),Paper);sheet.raycastTarget=true;
        var heading=Type(sheet.transform,"Composer heading","",new Vector2(35,22),new Vector2(1720,60),34,Ink,chineseUiFont);
        Block(sheet.transform,"Composer red rule",new Vector2(35,90),new Vector2(1720,5),Red);
        var actor=Type(sheet.transform,"Recipient interjection","",new Vector2(35,110),new Vector2(810,105),23,Red,chineseUiFont);
        var status=Type(sheet.transform,"Packet progress","",new Vector2(925,110),new Vector2(810,100),23,Ink,chineseUiFont);
        var parts=new Button[4];
        for(int i=0;i<4;i++)
        {
            parts[i]=Button(sheet.transform,"Dispatch fragment / "+(DispatchFragment)(i+1),"",new Vector2(35+(i%2)*415,225+(i/2)*175),new Vector2(395,155),Ink,Paper,chineseUiFont);
            var label=parts[i].GetComponentInChildren<ExhibitionText>();label.fontSize=22;label.alignment=TextAnchor.MiddleLeft;
        }
        var packet=Type(sheet.transform,"Composed actual words","",new Vector2(925,240),new Vector2(810,175),27,Ink,chineseUiFont);
        var reaction=Type(sheet.transform,"Recipient actual response","",new Vector2(925,445),new Vector2(810,225),24,Red,chineseUiFont);
        var tools=new Button[7];
        for(int i=0;i<7;i++)
        {
            tools[i]=Button(sheet.transform,"Dispatch tool / "+(DispatchTool)i,"",new Vector2(35+(i%4)*205,590+(i/4)*62),new Vector2(190,52),Ink,Paper,chineseUiFont);
            var label=tools[i].GetComponentInChildren<ExhibitionText>();label.fontSize=18;
            label.rectTransform.anchoredPosition=new Vector2(14,0);label.rectTransform.sizeDelta=new Vector2(162,52);
        }
        var cost=Type(sheet.transform,"Composer cost","",new Vector2(35,725),new Vector2(810,125),22,Muted,chineseUiFont);
        var help=Type(sheet.transform,"Composer help","",new Vector2(925,705),new Vector2(810,125),22,Muted,chineseUiFont);
        var clear=Button(sheet.transform,"Clear dispatch order","",new Vector2(35,870),new Vector2(260,67),Ink,Paper,chineseUiFont);
        var close=Button(sheet.transform,"Close dispatch composer","",new Vector2(315,870),new Vector2(535,67),Ink,Paper,chineseUiFont);
        var send=Button(sheet.transform,"Proof composed packet","",new Vector2(925,870),new Vector2(810,67),Red,Paper,chineseUiFont);
        var composer=veil.gameObject.AddComponent<DispatchComposer>();
        composer.Configure(veil.gameObject,folio,controller,parts,tools,send,close,clear,heading,actor,status,packet,cost,reaction,help);folio.ConfigureComposer(composer);
    }
}
