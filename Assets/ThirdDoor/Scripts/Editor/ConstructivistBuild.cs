using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ConstructivistBuild
{
    public const string Scene="Assets/Scenes/ConstructivistDemo.unity";
    [MenuItem("Third Door/Build Constructivist Demo")]
    public static void Build()
    {
        // Build the dedicated asset directly; do not replace the user's open or unsaved scene.
        if(!File.Exists(Scene))throw new FileNotFoundException("Demo scene missing",Scene);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
        PlayerSettings.productName="The Third Door";
        PlayerSettings.companyName="Third Door Studio";
        PlayerSettings.defaultScreenWidth=1600;
        PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.resizableWindow=true;
        PlayerSettings.runInBackground=true;
        QualitySettings.vSyncCount=0;
        QualitySettings.shadowDistance=70;
        QualitySettings.shadows=ShadowQuality.All;
        QualitySettings.shadowResolution=ShadowResolution.High;
        IncludeRuntimeShaders();
        string output=Path.GetFullPath("Builds/Constructivist/TheThirdDoor.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        AssetDatabase.SaveAssets();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=new[]{Scene},locationPathName=output,target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.None
        });
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Constructivist build failed: "+report.summary.result);
        Debug.Log("CONSTRUCTIVIST_BUILD / PASS / "+output);
    }

    private static void IncludeRuntimeShaders()
    {
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var list=settings.FindProperty("m_AlwaysIncludedShaders");
        foreach(string name in new[]{"Universal Render Pipeline/Lit","Universal Render Pipeline/Unlit","UI/Default","TextMeshPro/Mobile/Distance Field"})
        {
            var shader=Shader.Find(name);
            if(shader==null)throw new Exception("Required shader missing: "+name);
            bool exists=false;
            for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==shader)exists=true;
            if(exists)continue;
            int index=list.arraySize;list.InsertArrayElementAtIndex(index);
            list.GetArrayElementAtIndex(index).objectReferenceValue=shader;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
