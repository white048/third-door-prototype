using UnityEngine;
using UnityEngine.UI;
using static ExhibitionDesign;

public partial class SceneBootstrapper
{
    private GameObject BuildCasePlate(Transform parent,string name,Vector2 at,Vector2 size,GameObject fallback=null,bool withCaption=false)
    {
        var plate=Rect(parent,name,at,size);
        Block(plate,"Print border",Vector2.zero,size,Ink);
        var inset=new Vector2(14,14);
        var image=Rect(plate,"Case illustration",inset,size-inset*2).gameObject.AddComponent<RawImage>();
        image.raycastTarget=false;image.color=Color.white;
        var fit=image.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;
        // Fit inside a separate inset rect so AspectRatioFitter cannot cover the frame.
        var opening=Rect(plate,"Print opening",inset,size-inset*2);image.transform.SetParent(opening,false);
        ExhibitionText caption=null;
        if(withCaption)caption=Type(parent,name+" caption","",at+new Vector2(0,size.y+20),new Vector2(size.x,36),23,Ink);
        parent.gameObject.AddComponent<CaseArtworkView>().Configure(image,plate.gameObject,fallback,caption);
        return plate.gameObject;
    }
    private void BuildIllustrationInstallation(Transform root)
    {
        var station=new GameObject("05 / A sentence as an image").transform;
        station.SetParent(root,false);station.SetPositionAndRotation(new Vector3(8.7f,0,-1.1f),Quaternion.Euler(0,38,0));
        var frame=new GameObject("Gallery illustration frame").transform;frame.SetParent(station,false);
        StripColliders(LocalBox("Art / carbon backing",frame,new Vector3(0,2.55f,.04f),new Vector3(3.7f,4.5f,.12f),printInk));
        StripColliders(LocalBox("Art / paper mount",frame,new Vector3(0,2.65f,-.035f),new Vector3(3.55f,3.95f,.04f),printPaper));
        StripColliders(LocalBox("Art / red register",frame,new Vector3(-1.77f,2.65f,-.08f),new Vector3(.045f,3.95f,.02f),printRed));
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Printed narrative collage";quad.transform.SetParent(frame,false);
        quad.transform.localPosition=new Vector3(0,2.8f,-.075f);quad.transform.localScale=new Vector3(3.25f,3.25f,1);StripColliders(quad);
        var material=CreateUnlitMat("Art / narrative print",Color.white);
        frame.gameObject.AddComponent<ExhibitionResourceLifetime>().Own(material);
        var caption=WorldPrint("",Vector3.zero,.095f,Ink,frame).GetComponent<TextMesh>();caption.transform.localPosition=new Vector3(0,.86f,-.075f);caption.transform.localRotation=Quaternion.identity;
        var collider=station.gameObject.AddComponent<BoxCollider>();collider.isTrigger=true;collider.center=new Vector3(0,2.55f,0);collider.size=new Vector3(3.7f,4.5f,.25f);
        station.gameObject.AddComponent<IllustrationStation>().Configure(frame.gameObject,quad.GetComponent<Renderer>(),collider,material,caption);
    }
    private void BuildArtworkReflection(Transform panel,DialogueUI ui)
    {
        var root=Rect(panel,"Visual note plate",new Vector2(1030,32),new Vector2(470,550));
        var image=Rect(root,"Visual note illustration",Vector2.zero,new Vector2(470,470)).gameObject.AddComponent<RawImage>();image.raycastTarget=false;
        var caption=Type(root,"Visual note caption","",new Vector2(0,495),new Vector2(470,55),18,Muted,chineseUiFont);
        panel.gameObject.AddComponent<ArtReflectionView>().Configure(ui,root.gameObject,image,caption);
    }
}
