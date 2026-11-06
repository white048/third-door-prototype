using UnityEngine;

public partial class SceneBootstrapper
{
    private void BuildNarrativeInstallation(Transform root)
    {
        var installation=new GameObject("03 / The weight of a sentence").transform;
        installation.SetParent(root,false);
        var bench=new GameObject("00 / A place for yourself").transform;
        bench.SetParent(installation,false);bench.position=new Vector3(-6.1f,0,-1.2f);
        LocalBox("Rest / seat",bench,new Vector3(0,.6f,0),new Vector3(2.6f,.15f,.8f),middleRoomMat);
        LocalBox("Rest / red foot",bench,new Vector3(-.95f,.28f,0),new Vector3(.16f,.56f,.7f),redRoomMat);
        LocalBox("Rest / ink foot",bench,new Vector3(.95f,.28f,0),new Vector3(.16f,.56f,.7f),darkWallMat);
        LocalBox("Rest / back plaque",bench,new Vector3(0,1.2f,.22f),new Vector3(2.6f,.75f,.1f),middleRoomMat);
        var restPrint=WorldPrint("00 / REST\nYOU HAVE A PLACE HERE.",Vector3.zero,.18f,ExhibitionDesign.Ink,bench);
        restPrint.transform.localPosition=new Vector3(0,1.22f,.15f);
        bench.gameObject.AddComponent<RestStation>();
        var restLight=CreateLight("Rest / breathing space",bench.position+new Vector3(0,1.8f,-1),new Color(1,.91f,.7f),.6f,4,bench);
        restLight.shadows=LightShadows.None;
        FloorLine("A line of your own",new Vector3(-6.1f,.022f,-1.1f),new Vector3(-2.4f,.022f,-2.8f),.05f,printRed,installation);

        var table=new GameObject("A shared table / needs two yeses").transform;
        table.SetParent(installation,false);table.position=new Vector3(0,0,5.5f);
        LocalBox("Table top",table,new Vector3(0,.86f,0),new Vector3(3.3f,.12f,1.25f),middleRoomMat);
        LocalBox("Table leg / 01",table,new Vector3(-1.25f,.4f,0),new Vector3(.13f,.8f,.65f),redRoomMat);
        LocalBox("Table leg / 02",table,new Vector3(1.25f,.4f,0),new Vector3(.13f,.8f,.65f),darkWallMat);
        BuildChair(table,new Vector3(-2.1f,0,0),redRoomMat);
        BuildChair(table,new Vector3(2.1f,0,0),darkWallMat);
        var tableLabel=WorldPrint("SPEAK / LISTEN",Vector3.zero,.14f,ExhibitionDesign.Ink,table);
        tableLabel.transform.localPosition=new Vector3(0,1.23f,.1f);
        table.gameObject.SetActive(false);

        var slips=new Transform[8];
        string[] words={"CARRY","SOFTEN","DEFEND","FIX","PROMISE","HOLD","ONE MORE","FOR THEM"};
        for(int i=0;i<slips.Length;i++)
        {
            slips[i]=new GameObject("Carried sentence / "+i).transform;slips[i].SetParent(installation,false);
            var sheet=LocalBox("Paper",slips[i],Vector3.zero,new Vector3(1.65f,.36f,.035f),i%2==0?printPaper:printRed);StripColliders(sheet);
            var stamp=WorldPrint(words[i],Vector3.zero,.15f,i%2==0?ExhibitionDesign.Ink:ExhibitionDesign.Paper,slips[i]);
            stamp.transform.localPosition=new Vector3(0,0,-.025f);
        }
        var wires=new LineRenderer[3];
        for(int i=0;i<3;i++)
        {
            var go=new GameObject("Connection / "+i);go.transform.SetParent(installation,false);
            wires[i]=go.AddComponent<LineRenderer>();wires[i].sharedMaterial=i==0?printRed:printInk;
            wires[i].positionCount=28;wires[i].useWorldSpace=true;wires[i].numCapVertices=3;
            wires[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var marker=DecoBox("The revolving obligation",new Vector3(3.1f,10.7f,16.61f),new Vector3(.16f,.65f,.06f),printInk,installation).transform;
        var act=WorldPrint("I / BORROWED VOICES",new Vector3(0,4.7f,12.2f),.16f,ExhibitionDesign.Ink,installation).GetComponent<TextMesh>();
        installation.gameObject.AddComponent<RelationshipInstallation>().Configure(player,middleCeiling,table.gameObject,slips,wires,marker,restLight,act);
        BuildRibbonStation(installation);
    }

    private void BuildChair(Transform parent,Vector3 position,Material material)
    {
        var chair=new GameObject("A voice has its own seat").transform;chair.SetParent(parent,false);chair.localPosition=position;
        LocalBox("Seat",chair,new Vector3(0,.47f,0),new Vector3(.7f,.1f,.7f),material);
        LocalBox("Support",chair,new Vector3(0,.22f,0),new Vector3(.13f,.44f,.55f),material);
        LocalBox("Back",chair,new Vector3(position.x<0?-.3f:.3f,.85f,0),new Vector3(.08f,.8f,.7f),material);
    }
}
