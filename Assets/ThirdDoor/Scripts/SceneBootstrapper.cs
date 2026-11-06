using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public partial class SceneBootstrapper : MonoBehaviour
{
    private const string RootName = "ThirdDoor_RuntimeRoot";

    [SerializeField] private bool rebuildOnAwakeIfMissing = true;

    private Material redRoomMat;
    private Material blueRoomMat;
    private Material middleRoomMat;
    private Material blackDoorMat;
    private Material whiteGlowMat;
    private Material darkWallMat;
    private Material voidBlackMat;

    private PlayerController player;
    private DialogueUI dialogueUI;
    private DoorController doorController;
    private EndingManager endingManager;
    private GameStateManager gameStateManager;
    private Light middleLight;
    private Light exitBackLight;
    private SymbolicDoorController redDoorController;
    private SymbolicDoorController blueDoorController;
    private Transform redPressureWall;
    private Transform bluePressureWall;
    private Renderer[] guidanceLineRenderers;
    private Renderer[] sideDoorRenderers;
    private Transform middleCeiling;
    private Volume postProcessingVolume;
    private LockedFeedback lockedFeedback;
    private Font chineseUiFont;
    private Texture2D paperGrain;

    private void Awake()
    {
        // Always rebuild on Play so stale geometry from prior editor builds (via EditorApplication.delayCall)
        // never persists into runtime. BuildScene first destroys any existing root.
        if (Application.isPlaying && rebuildOnAwakeIfMissing)
        {
            BuildScene();
        }
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && rebuildOnAwakeIfMissing)
        {
            EditorApplication.delayCall -= BuildSceneInEditorDelay;
            EditorApplication.delayCall += BuildSceneInEditorDelay;
        }
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall -= BuildSceneInEditorDelay;
#endif
    }

