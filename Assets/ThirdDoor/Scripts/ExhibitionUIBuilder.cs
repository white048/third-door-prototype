using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static ExhibitionDesign;

public partial class SceneBootstrapper
{
    private void BuildExhibitionUI(Transform root)
    {
        var events=new GameObject("Exhibition input",typeof(EventSystem),typeof(StandaloneInputModule));
        events.transform.SetParent(root,false);
        var canvasGO=new GameObject("Exhibition / Graphic system",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(root,false);
        canvasGO.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);
        scaler.matchWidthOrHeight=.5f;
        scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        Transform c=canvasGO.transform;
        Block(c,"Masthead",new Vector2(36,30),new Vector2(1848,58),Paper);
        Block(c,"Red registration",new Vector2(36,30),new Vector2(8,58),Red);
        Type(c,"Exhibition name","THE THIRD DOOR",new Vector2(62,38),new Vector2(340,42),30,Ink);
        var edition=Type(c,"Edition","TWO CASES   /   FIVE CARDS IN HAND   /   009",new Vector2(1310,46),new Vector2(540,30),18,Ink);
        edition.alignment=TextAnchor.UpperRight;
        var footer=Block(c,"Footer paper",new Vector2(36,1010),new Vector2(1848,70),Paper);
        Bottom(footer.rectTransform);
        var footerRule=Block(c,"Footer rule",new Vector2(36,1010),new Vector2(1848,2),Ink);
        Bottom(footerRule.rectTransform);
        var help=Type(c,"Controls",LocalizationManager.Get("help_hint"),new Vector2(42,1026),new Vector2(1430,34),18,Ink,chineseUiFont);
        var chapter=Type(c,"Chapter","01 / ARRIVAL",new Vector2(1480,1026),new Vector2(365,36),24,Ink);
        Bottom(help.rectTransform);Bottom(chapter.rectTransform);
        chapter.alignment=TextAnchor.UpperRight;

        var objectiveBg=Block(c,"Objective paper",new Vector2(36,110),new Vector2(940,125),Paper);
        Type(objectiveBg.transform,"Objective caption","CURRENT INTENTION",new Vector2(20,12),new Vector2(500,24),16,Muted);
        ExhibitionText status=Type(objectiveBg.transform,"Status Text","",new Vector2(20,40),new Vector2(900,80),23,Ink,chineseUiFont);
        var promptPaper=Block(c,"Interaction prompt paper",new Vector2(450,908),new Vector2(1020,100),Paper);
        promptPaper.gameObject.SetActive(false);
        var prompt=Type(c,"Interaction Prompt","",new Vector2(470,920),new Vector2(980,84),23,Ink,chineseUiFont);
        prompt.alignment=TextAnchor.MiddleCenter;
        Bottom(prompt.rectTransform);
        var cross=Block(c,"Focus",Vector2.zero,new Vector2(5,5),Red);
        var crossRect=cross.rectTransform;
        crossRect.anchorMin=crossRect.anchorMax=crossRect.pivot=new Vector2(.5f,.5f);
        crossRect.anchoredPosition=Vector2.zero;

        // A paper transcript leaves the upper part of the installation visible.
        var panel=Block(c,"Dialogue Panel",new Vector2(190,330),new Vector2(1540,650),Paper);
        panel.raycastTarget=true;
        Bottom(panel.rectTransform);
        panel.rectTransform.anchorMin=panel.rectTransform.anchorMax=new Vector2(.5f,0);
        panel.rectTransform.anchoredPosition=new Vector2(-770,750);
        var group=panel.gameObject.AddComponent<CanvasGroup>();
        var bar=Block(panel.transform,"Speaker Color Bar",Vector2.zero,new Vector2(12,650),Red);
        var outline=Block(panel.transform,"Panel Top Accent",Vector2.zero,new Vector2(1540,3),Ink);
        ExhibitionText speaker=Type(panel.transform,"Speaker","",new Vector2(40,22),new Vector2(930,50),26,Ink,chineseUiFont);
        var underline=Block(panel.transform,"Speaker Underline",new Vector2(40,85),new Vector2(70,3),Red);
        ExhibitionText line=Type(panel.transform,"Line","",new Vector2(40,105),new Vector2(920,120),28,Ink,chineseUiFont);
        ExhibitionText result=Type(panel.transform,"Result","",new Vector2(40,242),new Vector2(920,240),26,Ink,chineseUiFont);
        var buttons=new Button[3];var labels=new ExhibitionText[3];
        for(int i=0;i<3;i++)
        {
            buttons[i]=Button(panel.transform,"Option "+(i+1),"",new Vector2(40,235+i*94),new Vector2(920,84),new Color(.84f,.82f,.75f),Ink,chineseUiFont);
            labels[i]=buttons[i].GetComponentInChildren<ExhibitionText>();
            labels[i].fontSize=24;
        }
        // Ending card is anchored to the bottom left so the opened exit stays visible.
        var endingCard=Block(c,"Ending paper",new Vector2(60,605),new Vector2(1190,360),Paper);
        Bottom(endingCard.rectTransform);
        var endingText=Type(endingCard.transform,"Ending Text","",new Vector2(34,28),new Vector2(1120,310),28,Ink,chineseUiFont);
        endingText.lineSpacing=1.25f;
        dialogueUI=canvasGO.AddComponent<DialogueUI>();
        dialogueUI.Configure(group,speaker,line,result,buttons,labels,status,endingText,player,bar,underline,outline);
        var next=Button(panel.transform,"Continue feedback","",new Vector2(40,520),new Vector2(920,58),Ink,Paper,chineseUiFont);
        var delta=Type(panel.transform,"Resource change","",new Vector2(40,480),new Vector2(920,34),22,Red,chineseUiFont);
        var hint=Type(panel.transform,"Conversation input hint","",new Vector2(40,603),new Vector2(1430,32),19,Muted,chineseUiFont);
        dialogueUI.ConfigureFeedback(next,delta,hint);
        BuildNarrativeCards(panel.transform,dialogueUI);
        BuildArtworkReflection(panel.transform,dialogueUI);
        canvasGO.AddComponent<InteractionPrompt>().Configure(player,prompt,promptPaper.gameObject);
        var hud=canvasGO.AddComponent<ExhibitionHUD>();
        hud.Configure(player,help,chapter,endingCard.gameObject,cross.gameObject,objectiveBg.gameObject,chineseUiFont);
        BuildResourceReadout(c);

        // Pause is built before the title, so the opening poster always has visual priority.
        var pause=Block(c,"Pause paper",Vector2.zero,new Vector2(1920,1080),Paper);
        Fullscreen(pause.rectTransform);
        pause.raycastTarget=true;
        Type(pause.transform,"Pause edition","03 / INTERMISSION",new Vector2(110,95),new Vector2(1000,80),42,Red);
        Type(pause.transform,"Pause title","TAKE YOUR\nTIME.",new Vector2(100,210),new Vector2(1100,390),150,Ink);
        var resume=Button(pause.transform,"Resume",LocalizationManager.Current==Language.Chinese?"继续   /   RESUME":"RESUME",new Vector2(110,620),new Vector2(580,76),Ink,Paper,chineseUiFont);
        var restart=Button(pause.transform,"Restart",""+(LocalizationManager.Current==Language.Chinese?"重新开始":"RESTART"),new Vector2(110,710),new Vector2(580,76),Ink,Paper,chineseUiFont);
        var titleReturn=Button(pause.transform,"Return to title","",new Vector2(110,800),new Vector2(580,76),Ink,Paper,chineseUiFont);
        var quit=Button(pause.transform,"Quit",LocalizationManager.Current==Language.Chinese?"退出游戏   /   QUIT":"QUIT",new Vector2(110,890),new Vector2(580,76),Ink,Paper,chineseUiFont);
        var sensitivityLabel=Type(pause.transform,"Sensitivity label","",new Vector2(1080,600),new Vector2(650,45),25,Ink,chineseUiFont);
        var slower=Button(pause.transform,"Slower","-",new Vector2(1080,670),new Vector2(110,70),Ink,Paper,chineseUiFont);
        var faster=Button(pause.transform,"Faster","+",new Vector2(1210,670),new Vector2(110,70),Ink,Paper,chineseUiFont);
        var audio=Button(pause.transform,"Audio","",new Vector2(1080,780),new Vector2(600,70),Ink,Paper,chineseUiFont);
        hud.ConfigurePause(pause.gameObject,resume,restart,quit,slower,faster,sensitivityLabel,audio);
        var language=Button(pause.transform,"Language setting","",new Vector2(1080,882),new Vector2(600,70),Ink,Paper,chineseUiFont);
        hud.ConfigureLanguage(language);
        BuildCaseNavigation(c,hud,titleReturn);

        var title=Block(c,"Intro Black Overlay",Vector2.zero,new Vector2(1920,1080),Paper);
        Fullscreen(title.rectTransform);
        title.raycastTarget=true;
        var titleGroup=title.gameObject.AddComponent<CanvasGroup>();
        var poster=Rect(title.transform,"Language Picker",Vector2.zero,new Vector2(1920,1080));
        Block(poster,"Top rule",new Vector2(88,74),new Vector2(1744,3),Ink);
        Type(poster,"Title masthead","AN INTERACTIVE SPATIAL ESSAY",new Vector2(90,95),new Vector2(1050,38),24,Ink);
        Type(poster,"Title index","VOL. 07   /   2026",new Vector2(1540,95),new Vector2(300,38),24,Ink);
        Type(poster,"Title","THE\nTHIRD\nDOOR",new Vector2(84,182),new Vector2(900,565),166,Ink).lineSpacing=.82f;
        Block(poster,"Title red rule",new Vector2(92,762),new Vector2(76,8),Red);
        var home=Button(poster,"Case Home","",new Vector2(92,790),new Vector2(395,48),Ink,Paper,chineseUiFont);
        var office=Button(poster,"Case Office","",new Vector2(507,790),new Vector2(405,48),Red,Paper,chineseUiFont);
        home.GetComponentInChildren<ExhibitionText>().fontSize=21;office.GetComponentInChildren<ExhibitionText>().fontSize=21;
        var titleSub=Type(poster,"Picker Title","",new Vector2(92,852),new Vector2(850,40),25,Ink,chineseUiFont);
        var brief=Type(poster,"Case brief","",new Vector2(92,898),new Vector2(850,48),19,Muted,chineseUiFont);
        var btnCN=Button(poster,"Btn Chinese","1   /   中文进入",new Vector2(92,952),new Vector2(350,58),Ink,Paper,chineseUiFont);
        var btnEN=Button(poster,"Btn English","2   /   ENTER IN ENGLISH",new Vector2(462,952),new Vector2(450,58),Red,Paper,chineseUiFont);
        Type(poster,"Title footnote","TWO VOICES   /   WORDS IN YOUR HANDS   /   YOUR OWN WAY OUT",new Vector2(92,1033),new Vector2(850,24),16,Muted);
        DrawTitlePoster(poster);
        var continueSaved=Button(poster,"Continue saved journey","",new Vector2(960,1000),new Vector2(460,43),Ink,Paper,chineseUiFont);
        continueSaved.GetComponentInChildren<ExhibitionText>().fontSize=20;
        var introText=Type(title.transform,"Intro Text","",new Vector2(865,330),new Vector2(890,430),42,Ink,chineseUiFont);
        introText.alignment=TextAnchor.MiddleLeft;
        introText.lineSpacing=1.35f;
        var skip=Type(title.transform,"Intro Skip Hint","",new Vector2(865,830),new Vector2(890,46),22,Muted,chineseUiFont);
        var introArtworkRoot=Rect(title.transform,"Introduction art",Vector2.zero,new Vector2(1920,1080));
        BuildCasePlate(introArtworkRoot,"Introduction case plate",new Vector2(160,220),new Vector2(620,620),null,true);
        var intro=title.gameObject.AddComponent<IntroSequence>();
        intro.Configure(titleGroup,introText,skip,player,poster.gameObject,titleSub,btnCN,btnEN);
        intro.ConfigureCases(home,office,brief,continueSaved);
        intro.ConfigureArtwork(introArtworkRoot.gameObject);
        BuildNotebook(c,poster,player,dialogueUI);
    }

