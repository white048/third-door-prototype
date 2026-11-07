using UnityEngine;

// Physical prints follow the selected case even when the title changes after scene construction.
public sealed class CaseInstallation : MonoBehaviour
{
    private GameObject workplan,agreement;
    private TextMesh[] doorFaces,doorHeadings;
    private TextMesh receipt;
    private TextMesh workHeading,workDetails;
    public string PrintedWorkplan=>workDetails!=null?workDetails.text:"";
    private CaseId selected=(CaseId)(-1);
    private RelationshipRun observed;
    private int revision=-1;

    public void Configure(GameObject board,GameObject tablePaper,TextMesh[] faces,TextMesh[] headings,TextMesh publicReceipt,TextMesh heading,TextMesh details)
    {
        workplan=board;agreement=tablePaper;doorFaces=faces;doorHeadings=headings;receipt=publicReceipt;workHeading=heading;workDetails=details;
        Refresh();
    }

    private void LateUpdate(){Refresh();}
    private void Refresh()
    {
        var state=GameStateManager.Instance;
        CaseId active=CaseInfo.Active;
        RelationshipRun run=state!=null?state.Run:null;
        int nextRevision=run!=null?run.Revision:-1;
        if(selected==active&&observed==run&&revision==nextRevision)return;
        selected=active;observed=run;revision=nextRevision;
        bool office=active==CaseId.Office;
        workplan.SetActive(office);
        agreement.SetActive(office&&run!=null&&run.Case==CaseId.Office&&run.Ending==RelationshipEnding.SharedTable);
        string[] names=office?new[]{"THE MANAGER","THE COLLEAGUE"}:new[]{"THE WEIGHT","THE DISTANCE"};
        for(int i=0;i<2;i++)
        {
            if(doorFaces[i]!=null)doorFaces[i].text=names[i];
            if(doorHeadings[i]!=null)doorHeadings[i].text="0"+(i+1)+" / "+names[i];
        }
        bool delivered=office&&run!=null&&run.Case==CaseId.Office&&run.OfficePublicRequestDelivered;
        bool deck=office&&run!=null&&run.DeckRules&&!IntroSequence.IsActive;
        if(workHeading!=null)workHeading.text=deck?(run.Phase==0?"01 / THE FIRST PLAN":run.Phase==1?"02 / REVISED NOTICE":"03 / HANDOFF"):"DELIVERABLES";
        if(workDetails!=null)workDetails.text=deck?(run.Phase==0?"FRIDAY / DRAFT AT 1\nREVIEW AT 2:30":run.Phase==1?"ROOM B / 2:30\nOLD ROOM A SLIP VOID":"COLLEAGUE / UPLOAD\nMANAGER / INVITE"):"FRIDAY DRAFT\nDAYTIME REVIEW";
        if(receipt!=null)
        {
            receipt.text=delivered?"PUBLIC REQUEST / DELIVERED":"PUBLIC REQUEST / NOT YET CARRIED";
            if(deck)receipt.text="01 "+(run.PublicPlanVerified(0)?"[X]":"[ ]")+"   02 "+(run.PublicPlanVerified(1)?"[X]":"[ ]")+"   TASKS "+run.CurrentTaskCount;
            receipt.color=delivered?ExhibitionDesign.Ink:ExhibitionDesign.Red;
        }
    }
}