#if UNITY_EDITOR
    private void BuildSceneInEditorDelay()
    {
        EditorApplication.delayCall -= BuildSceneInEditorDelay;

        if (this == null || Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
        {
            return;
        }

        BuildScene();
    }
#endif

    [ContextMenu("Build Third Door Scene")]
    public void BuildScene()
    {
        if (Application.isPlaying)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            QualitySettings.shadowDistance = 70;
        }
        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null)
        {
            // Disable immediately: deferred destruction must not leave duplicate managers/listeners alive.
            oldRoot.SetActive(false);
            DestroyImmediateSafe(oldRoot);
        }

        CreateMaterials();
        chineseUiFont = CreateChineseUiFont();
        NarrativeFontWarmup.BodyFont = chineseUiFont;
        NarrativeFontWarmup.Warm();

        GameObject root = new GameObject(RootName);
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.60f, 0.58f, 0.53f);
        RenderSettings.fog = false;

        BuildConstructivistArchitecture(root.transform);
        BuildPlayer(root.transform);
        BuildConstructivistLighting(root.transform);
        BuildExhibitionUI(root.transform);
        BuildManagers(root.transform);
        BuildConstructivistDoors(root.transform);
        BuildNarrativeInstallation(root.transform);
        BuildCaseInstallation(root.transform);
        BuildIllustrationInstallation(root.transform);
        BuildPostProcessing(root.transform);
        WireLockedFeedback();

        gameStateManager.Configure(doorController, endingManager, dialogueUI);
        gameStateManager.ConfigureSymbolicDoors(redDoorController, blueDoorController);
        endingManager.Configure(dialogueUI, middleLight);
        endingManager.ConfigureEnvironment(exitBackLight, redPressureWall, bluePressureWall, guidanceLineRenderers, sideDoorRenderers, middleCeiling);
        doorController.UpdateDoorState(0);
        root.AddComponent<ExhibitionMotion>().Configure(player);
        root.AddComponent<ExhibitionResourceLifetime>().Own(redRoomMat,blueRoomMat,middleRoomMat,blackDoorMat,whiteGlowMat,darkWallMat,voidBlackMat,printInk,printRed,printPaper,chineseUiFont,paperGrain,postProcessingVolume.profile);
        DemoValidation.AttachIfRequested(root);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLoop(GameSound.AmbientDrone, 0.16f);
        }
    }

    private void WireLockedFeedback()
    {
        if (lockedFeedback != null && postProcessingVolume != null)
        {
            lockedFeedback.Configure(postProcessingVolume);
        }

        foreach (DoorDialogue dialogue in FindObjectsOfType<DoorDialogue>())
        {
            dialogue.ConfigureLockedFeedback(lockedFeedback);
        }
    }

    private void CreateMaterials()
    {
        redRoomMat = CreateMat("RedRoom_Mat", ExhibitionDesign.Red, 0f);
        blueRoomMat = CreateMat("BlueRoom_Mat", ExhibitionDesign.Ink, 0f);
        middleRoomMat = CreateMat("MiddleRoom_Mat", new Color(.86f, .835f, .75f), 0f);
        paperGrain = new Texture2D(128,128,TextureFormat.RGBA32,false);
        paperGrain.name="Paper fibres / procedural";paperGrain.wrapMode=TextureWrapMode.Repeat;
        var fibres=new Color32[128*128];var random=new System.Random(1923);
        for(int i=0;i<fibres.Length;i++)
        {
            byte value=(byte)(245+random.Next(11));
            fibres[i]=new Color32(value,value,value,255);
        }
        paperGrain.SetPixels32(fibres);paperGrain.Apply(false,true);
        middleRoomMat.SetTexture("_BaseMap",paperGrain);middleRoomMat.SetTextureScale("_BaseMap",new Vector2(12,12));
        middleRoomMat.SetFloat("_Smoothness",.02f);
        blackDoorMat = CreateMat("BlackDoor_Mat", ExhibitionDesign.Ink, 0f);
        whiteGlowMat = CreateMat("WhiteGlow_Mat", ExhibitionDesign.Paper, 0.3f);
        darkWallMat = CreateMat("DarkWall_Mat", ExhibitionDesign.Ink, 0f);
        voidBlackMat = CreateUnlitMat("Void_Black_Mat", Color.black);
    }

    // Pure black, ignores lighting entirely. Used behind doors to give a true "void" reading through any crack.
    private static Material CreateUnlitMat(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        Material material = new Material(shader) { name = materialName, color = color };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        return material;
    }

    private static Material CreateMat(string materialName, Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            name = materialName,
            color = color
        };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        material.SetFloat("_Smoothness", 0.16f);

        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }

        return material;
    }

    private void BuildPlayer(Transform root)
    {
        GameObject playerObject = new GameObject("Player");
        playerObject.transform.SetParent(root);
        playerObject.transform.position = new Vector3(0f, 0.05f, -5f);

        CharacterController controller = playerObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        GameObject cameraObject = new GameObject("First Person Camera");
        cameraObject.transform.SetParent(playerObject.transform);
        cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        cameraObject.transform.localRotation = Quaternion.identity;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 64f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 90f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = ExhibitionDesign.Paper;
        cameraObject.tag = "MainCamera";

        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;

        // AddComponent<Camera> doesn't auto-attach an AudioListener (the Editor's default camera does, but ours doesn't).
        // Without this, every AudioSource fires silently and Console spams "no audio listeners in the scene".
        cameraObject.AddComponent<AudioListener>();

        player = playerObject.AddComponent<PlayerController>();
    }

    private Light CreateLight(string lightName, Vector3 position, Color color, float intensity, float range, Transform root)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(root);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Hard;
        return light;
    }

    private Font CreateChineseUiFont()
    {
        Font chineseFont = Font.CreateDynamicFontFromOSFont(
            new[]
            {
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "微软雅黑",
                "SimHei",
                "黑体",
                "SimSun",
                "宋体",
                "Arial Unicode MS",
                "Noto Sans CJK SC"
            },
            32);

        if (chineseFont != null)
        {
            return chineseFont;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void BuildManagers(Transform root)
    {
        GameObject managers = new GameObject("Game Managers");
        managers.transform.SetParent(root);
        gameStateManager = managers.AddComponent<GameStateManager>();
        endingManager = managers.AddComponent<EndingManager>();
        lockedFeedback = managers.AddComponent<LockedFeedback>();
        managers.AddComponent<AudioManager>();
        managers.AddComponent<PrototypeResetController>();
    }

    private void BuildPostProcessing(Transform root)
    {
        GameObject volumeObject = new GameObject("High Contrast Volume");
        volumeObject.transform.SetParent(root);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        postProcessingVolume = volume;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "ThirdDoor_Stylized_Volume";
        volume.profile = profile;

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.08f);
        bloom.threshold.Override(1.2f);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.16f);
        vignette.smoothness.Override(0.48f);

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.contrast.Override(12f);
        color.saturation.Override(-8f);
        color.postExposure.Override(0.1f);
    }

    private GameObject CreateBox(string objectName, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(parent);
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static void StripColliders(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            DestroyImmediateSafe(collider);
        }
    }

    private static void DestroyImmediateSafe(Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
