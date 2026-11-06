using UnityEngine;

public partial class SceneBootstrapper
{
    private void BuildCaseInstallation(Transform root)
    {
        var installation=new GameObject("04 / Terms and blank spaces").transform;
        installation.SetParent(root,false);
        // This print stands outside both the rest approach and the route to door 01.
        // Every part is decorative: the workplan never changes collision or sight to a door.
        var board=new GameObject("Office / Public workplan").transform;
        board.SetParent(installation,false);
        board.SetPositionAndRotation(new Vector3(-9.4f,0,1.6f),Quaternion.Euler(0,-48,0));
        CaseBox("Workplan / carbon backing",board,new Vector3(0,2.5f,.025f),new Vector3(4.56f,4.86f,.08f),printInk);
        CaseBox("Workplan / paper",board,new Vector3(0,2.5f,-.03f),new Vector3(4.46f,4.76f,.04f),printPaper);
        CaseBox("Workplan / red register",board,new Vector3(-2.15f,2.5f,-.06f),new Vector3(.07f,4.7f,.025f),printRed);
        CasePrint("02 / WORK PLAN",board,new Vector3(0,4.5f,-.07f),.3f,ExhibitionDesign.Ink);
        CasePrint("ONLY WHAT THE WORK NEEDS",board,new Vector3(0,4.08f,-.07f),.105f,ExhibitionDesign.Ink);
        CaseBox("Workplan / heading rule",board,new Vector3(0,3.84f,-.07f),new Vector3(3.84f,.025f,.015f),printInk);
        CasePrint("DELIVERABLES",board,new Vector3(0,3.5f,-.07f),.17f,ExhibitionDesign.Red);
        CasePrint("FRIDAY DRAFT\nDAYTIME REVIEW",board,new Vector3(0,2.92f,-.07f),.24f,ExhibitionDesign.Ink);
        CaseBox("Workplan / task rule",board,new Vector3(0,2.2f,-.07f),new Vector3(3.84f,.025f,.015f),printInk);
        CasePrint("REASON",board,new Vector3(0,1.92f,-.07f),.16f,ExhibitionDesign.Ink);
        // An outlined field is deliberately left empty. Private words never appear here.
        CaseBox("Blank reason / top",board,new Vector3(0,1.58f,-.07f),new Vector3(3.6f,.018f,.015f),printInk);
        CaseBox("Blank reason / bottom",board,new Vector3(0,.96f,-.07f),new Vector3(3.6f,.018f,.015f),printInk);
        CaseBox("Blank reason / left",board,new Vector3(-1.8f,1.27f,-.07f),new Vector3(.018f,.62f,.015f),printInk);
        CaseBox("Blank reason / right",board,new Vector3(1.8f,1.27f,-.07f),new Vector3(.018f,.62f,.015f),printInk);
        CasePrint("THIS SPACE MAY STAY BLANK",board,new Vector3(0,.68f,-.07f),.12f,ExhibitionDesign.Ink);
        var receipt=CasePrint("PUBLIC REQUEST / NOT YET CARRIED",board,new Vector3(0,.32f,-.07f),.098f,ExhibitionDesign.Red);

        var agreement=new GameObject("Office / A limited agreement").transform;
        agreement.SetParent(installation,false);
        agreement.SetPositionAndRotation(new Vector3(0,.94f,5.5f),Quaternion.Euler(90,0,0));
        CaseBox("Agreement / paper",agreement,Vector3.zero,new Vector3(1.9f,1,.014f),printPaper);
        CaseBox("Agreement / red rule",agreement,new Vector3(0,.4f,-.012f),new Vector3(1.7f,.025f,.008f),printRed);
        CasePrint("LIMITED AGREEMENT",agreement,new Vector3(0,.22f,-.016f),.075f,ExhibitionDesign.Ink);
        CasePrint("TASK / TIME / SCOPE",agreement,new Vector3(0,-.02f,-.016f),.06f,ExhibitionDesign.Ink);
        CasePrint("REASON: __________",agreement,new Vector3(0,-.3f,-.016f),.065f,ExhibitionDesign.Ink);
        installation.gameObject.AddComponent<CaseInstallation>().Configure(board.gameObject,agreement.gameObject,caseDoorFaces,caseDoorHeadings,receipt);
    }

    private GameObject CaseBox(string name,Transform parent,Vector3 at,Vector3 size,Material material)
    {
        var box=LocalBox(name,parent,at,size,material);StripColliders(box);return box;
    }
    private TextMesh CasePrint(string text,Transform parent,Vector3 at,float scale,Color color)
    {
        var print=WorldPrint(text,Vector3.zero,scale,color,parent);
        print.transform.localPosition=at;print.transform.localRotation=Quaternion.identity;
        return print.GetComponent<TextMesh>();
    }
}
