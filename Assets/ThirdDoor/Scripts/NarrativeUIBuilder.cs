using UnityEngine;
using UnityEngine.UI;
using static ExhibitionDesign;

public partial class SceneBootstrapper
{
    private void BuildNarrativeCards(Transform panel, DialogueUI dialogue)
    {
        Block(panel,"Card column rule",new Vector2(1000,30),new Vector2(2,550),Ink);
        var heading=Type(panel,"Card heading","",new Vector2(1030,28),new Vector2(470,35),22,Muted,chineseUiFont);
        var selected=Type(panel,"Card selected name","",new Vector2(1030,82),new Vector2(470,46),31,Red,chineseUiFont);
        var detail=Type(panel,"Card description","",new Vector2(1030,142),new Vector2(470,136),22,Ink,chineseUiFont);
        var cards=new Button[6];
        for(int i=0;i<6;i++)
        {
            cards[i]=Button(panel,"Card / "+(ConversationCard)i,"",new Vector2(1030+(i%2)*240,292+(i/2)*72),new Vector2(225,62),new Color(.84f,.82f,.75f),Ink,chineseUiFont);
            var text=cards[i].GetComponentInChildren<ExhibitionText>();text.fontSize=22;text.supportRichText=true;
        }
        var none=Button(panel,"No card","",new Vector2(1030,518),new Vector2(465,58),Ink,Paper,chineseUiFont);
        panel.gameObject.AddComponent<NarrativeCardTray>().Configure(dialogue,heading,selected,detail,cards,none);
    }

    private void BuildResourceReadout(Transform canvas)
    {
        var root=Block(canvas,"Relationship resources",new Vector2(1170,110),new Vector2(714,156),Paper);
        var labels=new ExhibitionText[3];var bars=new Image[3][];
        for(int row=0;row<3;row++)
        {
            labels[row]=Type(root.transform,"Resource "+row,"",new Vector2(20,13+row*47),new Vector2(235,34),22,Ink,chineseUiFont);
            int count=row==2?6:8;
            bars[row]=new Image[count];
            for(int i=0;i<count;i++) bars[row][i]=Block(root.transform,"Bar "+row+" / "+i,new Vector2(270+i*(count==6?62:46),18+row*47),new Vector2(count==6?54:38,20),Ink);
        }
        var notePaper=Block(canvas,"Resource note paper",new Vector2(1170,271),new Vector2(714,48),Paper);
        var status=Type(notePaper.transform,"Resource pressure note","",new Vector2(12,7),new Vector2(690,40),19,Red,chineseUiFont);
        canvas.gameObject.AddComponent<NarrativeResourceHUD>().Configure(root.gameObject,labels,bars,status);
    }

