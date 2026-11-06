using UnityEngine;
using UnityEngine.Rendering;

// All exhibition geometry is code-authored; no external models or baked assets are needed.
public partial class SceneBootstrapper
{
    private Material printInk;
    private Material printRed;
    private Material printPaper;
    private readonly TextMesh[] caseDoorFaces=new TextMesh[2];
    private readonly TextMesh[] caseDoorHeadings=new TextMesh[2];

    private void BuildConstructivistArchitecture(Transform root)
    {
        printInk = CreateUnlitMat("Print / Carbon", ExhibitionDesign.Ink);
        printRed = CreateUnlitMat("Print / Vermilion", ExhibitionDesign.Red);
        printPaper = CreateUnlitMat("Print / Warm white", ExhibitionDesign.Paper);
        Transform hall = new GameObject("01 / Architecture of a relationship").transform;
        hall.SetParent(root, false);
        CreateBox("Paper plinth", new Vector3(0,-0.22f,5), new Vector3(24,0.44f,34), middleRoomMat,hall);
        CreateBox("Exhibition back wall", new Vector3(0,5.5f,17),new Vector3(24,11,0.4f),middleRoomMat,hall);
        CreateBox("Left boundary",new Vector3(-12,3,5),new Vector3(.3f,6,34),middleRoomMat,hall);
        CreateBox("Right boundary",new Vector3(12,3,5),new Vector3(.3f,6,34),middleRoomMat,hall);
        CreateBox("Entry boundary",new Vector3(0,2,-12),new Vector3(24,4,.3f),middleRoomMat,hall);
        CreateBox("Exit return / left",new Vector3(-6.72f,1.975f,12.3f),new Vector3(10.56f,3.95f,.3f),middleRoomMat,hall);
        CreateBox("Exit return / right",new Vector3(6.72f,1.975f,12.3f),new Vector3(10.56f,3.95f,.3f),middleRoomMat,hall);

        // The circle, diagonal and vertical line form a spatial poster from the entry viewpoint.
        Disc("The red sun",new Vector3(3.1f,6.35f,16.72f),4.1f,printRed,hall);
        GameObject diagonal=CreateBox("Suspended diagonal",new Vector3(-2.8f,6.7f,14.9f),new Vector3(13,.58f,.5f),darkWallMat,hall);
        diagonal.transform.rotation=Quaternion.Euler(0,0,29);
        CreateBox("Counterweight",new Vector3(-8.8f,3.4f,12.5f),new Vector3(.5f,6.8f,.7f),redRoomMat,hall);
        CreateBox("Counterweight foot",new Vector3(-8.8f,.28f,12.5f),new Vector3(2.8f,.56f,1.8f),darkWallMat,hall);
        WorldPrint("03",new Vector3(-7.8f,7.6f,16.72f),2.7f,ExhibitionDesign.Ink,hall);
        WorldPrint("THE THIRD\nDOOR",new Vector3(-7.8f,5.8f,16.7f),.44f,ExhibitionDesign.Ink,hall);
        WorldPrint("A STUDY IN DISTANCE",new Vector3(2.5f,10.2f,16.7f),.16f,ExhibitionDesign.Ink,hall);
        WorldPrint("THREE VOICES.\nSEVEN POSITIONS.\nYOUR OWN WAY OUT.",new Vector3(8.7f,2.45f,16.7f),.2f,ExhibitionDesign.Ink,hall);

        // Inlaid typography and paths are non-colliding to keep walking completely smooth.
        FloorLine("01 / carry",new Vector3(-4.8f,.012f,4.8f),new Vector3(0,.012f,0),.13f,printRed,hall);
        FloorLine("02 / distance",new Vector3(4.8f,.013f,4.8f),new Vector3(0,.013f,0),.13f,printInk,hall);
        FloorLine("03 / release",new Vector3(0,.015f,0),new Vector3(0,.015f,12.3f),.07f,printInk,hall);
        for(int i=0;i<12;i++)
        {
            DecoBox("Measurement / "+i,new Vector3(-.32f,.021f,i+.2f),new Vector3(i%3==0?.48f:.2f,.014f,.025f),printInk,hall);
        }
        for(int i=0;i<7;i++)
        {
            DecoBox("Wall registration / "+i,new Vector3(-11.82f,1.1f+i*.55f,3),new Vector3(.02f,.045f,1.4f),printInk,hall);
        }
        // Side screens frame the doors without hiding the exit.
        GameObject redWing=CreateBox("Red wing",new Vector3(-8,2.8f,6.8f),new Vector3(.23f,5.6f,5.6f),redRoomMat,hall);
        redWing.transform.rotation=Quaternion.Euler(0,-26,0);
        GameObject inkWing=CreateBox("Ink wing",new Vector3(8.5f,2.4f,7.5f),new Vector3(.3f,4.8f,6.5f),darkWallMat,hall);
        inkWing.transform.rotation=Quaternion.Euler(0,26,0);
        for(int i=0;i<3;i++)
        {
            var beam=DecoBox("Rhythm / "+i,new Vector3(8.3f-i*.5f,6.8f+i*.65f,12.5f),new Vector3(4,.12f,.22f),printInk,hall);
            beam.transform.rotation=Quaternion.Euler(0,0,-25);
        }
        // Ceiling is a light architectural canopy; its compression expresses the loop ending.
        middleCeiling=new GameObject("Breathing frame").transform;
        middleCeiling.SetParent(hall,false);middleCeiling.position=new Vector3(0,7.7f,8);
        for(int i=0;i<3;i++)
        {
            var rail=LocalBox("Canopy rule / "+i,middleCeiling,new Vector3(-2+i*2,0,0),new Vector3(.09f,.12f,4),darkWallMat);
            StripColliders(rail);
        }
        redPressureWall=CreateBox("Loop / red pressure",new Vector3(-3.3f,2.6f,10),new Vector3(.18f,5.2f,7),redRoomMat,hall).transform;
        bluePressureWall=CreateBox("Loop / black pressure",new Vector3(3.3f,2.6f,10),new Vector3(.18f,5.2f,7),darkWallMat,hall).transform;
        redPressureWall.gameObject.SetActive(false);
        bluePressureWall.gameObject.SetActive(false);
        exitBackLight=CreateLight("Exit / daylight",new Vector3(0,3,14),Color.white,0,8,hall);
        exitBackLight.enabled=false;
        guidanceLineRenderers=new Renderer[0];
    }

