using UnityEngine;
using UnityEngine.UI;
using static ExhibitionDesign;

public partial class SceneBootstrapper
{
    private void BuildCaseDeck(Transform paper,NarrativeNotebook book,PlayerController controller,DialogueUI dialogue)
    {
        var veil=Block(paper,"Case deck builder",Vector2.zero,new Vector2(1920,1080),new Color(.04f,.04f,.035f,.96f));Fullscreen(veil.rectTransform);veil.raycastTarget=true;
        var sheet=Block(veil.transform,"Deck paper",new Vector2(65,45),new Vector2(1790,990),Paper);sheet.raycastTarget=true;
        var title=Type(sheet.transform,"Deck heading","",new Vector2(35,22),new Vector2(1720,58),36,Ink,chineseUiFont);
        var rule=Type(sheet.transform,"Deck rules","",new Vector2(35,85),new Vector2(1720,45),22,Muted,chineseUiFont);
        var presets=new Button[3];
        for(int i=0;i<3;i++){presets[i]=Button(sheet.transform,"Deck preset / "+i,"",new Vector2(35+i*365,145),new Vector2(345,55),Ink,Paper,chineseUiFont);presets[i].GetComponentInChildren<ExhibitionText>().fontSize=23;}
        var cards=new Button[9];
        for(int i=0;i<9;i++)
        {
            cards[i]=Button(sheet.transform,"Deck choice / "+(DispatchTool)(i+1),"",new Vector2(35+(i%3)*365,225+(i/3)*195),new Vector2(345,178),Ink,Paper,chineseUiFont);
            var label=cards[i].GetComponentInChildren<ExhibitionText>();label.fontSize=20;label.alignment=TextAnchor.UpperLeft;label.rectTransform.anchoredPosition=new Vector2(18,-12);label.rectTransform.sizeDelta=new Vector2(309,154);
        }
        Block(sheet.transform,"Deck side rule",new Vector2(1150,225),new Vector2(3,570),Red);
        var hand=Type(sheet.transform,"Deck selected hand","",new Vector2(1190,225),new Vector2(530,250),25,Ink,chineseUiFont);
        var hint=Type(sheet.transform,"Deck combinations","",new Vector2(1190,490),new Vector2(530,310),21,Muted,chineseUiFont);
        var count=Type(sheet.transform,"Deck selected count","",new Vector2(35,825),new Vector2(1680,42),23,Red,chineseUiFont);
        var back=Button(sheet.transform,"Close case deck","",new Vector2(35,895),new Vector2(590,62),Ink,Paper,chineseUiFont);
        var confirm=Button(sheet.transform,"Confirm case deck","",new Vector2(1190,895),new Vector2(530,62),Red,Paper,chineseUiFont);
        var view=veil.gameObject.AddComponent<CaseDeckPanel>();view.Configure(veil.gameObject,book,controller,dialogue,cards,presets,confirm,back,title,rule,hand,hint,count);book.ConfigureDeck(view);
    }
}
