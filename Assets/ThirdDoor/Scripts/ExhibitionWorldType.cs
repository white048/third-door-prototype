using UnityEngine;
using UnityEngine.Rendering;

// The font atlas stores coverage in alpha; UI/Default supports that and a depth-test override.
public class ExhibitionWorldType : MonoBehaviour
{
    private Font font;
    private Material ink;
    public void Configure(TextMesh text)
    {
        font=text.font;
        Shader shader=Shader.Find("UI/Default");
        ink=shader!=null?new Material(shader):new Material(font.material);
        ink.name="Depth-tested print";
        ink.mainTexture=font.material.mainTexture;
        ink.color=Color.white;
        ink.SetVector("_TextureSampleAdd",new Vector4(1,1,1,0));
        ink.SetInt("unity_GUIZTestMode",(int)CompareFunction.LessEqual);
        GetComponent<MeshRenderer>().sharedMaterial=ink;
        Font.textureRebuilt+=Refresh;
    }
    private void Refresh(Font changed){if(changed==font&&ink!=null)ink.mainTexture=font.material.mainTexture;}
    private void OnDestroy()
    {
        Font.textureRebuilt-=Refresh;
        if(ink!=null){if(Application.isPlaying)Destroy(ink);else DestroyImmediate(ink);}
    }
}