    private void BuildConstructivistLighting(Transform root)
    {
        var go=new GameObject("Raking afternoon light");
        go.transform.SetParent(root,false);
        go.transform.rotation=Quaternion.Euler(48,-32,0);
        Light sun=go.AddComponent<Light>();
        sun.type=LightType.Directional;
        sun.color=new Color(1,.95f,.86f);
        sun.intensity=1.05f;
        sun.shadows=LightShadows.Soft;
        sun.shadowStrength=.78f;
        sun.shadowBias=.025f;
        middleLight=CreateLight("Soft gallery bounce",new Vector3(0,5,3),Color.white,.8f,18,root);
        middleLight.shadows=LightShadows.None;
    }

    private void BuildConstructivistDoors(Transform root)
    {
        Transform doors=new GameObject("02 / Three voices").transform;
        doors.SetParent(root,false);
        var first=ExhibitionDoor("01", "THE WEIGHT",new Vector3(-4.8f,0,4.8f),Quaternion.Euler(0,-24,0),redRoomMat,DialogueActor.RedDoor,doors);
        var second=ExhibitionDoor("02", "THE DISTANCE",new Vector3(4.8f,0,4.8f),Quaternion.Euler(0,24,0),blueRoomMat,DialogueActor.BlueDoor,doors);
        redDoorController=first.GetComponent<SymbolicDoorController>();
        blueDoorController=second.GetComponent<SymbolicDoorController>();
        sideDoorRenderers=new[]{first.GetComponentInChildren<Renderer>(),second.GetComponentInChildren<Renderer>()};
        ExhibitionDoor("03", "THE WAY OUT",new Vector3(0,0,12.3f),Quaternion.identity,blackDoorMat,DialogueActor.ExitDoor,doors);
        WorldPrint("YOU MAY CARE.\nYOU MAY LEAVE.",new Vector3(0,1.9f,16.5f),.34f,ExhibitionDesign.Ink,doors);
    }

