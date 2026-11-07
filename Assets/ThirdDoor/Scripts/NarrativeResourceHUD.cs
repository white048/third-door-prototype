using UnityEngine;
using UnityEngine.UI;

public class NarrativeResourceHUD : MonoBehaviour
{
    private GameObject panel;
    private ExhibitionText[] labels;
    private Image[][] bars;
    private ExhibitionText pressure;
    private int revision=-1;
    private Language language;
    public void Configure(GameObject p,ExhibitionText[] text,Image[][] segments,ExhibitionText note){panel=p;labels=text;bars=segments;pressure=note;}
    private void Update()
    {
        var state=GameStateManager.Instance;if(state==null)return;
        bool visible=!IntroSequence.IsActive;
        panel.SetActive(visible);pressure.transform.parent.gameObject.SetActive(visible&&!state.Run.Ended);
        if(revision==state.Run.Revision&&language==LocalizationManager.Current)return;
        var run=state.Run;revision=run.Revision;language=LocalizationManager.Current;
        int[] values={run.Capacity,run.Load,run.Bridge};
        string[] names={NarrativeText.L("余力","BREATH"),NarrativeText.L("承重","LOAD"),NarrativeText.L("直连","BRIDGE")};
        for(int row=0;row<3;row++)
        {
            labels[row].text=names[row]+"   "+values[row]+" / "+bars[row].Length;
            Color ink=row==1?ExhibitionDesign.Red:ExhibitionDesign.Ink;
            if(row==0&&run.Capacity<=1)ink=ExhibitionDesign.Red;
            for(int i=0;i<bars[row].Length;i++)bars[row][i].color=i<values[row]?ink:new Color(.71f,.69f,.62f,.6f);
        }
        pressure.text=run.Capacity==0?NarrativeText.L("余力为 0 / 可去左侧 00 长椅休息，或先说清自己的限度。","NO BREATH / REST AT BENCH 00 ON THE LEFT, OR SAY HOW MUCH YOU CAN HELP."):run.Load>=6?NarrativeText.L("承重 ≥ 6 / 承接和追问会额外消耗余力。","LOAD ≥ 6 / CARRYING AND ASKING COST AN EXTRA BREATH."):run.HeldMessages>0?NarrativeText.L("在手的话 ","HELD ")+run.HeldMessages+NarrativeText.L(" / F 门前处理 / Tab 传话夹"," / F AT A DOOR / TAB CORRESPONDENCE"):NarrativeText.L("左侧 00 长椅可休息 / Tab 手记 / G 重述（获得卡后）","BENCH 00 ON THE LEFT / TAB NOTES / G REVISE");
        if(run.DeckRules&&run.Capacity>0&&run.Load<6)pressure.text=run.DeckReady?NarrativeText.L("本场五张牌各一次 / Tab 手牌 / 03 结束本场","FIVE CARDS, ONCE EACH / TAB YOUR HAND / 03 NEXT SCENE"):NarrativeText.L("E 门前选择本场手牌 / 下一场可换一张","E AT A DOOR TO PREPARE YOUR HAND / SWAP ONE NEXT SCENE");
    }
}
