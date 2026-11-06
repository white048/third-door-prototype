using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Resources serves a normal Unity build; loose files also support the inherited preview player.
public static class IllustrationLibrary
{
    private static readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
    private static readonly List<Texture2D> owned=new List<Texture2D>();
    private static bool hooked;
    public static bool ValidationFallback
    {
        get{foreach(string arg in Environment.GetCommandLineArgs())if(arg=="--art-fallback")return true;return false;}
    }
    public static Texture2D ForCase(CaseId id)=>Load(id==CaseId.Office?"CaseOffice-v1":"CaseHome-v1");
    public static Texture2D Master=>Load("KeyArtMaster-v1");
    public static Texture2D Load(string id)
    {
        if(ValidationFallback)return null;
        Texture2D image;if(cache.TryGetValue(id,out image))return image;
        image=Resources.Load<Texture2D>("Design/Illustrations/"+id);
        if(image==null&&Application.isPlaying)
        {
            try
            {
                string path=Path.Combine(Application.dataPath,"..","Art",id+".png");
                if(File.Exists(path)&&new FileInfo(path).Length<=32*1024*1024)
                {
                    var decoded=new Texture2D(2,2,TextureFormat.RGBA32,false,false){name="Illustration / "+id};
                    if(decoded.LoadImage(File.ReadAllBytes(path),true)&&decoded.width<=4096&&decoded.height<=4096)
                    {image=decoded;owned.Add(image);}
                    else UnityEngine.Object.Destroy(decoded);
                }
            }
            catch(Exception error){Debug.LogWarning("ILLUSTRATION / "+id+" unavailable: "+error.GetType().Name);}
        }
        if(image!=null){image.wrapMode=TextureWrapMode.Clamp;image.filterMode=FilterMode.Bilinear;}
        cache[id]=image;
        if(!hooked&&Application.isPlaying){Application.quitting+=Release;hooked=true;}
        return image;
    }
    private static void Release()
    {
        foreach(var image in owned)if(image!=null)UnityEngine.Object.Destroy(image);
        owned.Clear();cache.Clear();Application.quitting-=Release;hooked=false;
    }
}
