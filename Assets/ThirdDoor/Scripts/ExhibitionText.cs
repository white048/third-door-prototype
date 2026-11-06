using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.TextCore.LowLevel;

// One renderer per label. The small facade keeps the exhibition's typography consistent.
[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public class ExhibitionText : MonoBehaviour
{
    private TextMeshProUGUI rendered;
    private Text fallback;
    private Font boundFont;
    private CanvasGroup[] visibility;
    public Font font;
    public string text="";
    public int fontSize=24;
    public Color color=Color.black;
    public FontStyle fontStyle=FontStyle.Normal;
    public TextAnchor alignment=TextAnchor.UpperLeft;
    public HorizontalWrapMode horizontalOverflow=HorizontalWrapMode.Wrap;
    public VerticalWrapMode verticalOverflow=VerticalWrapMode.Truncate;
    public bool supportRichText,raycastTarget;
    public float lineSpacing=1;
    public RectTransform rectTransform => (RectTransform)transform;
    public float preferredHeight => rendered!=null?rendered.preferredHeight:fallback!=null?fallback.preferredHeight:0;
    public TMP_Text Rendered => rendered;
    private void Start(){visibility=GetComponentsInParent<CanvasGroup>(true);}
    private void OnEnable(){if(rendered!=null)Canvas.willRenderCanvases+=SubmitVisibleText;}
    private void OnDisable(){Canvas.willRenderCanvases-=SubmitVisibleText;}
    private void LateUpdate()
    {
        if(font==null)return;
        if(rendered==null&&fallback==null)
        {
            var asset=ExhibitionFontCache.Get(font);
            if(asset==null)fallback=gameObject.AddComponent<Text>();
            else
            {
                rendered=gameObject.AddComponent<TextMeshProUGUI>();rendered.raycastTarget=false;rendered.enableAutoSizing=false;
                rendered.margin=Vector4.zero;rendered.extraPadding=true;rendered.font=asset;boundFont=font;
                Canvas.willRenderCanvases+=SubmitVisibleText;
            }
        }
        if(fallback!=null)
        {
            fallback.font=font;fallback.text=text;fallback.fontSize=fontSize;fallback.color=color;
            fallback.fontStyle=fontStyle;fallback.alignment=alignment;fallback.supportRichText=supportRichText;
            fallback.horizontalOverflow=horizontalOverflow;fallback.verticalOverflow=verticalOverflow;
            fallback.lineSpacing=lineSpacing;fallback.raycastTarget=raycastTarget;return;
        }
        if(boundFont!=font){rendered.font=ExhibitionFontCache.Get(font);boundFont=font;}
        rendered.text=text;rendered.fontSize=fontSize;rendered.color=color;rendered.richText=supportRichText;
        rendered.enableWordWrapping=horizontalOverflow==HorizontalWrapMode.Wrap;
        rendered.overflowMode=TextOverflowModes.Truncate;
        rendered.lineSpacing=(lineSpacing-1)*fontSize;
        rendered.fontStyle=fontStyle==FontStyle.Bold?FontStyles.Bold:fontStyle==FontStyle.Italic?FontStyles.Italic:fontStyle==FontStyle.BoldAndItalic?FontStyles.Bold|FontStyles.Italic:FontStyles.Normal;
        switch(alignment)
        {
            case TextAnchor.UpperLeft:rendered.alignment=TextAlignmentOptions.TopLeft;break;
            case TextAnchor.UpperCenter:rendered.alignment=TextAlignmentOptions.Top;break;
            case TextAnchor.UpperRight:rendered.alignment=TextAlignmentOptions.TopRight;break;
            case TextAnchor.MiddleLeft:rendered.alignment=TextAlignmentOptions.Left;break;
            case TextAnchor.MiddleCenter:rendered.alignment=TextAlignmentOptions.Center;break;
            case TextAnchor.MiddleRight:rendered.alignment=TextAlignmentOptions.Right;break;
            case TextAnchor.LowerLeft:rendered.alignment=TextAlignmentOptions.BottomLeft;break;
            case TextAnchor.LowerCenter:rendered.alignment=TextAlignmentOptions.Bottom;break;
            default:rendered.alignment=TextAlignmentOptions.BottomRight;break;
        }
    }
    private void SubmitVisibleText()
    {
        if(rendered==null)return;
        // Submit after the canvas layout pass, which may query and invalidate TMP's cached text.
        // Hidden notebook and title pages retain their cached meshes.
        bool visible=color.a>.001f;
        if(visibility!=null)foreach(var group in visibility)if(group!=null&&group.alpha<.001f){visible=false;break;}
        if(visible)rendered.ForceMeshUpdate();
    }
}

public static class ExhibitionFontCache
{
    private static readonly Dictionary<string,TMP_FontAsset> families=new Dictionary<string,TMP_FontAsset>();
    private static readonly Dictionary<int,TMP_FontAsset> bindings=new Dictionary<int,TMP_FontAsset>();
    private static readonly HashSet<int> unavailable=new HashSet<int>();
    public static TMP_FontAsset Get(Font source)
    {
        if(source==null)return null;int id=source.GetInstanceID();
        if(bindings.TryGetValue(id,out var bound))return bound;
        if(unavailable.Contains(id))return null;
        string key=source.name;
        if(families.TryGetValue(key,out var cached)){bindings[id]=cached;return cached;}
        try
        {
            var asset=Bake(source,key);
            if(asset!=null){families[key]=asset;bindings[id]=asset;return asset;}
        }
        catch(System.Exception error){Debug.LogWarning("Print atlas fallback / "+key+" / "+error.Message);}
        unavailable.Add(id);return null;
    }
    private static TMP_FontAsset Bake(Font source,string key)
    {
        Shader shader=Shader.Find("TextMeshPro/Mobile/Distance Field");if(shader==null)shader=Shader.Find("TextMeshPro/Distance Field");
        if(shader==null)return null;
        FontEngine.InitializeFontEngine();
        const int point=64,padding=7;
        var result=FontEngine.LoadFontFace(source,point);
        if(result!=FontEngineError.Success)
        {
            string fonts=System.Environment.GetFolderPath(System.Environment.SpecialFolder.Fonts);
            bool display=source==ExhibitionDesign.DisplayFont;
            string[] files=display?new[]{"impact.ttf","arialbd.ttf","arial.ttf"}:new[]{"msyh.ttc","simhei.ttf","simsun.ttc","arial.ttf"};
            foreach(string file in files)
            {
                string filePath=System.IO.Path.Combine(fonts,file);
                if(!System.IO.File.Exists(filePath))continue;
                result=FontEngine.LoadFontFace(filePath,point);
                if(result==FontEngineError.Success)break;
            }
        }
        if(result!=FontEngineError.Success){Debug.LogWarning("Exhibition font unavailable: "+key);return null;}
        var face=FontEngine.GetFaceInfo();
        bool latin=source==ExhibitionDesign.DisplayFont;
        int size=latin?1024:4096;
        var texture=new Texture2D(size,size,TextureFormat.Alpha8,false,true){name="Print SDF / "+key,filterMode=FilterMode.Bilinear};
        var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
        typeof(FontEngine).GetMethod("ResetAtlasTexture",flags,null,new[]{typeof(Texture2D)},null).Invoke(null,new object[]{texture});
        var indexMethod=typeof(FontEngine).GetMethod("GetGlyphIndex",flags,null,new[]{typeof(uint)},null);
        var glyphIds=new List<uint>();var characterGlyphs=new Dictionary<uint,uint>();
        foreach(char character in NarrativeGlyphs.All)
        {
            uint glyph=(uint)indexMethod.Invoke(null,new object[]{(uint)character});if(glyph==0)continue;
            characterGlyphs[(uint)character]=glyph;if(!glyphIds.Contains(glyph))glyphIds.Add(glyph);
        }
        var free=new List<UnityEngine.TextCore.GlyphRect>{new UnityEngine.TextCore.GlyphRect(0,0,size-1,size-1)};
        var used=new List<UnityEngine.TextCore.GlyphRect>();
        UnityEngine.TextCore.Glyph[] packed;
        var signature=new[]{typeof(List<uint>),typeof(int),typeof(GlyphPackingMode),typeof(List<UnityEngine.TextCore.GlyphRect>),typeof(List<UnityEngine.TextCore.GlyphRect>),typeof(GlyphRenderMode),typeof(Texture2D),typeof(UnityEngine.TextCore.Glyph[]).MakeByRefType()};
        var packing=typeof(FontEngine).GetMethod("TryAddGlyphsToTexture",flags,null,signature,null);
        var arguments=new object[]{glyphIds,padding,GlyphPackingMode.BestShortSideFit,free,used,GlyphRenderMode.SDFAA,texture,null};
        bool complete=(bool)packing.Invoke(null,arguments);packed=(UnityEngine.TextCore.Glyph[])arguments[7];
        var glyphMap=new Dictionary<uint,UnityEngine.TextCore.Glyph>();
        foreach(var glyph in packed){if(glyph==null)break;glyph.atlasIndex=0;glyphMap[glyph.index]=glyph;}
        var asset=ScriptableObject.CreateInstance<TMP_FontAsset>();asset.name="Exhibition SDF / "+key;asset.faceInfo=face;
        Set(asset,"m_Version","1.1.0");Set(asset,"m_AtlasWidth",size);Set(asset,"m_AtlasHeight",size);Set(asset,"m_AtlasPadding",padding);Set(asset,"m_AtlasRenderMode",GlyphRenderMode.SDFAA);
        asset.atlasPopulationMode=AtlasPopulationMode.Static;asset.atlasTextures=new[]{texture};
        foreach(var glyph in glyphMap.Values)asset.glyphTable.Add(glyph);
        foreach(var pair in characterGlyphs)if(glyphMap.TryGetValue(pair.Value,out var glyph))asset.characterTable.Add(new TMP_Character(pair.Key,asset,glyph));
        var material=new Material(shader){name="Print SDF material / "+key};
        material.SetTexture("_MainTex",texture);material.SetFloat("_TextureWidth",size);material.SetFloat("_TextureHeight",size);material.SetFloat("_GradientScale",padding+1);
        material.SetFloat("_WeightNormal",asset.normalStyle);material.SetFloat("_WeightBold",asset.boldStyle);asset.material=material;
        asset.ReadFontAssetDefinition();
        Debug.Log("EXHIBITION_TEXT / "+key+" / glyphs="+asset.characterTable.Count+" / packed="+complete);
        return asset;
    }
    private static void Set(TMP_FontAsset asset,string field,object value)
    {typeof(TMP_FontAsset).GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(asset,value);}
    // The two immutable SDF atlases are session caches. Per-scene native Font bindings are removed.
    public static void Release(Font source){if(source!=null)bindings.Remove(source.GetInstanceID());}
}
