using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWindowsExe
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string ProductName = "TheThirdDoor";
    private const string OutputFolder = "Builds/Windows";

    [MenuItem("Third Door/Build Windows EXE")]
    public static void BuildWindows()
    {
        // Make sure the scene is in build settings.
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        // Basic player metadata so the .exe shows up with a recognizable name.
        PlayerSettings.productName = "The Third Door";
        PlayerSettings.companyName = "ThirdDoor Prototype";

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string buildDir = Path.Combine(projectRoot, OutputFolder);
        Directory.CreateDirectory(buildDir);
        string exePath = Path.Combine(buildDir, ProductName + ".exe");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Build] Succeeded → {exePath}  size={summary.totalSize / 1024 / 1024} MB  time={summary.totalTime}");
            EditorUtility.RevealInFinder(exePath);
        }
        else
        {
            Debug.LogError($"[Build] {summary.result} — see Console for errors.");
        }
    }

    [MenuItem("Third Door/Open Build Folder")]
    public static void OpenBuildFolder()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string buildDir = Path.Combine(projectRoot, OutputFolder);
        if (Directory.Exists(buildDir))
        {
            EditorUtility.RevealInFinder(buildDir);
        }
        else
        {
            Debug.LogWarning("Build folder doesn't exist yet — run \"Third Door/Build Windows EXE\" first.");
        }
    }
}