    private void BuildNotebook(Transform canvas,Transform titlePoster,PlayerController controller,DialogueUI dialogue)
    {
        var paper=Block(canvas,"Words carried / notebook",Vector2.zero,new Vector2(1920,1080),Paper);
        Fullscreen(paper.rectTransform);paper.raycastTarget=true;
        var title=Type(paper.transform,"Notebook title","",new Vector2(90,66),new Vector2(1450,130),76,Ink);
        var close=Button(paper.transform,"Close notebook","TAB / CLOSE",new Vector2(1550,80),new Vector2(280,66),Ink,Paper,chineseUiFont);
        var current=Button(paper.transform,"Current notes","",new Vector2(100,213),new Vector2(370,54),Ink,Paper,chineseUiFont);
        var archive=Button(paper.transform,"Ending archive","",new Vector2(490,213),new Vector2(420,54),Red,Paper,chineseUiFont);
        var notes=Rect(paper.transform,"Current journey",new Vector2(100,290),new Vector2(1720,625));
        var explanation=Type(notes,"Resource definitions","",Vector2.zero,new Vector2(1720,80),22,Ink,chineseUiFont);
        var memories=new ExhibitionText[2];
        for(int i=0;i<2;i++)
        {
            var card=Block(notes,"Heard need / "+i,new Vector2(i*880,92),new Vector2(840,150),new Color(.84f,.82f,.75f));
            Block(card.transform,"Speaker index",Vector2.zero,new Vector2(7,150),i==0?Red:Ink);
            memories[i]=Type(card.transform,"Need","",new Vector2(22,17),new Vector2(795,125),23,Ink,chineseUiFont);
        }
        var history=new ExhibitionText[6];var changes=new ExhibitionText[6];
        for(int i=0;i<6;i++)
        {
            Block(notes,"Ledger rule / "+i,new Vector2(0,270+i*57),new Vector2(1720,1),new Color(.5f,.48f,.41f,.5f));
            history[i]=Type(notes,"Response / "+i,"",new Vector2(0,271+i*57),new Vector2(1050,56),21,Ink,chineseUiFont);
            history[i].alignment=TextAnchor.MiddleLeft;
            changes[i]=Type(notes,"Recorded change / "+i,"",new Vector2(1070,271+i*57),new Vector2(650,56),21,Muted,chineseUiFont);
            changes[i].alignment=TextAnchor.MiddleRight;
        }
        var endingPage=Rect(paper.transform,"Seven positions",new Vector2(100,290),new Vector2(1720,625));
        var endingTitles=new ExhibitionText[7];var endingBodies=new ExhibitionText[7];var endingCards=new Image[7];
        for(int i=0;i<7;i++)
        {
            endingCards[i]=Block(endingPage,"Archive / "+i,new Vector2((i%2)*880,(i/2)*151),new Vector2(840,136),new Color(.84f,.82f,.75f));
            endingTitles[i]=Type(endingCards[i].transform,"Ending title","",new Vector2(20,14),new Vector2(800,38),26,Ink,chineseUiFont);
            endingBodies[i]=Type(endingCards[i].transform,"Ending body","",new Vector2(20,58),new Vector2(800,69),20,Ink,chineseUiFont);
        }
        var note=Type(paper.transform,"Notebook margin note","",new Vector2(100,937),new Vector2(1150,82),22,Muted,chineseUiFont);
        var rewind=Button(paper.transform,"Rewind response","",new Vector2(1340,937),new Vector2(490,68),Ink,Paper,chineseUiFont);
        var titleArchive=Button(titlePoster,"Title ending archive","",new Vector2(1450,1000),new Vector2(380,43),Ink,Paper,chineseUiFont);
        titleArchive.GetComponentInChildren<ExhibitionText>().fontSize=20;
        var notebook=paper.gameObject.AddComponent<NarrativeNotebook>();
        notebook.Configure(paper.gameObject,controller,dialogue,title,close,current,archive,notes.gameObject,explanation,memories,history,changes,endingPage.gameObject,endingTitles,endingBodies,endingCards,note,rewind,titleArchive);
        BuildCorrespondenceUI(paper.transform,notebook,controller,dialogue);
        BuildNarrativeRibbon(paper.transform,notebook);
        var readButtons=new Button[6];
        for(int i=0;i<6;i++)
        {
            readButtons[i]=Button(notes,"Read response / "+i,"",new Vector2(0,271+i*57),new Vector2(1720,56),Color.clear,Ink,chineseUiFont);
            var colors=readButtons[i].colors;colors.highlightedColor=new Color(.78f,.105f,.065f,.08f);colors.pressedColor=new Color(.78f,.105f,.065f,.15f);colors.disabledColor=Color.clear;readButtons[i].colors=colors;
        }
        var traceLayer=Block(paper.transform,"Sentence focus",Vector2.zero,new Vector2(1920,1080),new Color(0,0,0,.3f));
        Fullscreen(traceLayer.rectTransform);traceLayer.raycastTarget=true;
        var traceBorder=Block(traceLayer.transform,"A sentence in transit",new Vector2(220,310),new Vector2(1480,610),Ink);
        var trace=Block(traceBorder.transform,"Trace paper",new Vector2(3,3),new Vector2(1474,604),Paper);trace.raycastTarget=true;
        var traceTitle=Type(trace.transform,"Trace heading","",new Vector2(36,24),new Vector2(1080,48),30,Ink,chineseUiFont);
        var traceClose=Button(trace.transform,"Close sentence trace","",new Vector2(1150,22),new Vector2(280,53),Ink,Paper,chineseUiFont);
        var source=Type(trace.transform,"Original sentence","",new Vector2(36,110),new Vector2(650,200),26,Ink,chineseUiFont);
        Block(trace.transform,"Transit division",new Vector2(720,105),new Vector2(2,205),Red);
        var reply=Type(trace.transform,"Your response","",new Vector2(770,110),new Vector2(650,200),26,Ink,chineseUiFont);
        var consequence=Type(trace.transform,"Sentence consequence","",new Vector2(36,345),new Vector2(1390,220),23,Ink,chineseUiFont);
        notebook.ConfigureTrace(traceLayer.gameObject,traceTitle,source,reply,consequence,traceClose,readButtons);
        BuildDispatchComposer(paper.transform,paper.GetComponentInChildren<CorrespondenceFolio>(true),controller);
        BuildDispatchProof(paper.transform,paper.GetComponentInChildren<CorrespondenceFolio>(true));
    }
}
