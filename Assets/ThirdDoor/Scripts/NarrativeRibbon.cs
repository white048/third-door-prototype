using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ExhibitionDesign;

// The strip reads existing records. It never resolves a choice or changes a save.
public sealed class NarrativeRibbon : MonoBehaviour
{
    private sealed class Sheet { public RectTransform Rect; public float Top,Height; public int Phase; }
    private readonly List<Sheet> sheets=new List<Sheet>();
    private readonly float[] chapterOffsets=new float[4];
    private readonly List<GameObject> retired=new List<GameObject>();
    private RectTransform content,viewport;
    private ScrollRect scroll;
    private ExhibitionText position;
    private Button[] chapters;
    private Font bodyFont;
    private int revision=-1;
    private Language language;
    private CaseId caseId;
    private float length;
    private static readonly Color White=new Color(.973f,.969f,.949f);
    public int ResponseCount { get; private set; }
    public int ReceiptCount { get; private set; }
    public int VisibleSheets { get; private set; }
    public float ReadingPosition=>scroll==null?0:1-scroll.verticalNormalizedPosition;
    public bool UsesCutouts=>IllustrationLibrary.Load("ObjectCutouts-v7")!=null;

    public void Configure(ScrollRect reader,RectTransform window,RectTransform pages,ExhibitionText progress,Button[] jumps,Font font)
    {
        scroll=reader;viewport=window;content=pages;position=progress;chapters=jumps;bodyFont=font;
        for(int i=0;i<chapters.Length;i++){int chapter=i;chapters[i].onClick.AddListener(()=>JumpToChapter(chapter));}
        scroll.onValueChanged.AddListener(_=>RefreshVisibility());
    }
    public void Refresh(RelationshipRun run)
    {
        if(run==null||scroll==null)return;
        if(revision==run.Revision&&language==LocalizationManager.Current&&caseId==run.Case)return;
        float preserved=revision<0?0:ReadingPosition;
        revision=run.Revision;language=LocalizationManager.Current;caseId=run.Case;
        foreach(var sheet in sheets){sheet.Rect.gameObject.SetActive(false);retired.Add(sheet.Rect.gameObject);}
        sheets.Clear();length=0;ResponseCount=ReceiptCount=0;
        foreach(var old in retired)Destroy(old);retired.Clear();
        Cover(run);
        for(int phase=0;phase<3;phase++)
        {
            chapterOffsets[phase]=length;
            RoundHeading(run,phase);
            foreach(var record in run.History)if(record.Phase==phase){Response(record);ResponseCount++;}
            bool receiptHeading=false;
            foreach(var receipt in run.Correspondence)if(receipt.Phase==phase)
            {
                if(!receiptHeading){ReceiptHeading(phase);receiptHeading=true;}
                Receipt(run,receipt);ReceiptCount++;
            }
        }
        chapterOffsets[3]=length;Closing(run);
        content.sizeDelta=new Vector2(1360,length);
        SetReadingPosition(preserved);
        for(int i=0;i<3;i++)chapters[i].interactable=i<=run.Phase;
        chapters[3].GetComponentInChildren<ExhibitionText>().text=NarrativeText.L("纸带末端","THE END OF THE STRIP");
        RefreshVisibility();
    }
    public void SetReadingPosition(float value)
    {if(scroll==null)return;scroll.StopMovement();scroll.verticalNormalizedPosition=1-Mathf.Clamp01(value);RefreshVisibility();}
    public void JumpToChapter(int phase)
    {if(scroll==null)return;SetReadingPosition(chapterOffsets[Mathf.Clamp(phase,0,3)]/Mathf.Max(1,length-viewport.rect.height));}
    private void Update()
    {
        if(!NarrativeNotebook.Open||ExhibitionHUD.Paused)return;
        float direction=(Input.GetKey(KeyCode.DownArrow)?1:0)-(Input.GetKey(KeyCode.UpArrow)?1:0);
        if(direction!=0)SetReadingPosition(ReadingPosition+direction*520*Time.unscaledDeltaTime/Mathf.Max(1,length-viewport.rect.height));
        RefreshVisibility();
    }
    private void RefreshVisibility()
    {
        if(scroll==null)return;
        float offset=ReadingPosition*Mathf.Max(0,length-viewport.rect.height);VisibleSheets=0;
        foreach(var sheet in sheets)
        {
            bool visible=sheet.Top+sheet.Height>=offset-50&&sheet.Top<=offset+viewport.rect.height+50;
            if(sheet.Rect.gameObject.activeSelf!=visible)sheet.Rect.gameObject.SetActive(visible);
            if(visible)VisibleSheets++;
        }
        if(position!=null)position.text=NarrativeText.L("已读 ","READ ")+Mathf.RoundToInt(ReadingPosition*100)+"%\n"+NarrativeText.L("滚轮 / ↑ ↓\n左侧跳转轮次","WHEEL / ↑ ↓\nJUMP BY ROUND");
    }
    private RectTransform NewSheet(string name,float height,Color color,int phase=-1)
    {
        var root=Block(content,name,new Vector2(0,length),new Vector2(1360,height),color);
        sheets.Add(new Sheet{Rect=root.rectTransform,Top=length,Height=height,Phase=phase});length+=height;
        return root.rectTransform;
    }
    private ExhibitionText Words(Transform parent,string name,string value,float x,float y,float width,float height,int size,Color color)
    {return Type(parent,name,value,new Vector2(x,y),new Vector2(width,height),size,color,bodyFont);}
    private void Caption(Transform parent,string value,float x,float y,Color color)
    {Words(parent,"Ribbon caption",value,x,y,1080,45,20,color);}
    private void Photo(Transform parent,int tile,Vector2 at,Vector2 size,float angle=0,float opacity=1)
    {
        var texture=IllustrationLibrary.Load("ObjectCutouts-v7");
        if(texture==null)
        {
            var envelope=Block(parent,"Ribbon geometric fallback",at+size*.18f,size*.64f,tile==2?Ink:new Color(.83f,.81f,.75f),angle);
            Block(envelope.transform,"Fallback fold",new Vector2(12,12),new Vector2(Mathf.Max(1,size.x*.64f-24),3),Red);
            return;
        }
        var rect=Rect(parent,"Ribbon cutout / "+tile,at,size);rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition+=new Vector2(size.x*.5f,-size.y*.5f);rect.localRotation=Quaternion.Euler(0,0,angle);
        var image=rect.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
        image.uvRect=new Rect((tile%3)/3f,(2-tile/3)/3f,1/3f,1/3f);image.color=new Color(1,1,1,opacity);
    }
    private void Cover(RelationshipRun run)
    {
        var root=NewSheet("Ribbon cover",720,White);
        Caption(root,"THE THIRD DOOR / A STRIP FROM THIS RUN",70,35,Muted);
        Words(root,"Ribbon case",CaseInfo.Title(run.Case),70,110,860,110,54,Ink);
        Words(root,"Ribbon opening",NarrativeText.L("一开始，\n只是请你带一句话。","At first,\nit was one message."),100,290,660,170,37,Ink);
        Photo(root,0,new Vector2(820,190),new Vector2(420,420),-9);
        Caption(root,NarrativeText.L("往下读，看看后来多了什么。","Read on to see what you took on."),100,590,Muted);
        Block(root,"Ribbon opening thread",new Vector2(114,664),new Vector2(1100,2),Red);
    }
    private void RoundHeading(RelationshipRun run,int phase)
    {
        var root=NewSheet("Ribbon round / "+phase,300,White,phase);
        Words(root,"Ribbon round number","0"+(phase+1),70,55,220,130,84,Red);
        Words(root,"Ribbon round title",NarrativeText.PhaseName(phase),355,70,920,80,34,Ink);
        bool heard=false;foreach(var record in run.History)if(record.Phase==phase)heard=true;
        string line=heard?NarrativeText.L("你听见的话，和你给的回应。","What you heard, and how you answered."):phase>run.Phase?NarrativeText.L("还没有发生。","This round has not happened yet."):NarrativeText.L("这一轮还没有回应记录。","No response recorded for this round yet.");
        Words(root,"Ribbon round note",line,355,170,920,80,25,Muted);
    }
    private void Response(ConversationRecord record)
    {
        bool dark=record.LoadAfter>=6||record.TooExhaustedToListen;
        Color paper=dark?Ink:White,ink=dark?White:Ink,muted=dark?new Color(.74f,.72f,.67f):Muted;
        var root=NewSheet("Ribbon response / "+ResponseCount,1010,paper,record.Phase);
        Caption(root,NarrativeText.Speaker(record.Voice),70,20,muted);
        bool scope=caseId==CaseId.Office&&record.Voice==1&&record.Option==2;
        int tile=scope?4:record.Option==0?0:record.Option==1?(caseId==CaseId.Office&&record.Voice==1?2:1):7;
        int count=record.Option==0?Mathf.Clamp(record.LoadAfter,1,6):1;
        for(int i=0;i<count;i++)Photo(root,tile,new Vector2(65+(i%2)*24,155+i*44),new Vector2(435,435),i%2==0?-6:7);
        if(record.LoadAfter>=6)Photo(root,3,new Vector2(200,430),new Vector2(255,255),8);
        if(record.Card==ConversationCard.Pause)Photo(root,8,new Vector2(175,475),new Vector2(240,240));
        Words(root,"Ribbon object note",record.TooExhaustedToListen?NarrativeText.L("这一次，已经听不进去了。","This time, you could not take in any more."):NarrativeText.L("回应后承重 ","LOAD AFTER THIS RESPONSE ")+record.LoadAfter+" / 8",70,760,440,135,27,ink);
        Words(root,"Ribbon source label",NarrativeText.L("对方说","THEY SAID"),600,80,680,40,19,muted);
        Words(root,"Ribbon source",NarrativeText.RecordedLine(record),600,137,680,170,27,ink);
        Words(root,"Ribbon reply label",NarrativeText.L("你说","YOU SAID"),600,336,680,40,19,dark?White:Red);
        Words(root,"Ribbon reply",NarrativeText.RecordReply(record),600,390,680,153,28,ink);
        Block(root,"Ribbon response rule",new Vector2(600,570),new Vector2(680,2),dark?White:Red);
        Words(root,"Ribbon consequence",NarrativeText.Feedback(record),600,607,680,285,23,ink);
        Words(root,"Ribbon response cost",NarrativeText.Delta(record.CapacityAfter-record.CapacityBefore,record.LoadAfter-record.LoadBefore,record.BridgeAfter-record.BridgeBefore),70,931,1210,55,22,muted);
    }
    private void ReceiptHeading(int phase)
    {
        var root=NewSheet("Ribbon delivered heading / "+phase,150,White,phase);
        Words(root,"Ribbon delivery section",NarrativeText.L("这一轮，门前还发生了这些事。","At the doors, this round."),80,45,1200,75,30,Ink);
    }
    private void Receipt(RelationshipRun run,CorrespondenceEvent receipt)
    {
        var root=NewSheet("Ribbon receipt / "+ReceiptCount,820,White,receipt.Phase);
        int tile=receipt.Action==MessageAction.Deliver?(receipt.Payload==MessagePayload.Need?2:0):receipt.Action==MessageAction.Return?8:receipt.Action==MessageAction.Repair?5:4;
        Photo(root,tile,new Vector2(65,135),new Vector2(440,440),receipt.Breach?12:-5);
        Caption(root,CorrespondenceText.Source(receipt.Source)+" / "+CorrespondenceText.ActionName(receipt.Action),70,27,Red);
        var item=run.Message(receipt.MessageId);
        string words=receipt.Packet?DispatchText.Packet(run.Case,receipt.Source,receipt.First,receipt.Second,receipt.Tool,receipt.Phase,receipt.ConsentRenewed):receipt.Action==MessageAction.Deliver&&item!=null?(receipt.Payload==MessagePayload.Request?CorrespondenceText.Request(item):CorrespondenceText.Need(item)):NarrativeText.L("记录本人实际确认的内容，不代替另一人的答复。","Record what this person confirmed. It does not answer for anyone else.");
        Words(root,"Ribbon delivered words",words,600,117,680,210,28,Ink);
        Words(root,"Ribbon receipt reply",CorrespondenceText.Outcome(receipt),600,376,680,270,25,Ink);
        Words(root,"Ribbon receipt costs",CorrespondenceText.Delta(receipt),70,688,1210,105,22,Muted);
    }
    private void Closing(RelationshipRun run)
    {
        bool heavy=run.Ending==RelationshipEnding.Loop||run.Ending==RelationshipEnding.Exhaustion||run.Ending==RelationshipEnding.FalsePeace;
        var root=NewSheet("Ribbon closing",850,heavy?Ink:White);Color ink=heavy?White:Ink;
        switch(run.Ending)
        {
            case RelationshipEnding.SharedTable:
                Photo(root,1,new Vector2(735,155),new Vector2(285,285),-22);
                Photo(root,1,new Vector2(990,250),new Vector2(285,285),160);
                Photo(root,6,new Vector2(850,390),new Vector2(240,240));break;
            case RelationshipEnding.Boundary:
                Photo(root,8,new Vector2(740,285),new Vector2(350,350),-10);
                Photo(root,0,new Vector2(975,155),new Vector2(275,275),12);break;
            case RelationshipEnding.Loop:
                for(int i=0;i<3;i++)Photo(root,1,new Vector2(750+i*65,130+i*135),new Vector2(300,300),i%2==0?-18:155);break;
            case RelationshipEnding.Alliance:
                Photo(root,0,new Vector2(745,195),new Vector2(300,300),-14);
                Photo(root,8,new Vector2(800,350),new Vector2(340,340));
                Photo(root,2,new Vector2(1020,95),new Vector2(250,250),18);break;
            case RelationshipEnding.Exhaustion:
                Photo(root,1,new Vector2(735,235),new Vector2(485,485),75);
                Photo(root,3,new Vector2(880,200),new Vector2(320,320));break;
            case RelationshipEnding.FalsePeace:
                for(int i=0;i<4;i++)Photo(root,5,new Vector2(745+i*20,180+i*85),new Vector2(420,240),i%2==0?-4:5);
                Photo(root,4,new Vector2(875,120),new Vector2(210,210));break;
            default:Photo(root,7,new Vector2(760,130),new Vector2(470,470));break;
        }
        Words(root,"Ribbon closing title",run.Ended?NarrativeText.EndingTitle(run.Ending):NarrativeText.L("这张纸还没写完。","The strip is still being written."),75,100,700,155,41,ink);
        Words(root,"Ribbon closing body",run.Ended?NarrativeText.EndingBody(run.Ending):NarrativeText.L("回到门前，决定下一句怎么说。\n已听过的话会留在这里。","Return to the doors and choose what to say next.\nWhat you have heard will stay here."),75,310,620,280,29,ink);
        Words(root,"Ribbon closing resources",NarrativeText.L("此刻 / 余力 ","NOW / BREATH ")+run.Capacity+NarrativeText.L(" · 承重 "," · LOAD ")+run.Load+NarrativeText.L(" · 直连 "," · BRIDGE ")+run.Bridge,75,719,1200,65,24,ink);
    }
}