    private GameObject ExhibitionDoor(string number,string caption,Vector3 at,Quaternion rotation,Material material,DialogueActor actor,Transform parent)
    {
        Transform station=new GameObject(number+" / "+caption).transform;
        station.SetParent(parent,false);
        station.SetPositionAndRotation(at,rotation);
        const float width=2.25f,height=3.65f;
        // Local coordinates make every printed detail part of the actual moving door.
        LocalBox("Left jamb",station,new Vector3(-1.3f,2,0),new Vector3(.28f,4,.48f),darkWallMat);
        LocalBox("Right jamb",station,new Vector3(1.3f,2,0),new Vector3(.28f,4,.48f),darkWallMat);
        LocalBox("Lintel",station,new Vector3(0,3.9f,0),new Vector3(2.88f,.25f,.48f),darkWallMat);
        LocalBox("Threshold",station,new Vector3(0,.04f,-.25f),new Vector3(2.85f,.08f,.85f),darkWallMat);
        var pivot=new GameObject("Moving leaf").transform;
        pivot.SetParent(station,false);
        var panel=LocalBox("Printed door leaf",pivot,new Vector3(0,height*.5f,0),new Vector3(width,height,.16f),material);
        Color face=ExhibitionDesign.Paper;
        var numeral=WorldPrint(number,Vector3.zero,1.25f,face,pivot);
        numeral.transform.localPosition=new Vector3(-.14f,2.35f,-.095f);
        numeral.transform.localRotation=Quaternion.identity;
        var small=WorldPrint(caption,Vector3.zero,.125f,face,pivot);
        small.transform.localPosition=new Vector3(0,.65f,-.1f);
        small.transform.localRotation=Quaternion.identity;
        var line=LocalBox("Typographic rule",pivot,new Vector3(0,1.17f,-.1f),new Vector3(1.65f,.025f,.018f),printPaper);
        StripColliders(line);
        var handle=LocalBox("Handle",pivot,new Vector3(.88f,1.35f,-.16f),new Vector3(.06f,.38f,.09f),middleRoomMat);
        StripColliders(handle);
        // Shadow reveal and reverse-side borders preserve depth when a leaf starts moving.
        var backing=LocalBox("Recess",station,new Vector3(0,1.9f,.45f),new Vector3(2.5f,3.8f,.1f),actor==DialogueActor.ExitDoor?printPaper:voidBlackMat);
        StripColliders(backing);
        if(actor==DialogueActor.ExitDoor) backing.SetActive(false);
        var cap=WorldPrint(number+" / "+caption,Vector3.zero,.15f,ExhibitionDesign.Ink,station);
        cap.transform.localPosition=new Vector3(0,4.3f,-.28f);
        cap.transform.localRotation=Quaternion.identity;
        if(actor==DialogueActor.RedDoor||actor==DialogueActor.BlueDoor)
        {
            int voice=actor==DialogueActor.RedDoor?0:1;
            caseDoorFaces[voice]=small.GetComponent<TextMesh>();
            caseDoorHeadings[voice]=cap.GetComponent<TextMesh>();
        }
        var interaction=station.gameObject.AddComponent<DoorDialogue>();
        interaction.Configure(actor,dialogueUI);
        if(actor==DialogueActor.ExitDoor)
        {
            doorController=station.gameObject.AddComponent<DoorController>();
            doorController.Configure(pivot,null,null,panel.GetComponent<Renderer>(),material,whiteGlowMat);
        }
        else
        {
            var symbolic=station.gameObject.AddComponent<SymbolicDoorController>();
            symbolic.Configure(pivot,null,null,material,whiteGlowMat,Vector3.up*4.1f,actor==DialogueActor.RedDoor?-32:32);
        }
        return station.gameObject;
    }

    private GameObject LocalBox(string label,Transform parent,Vector3 position,Vector3 size,Material material)
    {
        var go=CreateBox(label,Vector3.zero,size,material,parent);
        go.transform.localPosition=position;
        go.transform.localRotation=Quaternion.identity;
        return go;
    }

    private GameObject DecoBox(string label,Vector3 at,Vector3 size,Material material,Transform parent)
    {
        var go=CreateBox(label,at,size,material,parent);
        StripColliders(go);
        return go;
    }

    private void FloorLine(string label,Vector3 a,Vector3 b,float thickness,Material material,Transform parent)
    {
        var line=DecoBox(label,(a+b)*.5f,new Vector3(thickness,.015f,Vector3.Distance(a,b)),material,parent);
        line.transform.rotation=Quaternion.LookRotation(b-a);
    }

    private void Disc(string label,Vector3 at,float radius,Material material,Transform parent)
    {
        const int segments=160;
        var go=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer));
        go.transform.SetParent(parent,false);
        go.transform.position=at;
        var vertices=new Vector3[segments+1];
        var indices=new int[segments*3];
        for(int i=0;i<segments;i++)
        {
            float a=i*Mathf.PI*2/segments;
            vertices[i+1]=new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);
            indices[i*3]=0;indices[i*3+1]=(i+1)%segments+1;indices[i*3+2]=i+1;
        }
        var mesh=new Mesh{name="Circular print / 160 segments"};
        mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        go.GetComponent<MeshFilter>().sharedMesh=mesh;
        go.GetComponent<Renderer>().sharedMaterial=material;
        go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        go.AddComponent<ExhibitionMeshLifetime>().mesh=mesh;
    }

    private GameObject WorldPrint(string value,Vector3 at,float scale,Color color,Transform parent)
    {
        var go=new GameObject("Print / "+value.Replace('\n',' '));
        go.transform.SetParent(parent,false);
        go.transform.position=at;
        var text=go.AddComponent<TextMesh>();
        text.text=value;
        text.font=ExhibitionDesign.DisplayFont;
        text.fontSize=200;
        text.characterSize=scale*.05f;
        text.anchor=TextAnchor.MiddleCenter;
        text.alignment=TextAlignment.Center;
        text.color=color;
        text.lineSpacing=.85f;
        go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
        go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        go.AddComponent<ExhibitionWorldType>().Configure(text);
        return go;
    }
}