    private void DrawTitlePoster(Transform parent)
    {
        var artwork=Rect(parent,"Spatial poster",new Vector2(1000,155),new Vector2(820,815));
        Shape(artwork,"Registration circle",new Vector2(80,0),new Vector2(700,700),ExhibitionGraphic.Shape.Ring,Ink);
        Shape(artwork,"Vermilion disc",new Vector2(220,90),new Vector2(560,560),ExhibitionGraphic.Shape.Disc,Red);
        Block(artwork,"Diagonal",new Vector2(-10,610),new Vector2(900,88),Ink,31);
        Block(artwork,"Paper cut",new Vector2(128,290),new Vector2(110,510),Paper,-14);
        Block(artwork,"Door 01",new Vector2(210,392),new Vector2(125,368),Ink,-14);
        Block(artwork,"Door 02",new Vector2(382,346),new Vector2(128,370),Paper,-14);
        Block(artwork,"Door 03",new Vector2(548,304),new Vector2(128,370),Ink,-14);
        Type(artwork,"Poster numeral","03",new Vector2(605,442),new Vector2(130,140),90,Paper);
        Type(artwork,"Poster caption","THE SPACE\nBETWEEN US.",new Vector2(28,745),new Vector2(420,88),35,Ink);
        Block(artwork,"Baseline",new Vector2(28,850),new Vector2(750,2),Ink);
        BuildCasePlate(parent,"Title case plate",new Vector2(1030,160),new Vector2(760,760),artwork.gameObject,true);
        var grain=Shape(parent,"Paper grain",Vector2.zero,new Vector2(1920,1080),ExhibitionGraphic.Shape.Grain,new Color(.1f,.08f,.05f,.12f));
        grain.raycastTarget=false;
    }

    private ExhibitionGraphic Shape(Transform parent,string name,Vector2 at,Vector2 size,ExhibitionGraphic.Shape shape,Color color)
    {
        var graphic=Rect(parent,name,at,size).gameObject.AddComponent<ExhibitionGraphic>();
        graphic.shape=shape;graphic.color=color;graphic.raycastTarget=false;
        return graphic;
    }

    private static void Bottom(RectTransform rect)
    {
        Vector2 at=rect.anchoredPosition;
        rect.anchorMin=rect.anchorMax=Vector2.zero;
        rect.anchoredPosition=new Vector2(at.x,1080+at.y);
    }

    private static void Fullscreen(RectTransform rect)
    {
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
        rect.offsetMin=rect.offsetMax=Vector2.zero;
    }
}
