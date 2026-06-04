using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ThirdDoorSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string MaterialFolder = "Assets/ThirdDoor/Materials";

    [MenuItem("Third Door/Build Playable Prototype Scene")]
    public static void BuildPlayablePrototypeScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainScene";

        GameObject bootstrapperObject = new GameObject("SceneBootstrapper");
        SceneBootstrapper bootstrapper = bootstrapperObject.AddComponent<SceneBootstrapper>();
        bootstrapper.BuildScene();

        RebindSceneMaterials();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built The Third Door prototype scene at " + ScenePath);
    }

    private static void RebindSceneMaterials()
    {
        Directory.CreateDirectory(MaterialFolder);

        Dictionary<string, Material> materialAssets = new Dictionary<string, Material>
        {
            { "RedRoom_Mat", CreateOrLoadMaterial("RedRoom_Mat", new Color(1f, 0.02f, 0.025f), 0f) },
            { "BlueRoom_Mat", CreateOrLoadMaterial("BlueRoom_Mat", new Color(0.02f, 0.18f, 1f), 0f) },
            { "MiddleRoom_Mat", CreateOrLoadMaterial("MiddleRoom_Mat", new Color(0.86f, 0.67f, 0.18f), 0f) },
            { "BlackDoor_Mat", CreateOrLoadMaterial("BlackDoor_Mat", Color.black, 0f) },
            { "WhiteGlow_Mat", CreateOrLoadMaterial("WhiteGlow_Mat", Color.white, 4f) },
            { "DarkWall_Mat", CreateOrLoadMaterial("DarkWall_Mat", new Color(0.025f, 0.025f, 0.03f), 0f) }
        };

        foreach (Renderer renderer in Object.FindObjectsOfType<Renderer>())
        {
            Material current = renderer.sharedMaterial;
            if (current == null)
            {
                continue;
            }

            string cleanName = current.name.Replace(" (Instance)", string.Empty);
            if (materialAssets.TryGetValue(cleanName, out Material asset))
            {
                renderer.sharedMaterial = asset;
            }
        }

        // DoorController is already configured by SceneBootstrapper.BuildScene above.
    }

    private static Material CreateOrLoadMaterial(string materialName, Color color, float emission)
    {
        string path = $"{MaterialFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = materialName
            };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        material.SetFloat("_Smoothness", 0.16f);

        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }

        EditorUtility.SetDirty(material);
        return material;
    }
}
