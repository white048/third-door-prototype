using UnityEngine;
using UnityEngine.UI;
using static ExhibitionDesign;

public partial class SceneBootstrapper
{
    private void BuildRibbonStation(Transform installation)
    {
        var stand=new GameObject("A sentence gets heavy / physical strip").transform;
        stand.SetParent(installation,false);stand.position=new Vector3(4.7f,0,-2.5f);stand.rotation=Quaternion.Euler(0,38,0);
        StripColliders(LocalBox("Strip / base",stand,new Vector3(0,.15f,.12f),new Vector3(2,.18f,.8f),printInk));
        StripColliders(LocalBox("Strip / support",stand,new Vector3(0,1.5f,.16f),new Vector3(.11f,2.8f,.12f),printInk));
        StripColliders(LocalBox("Strip / unrolled paper",stand,new Vector3(0,2.3f,0),new Vector3(1.7f,2.95f,.045f),printPaper));
        foreach(float y in new[]{.79f,3.81f})
        {
            var roll=GameObject.CreatePrimitive(PrimitiveType.Cylinder);roll.name="Strip / paper roll";roll.transform.SetParent(stand,false);
            roll.transform.localPosition=new Vector3(0,y,-.01f);roll.transform.localScale=new Vector3(.16f,.91f,.16f);roll.transform.localRotation=Quaternion.Euler(0,0,90);
            roll.GetComponent<Renderer>().sharedMaterial=printPaper;StripColliders(roll);
        }
        var heading=WorldPrint("WORDS IN TRANSIT",Vector3.zero,.09f,Ink,stand);heading.transform.localPosition=new Vector3(0,3.51f,-.07f);heading.transform.localRotation=Quaternion.identity;
        var count=WorldPrint("",Vector3.zero,.075f,Ink,stand).GetComponent<TextMesh>();count.transform.localPosition=new Vector3(0,1.12f,-.07f);count.transform.localRotation=Quaternion.identity;
        var cue=WorldPrint("E / UNROLL",Vector3.zero,.105f,Red,stand);cue.transform.localPosition=new Vector3(0,.5f,-.18f);cue.transform.localRotation=Quaternion.identity;
        var canvas=new GameObject("Strip / photographic fragments",typeof(RectTransform),typeof(Canvas));canvas.transform.SetParent(stand,false);
        var canvasRect=canvas.GetComponent<RectTransform>();canvasRect.sizeDelta=new Vector2(250,360);canvasRect.localPosition=new Vector3(0,2.42f,-.06f);canvasRect.localScale=Vector3.one*.006f;
        canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        var shader=Shader.Find("UI/Default");Material ink=shader==null?null:new Material(shader);
        if(ink!=null){ink.SetInt("unity_GUIZTestMode",(int)UnityEngine.Rendering.CompareFunction.LessEqual);stand.gameObject.AddComponent<ExhibitionResourceLifetime>().Own(ink);}
        var photos=new RawImage[6];
        for(int i=0;i<photos.Length;i++)
        {
            photos[i]=Rect(canvasRect,"Strip / envelope "+i,new Vector2(30+(i%2)*8,8+i*25),new Vector2(190,190)).gameObject.AddComponent<RawImage>();
            photos[i].raycastTarget=false;if(ink!=null)photos[i].material=ink;
            photos[i].rectTransform.localRotation=Quaternion.Euler(0,0,i%2==0?-7:6);
        }
        var collider=stand.gameObject.AddComponent<BoxCollider>();collider.isTrigger=true;collider.center=new Vector3(0,2.1f,0);collider.size=new Vector3(1.9f,3.8f,.32f);
        stand.gameObject.AddComponent<NarrativeRibbonStation>().Configure(photos,count);
    }
    private void BuildNarrativeRibbon(Transform paper,NarrativeNotebook notebook)
    {
        var tab=Button(paper,"Collage ribbon tab","",new Vector2(1420,213),new Vector2(410,54),Ink,Paper,chineseUiFont);
        var page=Rect(paper,"Collage ribbon page",new Vector2(100,290),new Vector2(1720,625));
        var nav=new Button[4];
        for(int i=0;i<4;i++)
        {
            nav[i]=Button(page,"Ribbon chapter / "+i,i<3?"0"+(i+1):"",new Vector2(0,i*80),new Vector2(230,64),i==3?Red:Ink,Paper,chineseUiFont);
            nav[i].GetComponentInChildren<ExhibitionText>().fontSize=i==3?18:25;
        }
        var progress=Type(page,"Ribbon reading progress","",new Vector2(8,375),new Vector2(220,200),23,Muted,chineseUiFont);
        var window=Block(page,"Ribbon viewport",new Vector2(260,0),new Vector2(1400,625),new Color(.973f,.969f,.949f));
        window.raycastTarget=true;window.gameObject.AddComponent<RectMask2D>();
        var content=Rect(window.transform,"Ribbon content",Vector2.zero,new Vector2(1360,625));
        var reader=window.gameObject.AddComponent<ScrollRect>();
        reader.viewport=window.rectTransform;reader.content=content;reader.horizontal=false;reader.vertical=true;
        reader.movementType=ScrollRect.MovementType.Clamped;reader.inertia=false;reader.scrollSensitivity=95;
        var track=Block(page,"Ribbon scroll track",new Vector2(1687,0),new Vector2(14,625),new Color(.82f,.80f,.75f));track.raycastTarget=true;
        var handle=Block(track.transform,"Ribbon scroll handle",Vector2.zero,Vector2.zero,Red);handle.raycastTarget=true;
        var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;
        scrollbar.direction=Scrollbar.Direction.BottomToTop;scrollbar.navigation=new Navigation{mode=Navigation.Mode.None};
        reader.verticalScrollbar=scrollbar;
        var ribbon=page.gameObject.AddComponent<NarrativeRibbon>();ribbon.Configure(reader,window.rectTransform,content,progress,nav,chineseUiFont);
        notebook.ConfigureRibbon(page.gameObject,tab,ribbon);
    }
}
