using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DoorPreviewSceneBuilder
{
    private const string DoorAssetFolder = "Assets/ThirdDoor/ExternalAssets/ThirdDoor_Curated_AssetPack/Assets/DoorsAsset_FBX";
    private const string PreviewScenePath = "Assets/Scenes/DoorPreviewScene.unity";
    private const float TargetDoorHeight = 2.4f;
    private const float ColumnSpacing = 4.8f;
    private const float RowSpacing = 5.2f;

    [MenuItem("Third Door/Build Door Preview Scene")]
    public static void BuildDoorPreviewScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "DoorPreviewScene";

        Material red = CreatePreviewMaterial("Preview_RedDoor_Mat", new Color(1f, 0.02f, 0.03f));
        Material blue = CreatePreviewMaterial("Preview_BlueDoor_Mat", new Color(0.03f, 0.18f, 1f));
        Material black = CreatePreviewMaterial("Preview_BlackDoor_Mat", Color.black);
        Material floor = CreatePreviewMaterial("Preview_Floor_Mat", new Color(0.18f, 0.18f, 0.18f));

        CreateFloor(floor);
        CreateCameraAndLight();

        int doorCount = 0;
        for (int i = 1; i <= 17; i++)
        {
            string assetPath = $"{DoorAssetFolder}/Door_{i}_grp.fbx";
            GameObject doorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (doorAsset == null)
            {
                Debug.LogWarning("Door preview asset not found: " + assetPath);
                continue;
            }

            int column = doorCount % 6;
            int row = doorCount / 6;
            Vector3 position = new Vector3((column - 2.5f) * ColumnSpacing, 0f, row * RowSpacing);

            GameObject door = (GameObject)PrefabUtility.InstantiatePrefab(doorAsset);
            door.name = $"Door_{i:00}_Preview";
            door.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            door.transform.localScale = Vector3.one;

            ApplyMaterialByColumn(door, column, red, blue, black);
            NormalizeDoorToSlot(door, position);
            Bounds bounds = CalculateRendererBounds(door);

            CreateLabel($"Door {i}", new Vector3(position.x, bounds.max.y + 0.35f, position.z - 0.35f));
            CreateSelectionMarker(position, column);
            doorCount++;
        }

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, PreviewScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built door preview scene with {doorCount} doors: {PreviewScenePath}");
    }

    private static Material CreatePreviewMaterial(string materialName, Color color)
    {
        string folder = "Assets/ThirdDoor/Materials";
        Directory.CreateDirectory(folder);
        string path = $"{folder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = materialName;
        material.color = color;
        material.SetFloat("_Smoothness", 0.18f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateFloor(Material material)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Preview Floor";
        floor.transform.position = new Vector3(0f, -0.06f, 5.2f);
        floor.transform.localScale = new Vector3(31f, 0.1f, 18f);
        floor.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateCameraAndLight()
    {
        GameObject cameraObject = new GameObject("Preview Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 7.0f, -15.0f);
        cameraObject.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 52f;

        GameObject lightObject = new GameObject("Preview Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.2f;
        light.shadows = LightShadows.Hard;
    }

    private static void ApplyMaterialByColumn(GameObject root, int column, Material red, Material blue, Material black)
    {
        Material material = column % 3 == 0 ? red : column % 3 == 1 ? blue : black;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static void NormalizeDoorToSlot(GameObject door, Vector3 slotPosition)
    {
        Bounds bounds = CalculateRendererBounds(door);
        if (bounds.size.y <= 0.001f)
        {
            door.transform.position = slotPosition;
            return;
        }

        float scaleFactor = TargetDoorHeight / bounds.size.y;
        door.transform.localScale *= scaleFactor;

        bounds = CalculateRendererBounds(door);
        Vector3 desiredCenter = new Vector3(slotPosition.x, bounds.extents.y, slotPosition.z);
        Vector3 offset = desiredCenter - bounds.center;
        door.transform.position += offset;
    }

    private static Bounds CalculateRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static void CreateLabel(string label, Vector3 position)
    {
        GameObject labelObject = new GameObject(label + " Label");
        labelObject.transform.position = position;
        labelObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.text = label;
        text.fontSize = 48;
        text.characterSize = 0.04f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.white;
    }

    private static void CreateSelectionMarker(Vector3 position, int column)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Color Role Marker";
        marker.transform.position = position + new Vector3(0f, 0.02f, -1.3f);
        marker.transform.localScale = new Vector3(1.8f, 0.04f, 0.16f);
        Renderer renderer = marker.GetComponent<Renderer>();
        renderer.sharedMaterial = CreatePreviewMaterial(
            column % 3 == 0 ? "Preview_RedMarker_Mat" : column % 3 == 1 ? "Preview_BlueMarker_Mat" : "Preview_BlackMarker_Mat",
            column % 3 == 0 ? Color.red : column % 3 == 1 ? Color.blue : Color.black);
    }
}
