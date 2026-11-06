using UnityEngine;
using UnityEngine.UI;

public static class ExhibitionDesign
{
    public static readonly Color Paper=new Color(.91f,.885f,.81f);
    public static readonly Color Ink=new Color(.075f,.08f,.075f);
    public static readonly Color Red=new Color(.78f,.105f,.065f);
    public static readonly Color Muted=new Color(.38f,.38f,.34f);
    private static Font displayFont;
    public static Font DisplayFont
    {
        get
        {
            if(displayFont==null)displayFont=Resources.Load<Font>("Design/Display");
            if(displayFont==null)displayFont=Font.CreateDynamicFontFromOSFont(new[]{"Impact","Arial Narrow","Arial"},100);
            return displayFont;
        }
    }

    public static RectTransform Rect(Transform parent,string name,Vector2 at,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));
        var rect=go.GetComponent<RectTransform>();
        rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);
        rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(at.x,-at.y);
        rect.sizeDelta=size;
        return rect;
    }
    public static Image Block(Transform parent,string name,Vector2 at,Vector2 size,Color color,float angle=0)
    {
        var rect=Rect(parent,name,at,size);
        rect.localRotation=Quaternion.Euler(0,0,angle);
        var image=rect.gameObject.AddComponent<Image>();
        image.color=color;
        image.raycastTarget=false;
        return image;
    }
    public static ExhibitionText Type(Transform parent,string name,string value,Vector2 at,Vector2 size,int fontSize,Color color,Font font=null)
    {
        var text=Rect(parent,name,at,size).gameObject.AddComponent<ExhibitionText>();
        text.font=font!=null?font:DisplayFont;
        text.text=value;
        text.fontSize=fontSize;
        text.color=color;
        text.raycastTarget=false;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;
        text.verticalOverflow=VerticalWrapMode.Truncate;
        text.supportRichText=false;
        return text;
    }
    public static Button Button(Transform parent,string name,string value,Vector2 at,Vector2 size,Color fill,Color textColor,Font font)
    {
        var rect=Rect(parent,name,at,size);
        var image=rect.gameObject.AddComponent<Image>();
        image.color=Color.white;
        var button=rect.gameObject.AddComponent<Button>();
        button.targetGraphic=image;
        var colors=button.colors;
        colors.normalColor=fill;
        colors.highlightedColor=Color.Lerp(fill,Red,.25f);
        colors.selectedColor=colors.normalColor;
        colors.pressedColor=Red;
        colors.fadeDuration=.06f;
        colors.colorMultiplier=1f;
        button.colors=colors;
        var label=Type(rect,"Label",value,new Vector2(24,0),new Vector2(size.x-48,size.y),26,textColor,font);
        label.alignment=TextAnchor.MiddleLeft;
        return button;
    }
}

