using UnityEngine;

// Physical prints follow the selected case even when the title changes after scene construction.
public sealed class CaseInstallation : MonoBehaviour
{
    private GameObject workplan,agreement;
    private TextMesh[] doorFaces,doorHeadings;
    private TextMesh receipt;
    private CaseId selected=(CaseId)(-1);
    private RelationshipRun observed;
    private int revision=-1;

    public void Configure(GameObject board,GameObject tablePaper,TextMesh[] faces,TextMesh[] headings,TextMesh publicReceipt)
    {
        workplan=board;agreement=tablePaper;doorFaces=faces;doorHeadings=headings;receipt=publicReceipt;
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
        if(receipt!=null)
        {
            receipt.text=delivered?"PUBLIC REQUEST / DELIVERED":"PUBLIC REQUEST / NOT YET CARRIED";
            receipt.color=delivered?ExhibitionDesign.Ink:ExhibitionDesign.Red;
        }
    }
}
