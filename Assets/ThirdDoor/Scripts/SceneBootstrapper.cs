using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SceneBootstrapper : MonoBehaviour
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
        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null)
        {
            DestroyImmediateSafe(oldRoot);
        }

        CreateMaterials();
        chineseUiFont = CreateChineseUiFont();

        GameObject root = new GameObject(RootName);
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.025f, 0.025f, 0.03f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogDensity = 0.018f;

        BuildRooms(root.transform);
        BuildEndingEnvironment(root.transform);
        BuildPlayer(root.transform);
        BuildLighting(root.transform);
        BuildUI(root.transform);
        BuildManagers(root.transform);
        BuildVoiceDoors(root.transform);
        BuildThirdDoor(root.transform);
        BuildFinalChoiceZone(root.transform);
        BuildPostProcessing(root.transform);
        WireLockedFeedback();

        gameStateManager.Configure(doorController, endingManager, dialogueUI);
        gameStateManager.ConfigureSymbolicDoors(redDoorController, blueDoorController);
        endingManager.Configure(dialogueUI, middleLight);
        endingManager.ConfigureEnvironment(exitBackLight, redPressureWall, bluePressureWall, guidanceLineRenderers, sideDoorRenderers, middleCeiling);
        doorController.UpdateDoorState(0);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLoop(GameSound.AmbientDrone, 0.30f);
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
        redRoomMat = CreateMat("RedRoom_Mat", new Color(1f, 0.02f, 0.025f), 0f);
        blueRoomMat = CreateMat("BlueRoom_Mat", new Color(0.02f, 0.18f, 1f), 0f);
        middleRoomMat = CreateMat("MiddleRoom_Mat", new Color(0.86f, 0.67f, 0.18f), 0f);
        blackDoorMat = CreateMat("BlackDoor_Mat", new Color(0f, 0f, 0f), 0f);
        whiteGlowMat = CreateMat("WhiteGlow_Mat", Color.white, 4f);
        darkWallMat = CreateMat("DarkWall_Mat", new Color(0.025f, 0.025f, 0.03f), 0f);
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

    private void BuildRooms(Transform root)
    {
        Transform rooms = new GameObject("Symbolic Rooms").transform;
        rooms.SetParent(root);

        CreateBox("Middle Floor", new Vector3(0f, -0.05f, 1.75f), new Vector3(6f, 0.1f, 9.5f), middleRoomMat, rooms);
        CreateBox("Red Floor", new Vector3(-6.5f, -0.05f, 0.5f), new Vector3(7f, 0.1f, 6.5f), redRoomMat, rooms);
        CreateBox("Blue Floor", new Vector3(6.5f, -0.05f, 0.5f), new Vector3(7f, 0.1f, 6.5f), blueRoomMat, rooms);

        CreateBox("Middle Back Wall", new Vector3(0f, 1.5f, -3f), new Vector3(6f, 3f, 0.25f), middleRoomMat, rooms);
        CreateBox("Middle Front Left Wall", new Vector3(-2.2f, 1.5f, 6.55f), new Vector3(1.6f, 3f, 0.25f), darkWallMat, rooms);
        CreateBox("Middle Front Right Wall", new Vector3(2.2f, 1.5f, 6.55f), new Vector3(1.6f, 3f, 0.25f), darkWallMat, rooms);
        CreateBox("Middle Left Rear Side Wall", new Vector3(-3f, 1.5f, -1.8f), new Vector3(0.25f, 3f, 2.4f), darkWallMat, rooms);
        CreateBox("Middle Left Front Side Wall", new Vector3(-3f, 1.5f, 4.08f), new Vector3(0.25f, 3f, 4.95f), darkWallMat, rooms);
        CreateBox("Middle Left Door Header", new Vector3(-3f, 2.55f, 0.5f), new Vector3(0.25f, 0.9f, 2.2f), darkWallMat, rooms);
        CreateBox("Middle Right Rear Side Wall", new Vector3(3f, 1.5f, -1.8f), new Vector3(0.25f, 3f, 2.4f), darkWallMat, rooms);
        CreateBox("Middle Right Front Side Wall", new Vector3(3f, 1.5f, 4.08f), new Vector3(0.25f, 3f, 4.95f), darkWallMat, rooms);
        CreateBox("Middle Right Door Header", new Vector3(3f, 2.55f, 0.5f), new Vector3(0.25f, 0.9f, 2.2f), darkWallMat, rooms);
        middleCeiling = CreateBox("Middle Ceiling", new Vector3(0f, 3.05f, 1.75f), new Vector3(6.1f, 0.35f, 9.6f), middleRoomMat, rooms).transform;

        CreateBox("Red Back Wall", new Vector3(-6.5f, 1.5f, -2.75f), new Vector3(7f, 3f, 0.25f), redRoomMat, rooms);
        CreateBox("Red Front Wall", new Vector3(-6.5f, 1.5f, 3.75f), new Vector3(7f, 3f, 0.25f), redRoomMat, rooms);
        CreateBox("Red Left Wall", new Vector3(-10f, 1.5f, 0.5f), new Vector3(0.25f, 3f, 6.5f), darkWallMat, rooms);
        CreateBox("Red Door Top", new Vector3(-3f, 2.55f, 0.5f), new Vector3(0.25f, 0.9f, 2.2f), darkWallMat, rooms);
        CreateBox("Red Door Rear Edge", new Vector3(-3f, 1.5f, -1.68f), new Vector3(0.25f, 3f, 2.15f), darkWallMat, rooms);
        CreateBox("Red Door Front Edge", new Vector3(-3f, 1.5f, 2.48f), new Vector3(0.25f, 3f, 2.15f), darkWallMat, rooms);
        CreateBox("Red Ceiling", new Vector3(-6.5f, 3.05f, 0.5f), new Vector3(7.1f, 0.35f, 6.6f), redRoomMat, rooms);

        CreateBox("Blue Back Wall", new Vector3(6.5f, 1.5f, -2.75f), new Vector3(7f, 3f, 0.25f), blueRoomMat, rooms);
        CreateBox("Blue Front Wall", new Vector3(6.5f, 1.5f, 3.75f), new Vector3(7f, 3f, 0.25f), blueRoomMat, rooms);
        CreateBox("Blue Right Wall", new Vector3(10f, 1.5f, 0.5f), new Vector3(0.25f, 3f, 6.5f), darkWallMat, rooms);
        CreateBox("Blue Door Top", new Vector3(3f, 2.55f, 0.5f), new Vector3(0.25f, 0.9f, 2.2f), darkWallMat, rooms);
        CreateBox("Blue Door Rear Edge", new Vector3(3f, 1.5f, -1.68f), new Vector3(0.25f, 3f, 2.15f), darkWallMat, rooms);
        CreateBox("Blue Door Front Edge", new Vector3(3f, 1.5f, 2.48f), new Vector3(0.25f, 3f, 2.15f), darkWallMat, rooms);
        CreateBox("Blue Ceiling", new Vector3(6.5f, 3.05f, 0.5f), new Vector3(7.1f, 0.35f, 6.6f), blueRoomMat, rooms);

        CreateBox("Exit Black Header", new Vector3(0f, 2.75f, 6.55f), new Vector3(3.8f, 0.6f, 0.35f), darkWallMat, rooms);
        CreateBox("Exit Left Jamb", new Vector3(-1.55f, 1.5f, 6.55f), new Vector3(0.25f, 3f, 0.35f), darkWallMat, rooms);
        CreateBox("Exit Right Jamb", new Vector3(1.55f, 1.5f, 6.55f), new Vector3(0.25f, 3f, 0.35f), darkWallMat, rooms);

        // Corridor floor beyond the third door so the player can step through after the good ending without falling into void.
        CreateBox("Exit Corridor Floor", new Vector3(0f, -0.05f, 8.4f), new Vector3(2.8f, 0.1f, 3.8f), middleRoomMat, rooms);
    }

    private void BuildGuidanceLines(Transform root)
    {
        Transform lines = new GameObject("Floor Guidance Lines").transform;
        lines.SetParent(root);

        GameObject redLine = CreateBox("Red To Middle Line", new Vector3(-3.1f, 0.012f, 0.5f), new Vector3(3.8f, 0.025f, 0.08f), whiteGlowMat, lines);
        GameObject blueLine = CreateBox("Blue To Middle Line", new Vector3(3.1f, 0.012f, 0.5f), new Vector3(3.8f, 0.025f, 0.08f), whiteGlowMat, lines);
        GameObject exitLine = CreateBox("Middle To Exit Line", new Vector3(0f, 0.012f, 3.95f), new Vector3(0.08f, 0.025f, 4.6f), whiteGlowMat, lines);
        guidanceLineRenderers = new[]
        {
            redLine.GetComponent<Renderer>(),
            blueLine.GetComponent<Renderer>(),
            exitLine.GetComponent<Renderer>()
        };
    }

    private void BuildWorldLabels(Transform root)
    {
        Transform labels = new GameObject("World Labels").transform;
        labels.SetParent(root);

        CreateWorldLabel("红门", new Vector3(-6.5f, 2.25f, -2.58f), Quaternion.Euler(0f, 0f, 0f), Color.white, labels);
        CreateWorldLabel("蓝门", new Vector3(6.5f, 2.25f, -2.58f), Quaternion.Euler(0f, 0f, 0f), Color.white, labels);
        CreateWorldLabel("第三扇门", new Vector3(0f, 2.35f, 6.28f), Quaternion.Euler(0f, 180f, 0f), Color.white, labels);
    }

    private void CreateWorldLabel(string labelText, Vector3 position, Quaternion rotation, Color color, Transform parent)
    {
        GameObject labelObject = new GameObject(labelText + " Label");
        labelObject.transform.SetParent(parent);
        labelObject.transform.SetPositionAndRotation(position, rotation);

        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = labelText;
        textMesh.fontSize = 72;
        textMesh.characterSize = 0.045f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;
        textMesh.font = chineseUiFont != null ? chineseUiFont : CreateChineseUiFont();

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        if (renderer != null && textMesh.font != null)
        {
            renderer.sharedMaterial = textMesh.font.material;
        }
    }

    private void BuildEndingEnvironment(Transform root)
    {
        Transform ending = new GameObject("Ending Environment Props").transform;
        ending.SetParent(root);

        redPressureWall = CreateBox("Red Pressure Wall", new Vector3(-2.35f, 1.45f, 1.7f), new Vector3(0.22f, 2.9f, 8.5f), redRoomMat, ending).transform;
        bluePressureWall = CreateBox("Blue Pressure Wall", new Vector3(2.35f, 1.45f, 1.7f), new Vector3(0.22f, 2.9f, 8.5f), blueRoomMat, ending).transform;
        redPressureWall.gameObject.SetActive(false);
        bluePressureWall.gameObject.SetActive(false);

        exitBackLight = CreateLight("Exit Back White Light", new Vector3(0f, 1.8f, 7.5f), Color.white, 0f, 1f, ending);
        exitBackLight.enabled = false;
    }

    private void BuildPlayer(Transform root)
    {
        GameObject playerObject = new GameObject("Player");
        playerObject.transform.SetParent(root);
        playerObject.transform.position = new Vector3(0f, 0.05f, 0f);

        CharacterController controller = playerObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        GameObject cameraObject = new GameObject("First Person Camera");
        cameraObject.transform.SetParent(playerObject.transform);
        cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        cameraObject.transform.localRotation = Quaternion.identity;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 68f;
        camera.nearClipPlane = 0.03f;
        cameraObject.tag = "MainCamera";

        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;

        // AddComponent<Camera> doesn't auto-attach an AudioListener (the Editor's default camera does, but ours doesn't).
        // Without this, every AudioSource fires silently and Console spams "no audio listeners in the scene".
        cameraObject.AddComponent<AudioListener>();

        player = playerObject.AddComponent<PlayerController>();
    }

    private void BuildLighting(Transform root)
    {
        GameObject directional = new GameObject("Hard Stage Directional Light");
        directional.transform.SetParent(root);
        directional.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        Light sun = directional.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.Hard;

        middleLight = CreateLight("Middle Yellow Light", new Vector3(0f, 2.6f, 1.2f), new Color(1f, 0.79f, 0.3f), 2.6f, 7f, root);
        // Red / Blue room interiors are intentionally unlit so the rooms read as black through any door gap.
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

    private void BuildThirdDoor(Transform root)
    {
        // Frame opening is ~2.85 wide (x: -1.425 to 1.425) and 2.45 tall (y: 0 to 2.45).
        const float panelWidth = 2.84f;
        const float panelHeight = 2.45f;
        const float panelDepth = 0.12f;
        Vector3 doorCenter = new Vector3(0f, panelHeight * 0.5f, 6.58f);

        GameObject doorRoot = new GameObject("Exit Door Interactable");
        doorRoot.transform.SetParent(root);
        doorRoot.transform.position = doorCenter;

        // Cube panel rotated 180° so its face points back toward the player. Keeps its default collider for blocking + raycast.
        GameObject panel = CreateDoorPanel("Third Door Model", doorCenter, Quaternion.Euler(0f, 180f, 0f), blackDoorMat, doorRoot.transform, panelHeight, panelWidth, panelDepth);

        // No printed label on the third door — the door itself is the statement.

        Transform movingLeaf = CreateDoorLeafPivot(panel, "Third Door Leaf Pivot", -1f);

        // Slit hidden permanently — only the full open of the good ending lets light through.
        GameObject glow = CreateBox("White Exit Slit", new Vector3(0f, panelHeight * 0.5f, 6.58f - panelDepth * 0.5f - 0.04f), new Vector3(0.06f, panelHeight - 0.2f, 0.06f), whiteGlowMat, doorRoot.transform);
        StripColliders(glow);
        Light slitLight = CreateLight("Exit Slit Light", new Vector3(0f, panelHeight * 0.5f + 0.1f, 6.2f), Color.white, 0f, 5f, doorRoot.transform);
        slitLight.enabled = false;

        doorController = doorRoot.AddComponent<DoorController>();
        doorController.Configure(movingLeaf, glow.transform, slitLight, panel.GetComponent<Renderer>(), blackDoorMat, whiteGlowMat);

        DoorDialogue interactable = doorRoot.AddComponent<DoorDialogue>();
        interactable.Configure(DialogueActor.ExitDoor, dialogueUI);
        interactable.ConfigurePrompt("第三扇门");
    }

    private void BuildVoiceDoors(Transform root)
    {
        Transform doors = new GameObject("Voice Doors").transform;
        doors.SetParent(root);

        GameObject redDoor = CreateVoiceDoor("红门", DialogueActor.RedDoor, new Vector3(-3.08f, 1.05f, 0.4f), Quaternion.Euler(0f, 90f, 0f), redRoomMat, doors);
        GameObject blueDoor = CreateVoiceDoor("蓝门", DialogueActor.BlueDoor, new Vector3(3.08f, 1.05f, 0.4f), Quaternion.Euler(0f, -90f, 0f), blueRoomMat, doors);

        redDoorController = redDoor.GetComponent<SymbolicDoorController>();
        blueDoorController = blueDoor.GetComponent<SymbolicDoorController>();

        sideDoorRenderers = new[]
        {
            redDoor.GetComponentInChildren<Renderer>(),
            blueDoor.GetComponentInChildren<Renderer>()
        };
    }

    private GameObject CreateVoiceDoor(string doorName, DialogueActor actor, Vector3 position, Quaternion rotation, Material material, Transform parent)
    {
        // Side openings are ~2.01 wide (z) and 2.1 tall (y). Panel fills the opening almost exactly.
        const float panelWidth = 2.0f;
        const float panelHeight = 2.1f;
        const float panelDepth = 0.12f;

        GameObject doorRoot = new GameObject(doorName + " Interactable");
        doorRoot.transform.SetParent(parent);
        doorRoot.transform.position = position;

        // Pure-black unlit backing on the room-side of the panel — reads as absolute void through any crack, regardless of scene lighting.
        Vector3 backingOffset = rotation * new Vector3(0f, 0f, -(panelDepth * 0.5f + 0.06f));
        GameObject backing = CreateBox(doorName + " Shadow Backing", position + backingOffset, new Vector3(2.4f, 2.4f, 0.08f), voidBlackMat, doorRoot.transform);
        backing.transform.rotation = rotation;
        StripColliders(backing);

        // Panel keeps its default CreatePrimitive collider — that gives the door (a) solid blocking and (b) a raycast target for InteractionPrompt.
        GameObject panel = CreateDoorPanel(doorName + " Model", position, rotation, material, doorRoot.transform, panelHeight, panelWidth, panelDepth);

        // Fixed printed label on the door panel — MOM for the red door, KID for the blue door.
        string labelText = actor == DialogueActor.RedDoor ? "MOM" : "KID";
        CreateDoorLabel(panel, labelText, panelDepth);

        Transform movingLeaf = CreateDoorLeafPivot(panel, doorName + " Leaf Pivot", actor == DialogueActor.RedDoor ? -1f : 1f);

        // Slit GameObject still created so the controller has a reference; controller keeps it hidden permanently.
        Vector3 slitLocalOffset = new Vector3(0f, 0f, panelDepth * 0.5f + 0.04f);
        GameObject glow = CreateBox(doorName + " White Slit", position + rotation * slitLocalOffset, new Vector3(0.05f, panelHeight - 0.2f, 0.05f), whiteGlowMat, doorRoot.transform);
        glow.transform.rotation = rotation;
        StripColliders(glow);
        Light glowLight = CreateLight(doorName + " White Light", position + rotation * new Vector3(0f, 0.05f, 0.55f), Color.white, 0f, 4f, doorRoot.transform);
        glowLight.enabled = false;

        SymbolicDoorController symbolicDoor = doorRoot.AddComponent<SymbolicDoorController>();
        symbolicDoor.Configure(movingLeaf, glow.transform, glowLight, material, whiteGlowMat, Vector3.zero, actor == DialogueActor.RedDoor ? 76f : -76f);

        DoorDialogue dialogue = doorRoot.AddComponent<DoorDialogue>();
        dialogue.Configure(actor, dialogueUI);
        dialogue.ConfigurePrompt(doorName);

        return doorRoot;
    }

    private void BuildFinalChoiceZone(Transform root)
    {
        GameObject finalChoiceZone = new GameObject("Middle Final Choice Zone");
        finalChoiceZone.transform.SetParent(root);
        finalChoiceZone.transform.position = new Vector3(0f, 1.25f, 1.5f);
        BoxCollider finalTrigger = finalChoiceZone.AddComponent<BoxCollider>();
        finalTrigger.isTrigger = true;
        finalTrigger.size = new Vector3(4.6f, 2.5f, 6.2f);
        finalChoiceZone.AddComponent<FinalChoiceZone>().Configure(dialogueUI);
    }

    private void BuildUI(Transform root)
    {
        GameObject eventSystem = new GameObject("Third Door EventSystem");
        eventSystem.transform.SetParent(root);
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        GameObject canvasObject = new GameObject("Third Door UI");
        canvasObject.transform.SetParent(root);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        dialogueUI = canvasObject.AddComponent<DialogueUI>();

        Text prompt = CreateText("Interaction Prompt", canvasObject.transform, "按 E 交互", 32, TextAnchor.MiddleCenter);
        RectTransform promptRect = prompt.rectTransform;
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 78f);
        promptRect.sizeDelta = new Vector2(560f, 60f);
        prompt.gameObject.SetActive(false);

        Text status = CreateText("Status Text", canvasObject.transform, string.Empty, 26, TextAnchor.UpperLeft);
        RectTransform statusRect = status.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(0f, 1f);
        statusRect.pivot = new Vector2(0f, 1f);
        statusRect.anchoredPosition = new Vector2(34f, -28f);
        statusRect.sizeDelta = new Vector2(760f, 78f);
        status.color = new Color(1f, 0.95f, 0.72f);
        status.gameObject.SetActive(false);

        Text ending = CreateText("Ending Text", canvasObject.transform, string.Empty, 56, TextAnchor.MiddleCenter);
        ending.fontStyle = FontStyle.Bold;
        ending.lineSpacing = 1.3f;
        RectTransform endingRect = ending.rectTransform;
        endingRect.anchorMin = new Vector2(0.5f, 0.5f);
        endingRect.anchorMax = new Vector2(0.5f, 0.5f);
        endingRect.pivot = new Vector2(0.5f, 0.5f);
        endingRect.sizeDelta = new Vector2(1400f, 360f);
        ending.color = Color.white;

        // ─── Constructivist dialogue panel ───
        // Dark base panel + a thick colored speaker bar on the left + thin underline below the speaker name.
        const float panelWidth = 1240f;
        const float panelHeight = 460f;
        const float barWidth = 56f;
        const float contentPaddingLeft = 96f;
        const float contentWidth = panelWidth - contentPaddingLeft - 36f;

        GameObject panel = new GameObject("Dialogue Panel");
        panel.transform.SetParent(canvasObject.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.04f, 0.05f, 0.95f);
        CanvasGroup group = panel.AddComponent<CanvasGroup>();
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 130f);
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        // Thick speaker-color bar on the left edge — dynamic color set by DialogueUI when a speaker shows up.
        GameObject barObject = new GameObject("Speaker Color Bar");
        barObject.transform.SetParent(panel.transform, false);
        Image barImage = barObject.AddComponent<Image>();
        barImage.color = Color.white;
        barImage.raycastTarget = false;
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(0f, 1f);
        barRect.pivot = new Vector2(0f, 0.5f);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = new Vector2(barWidth, 0f);

        // Top edge accent — 2px line in the same speaker color across the top of the panel.
        GameObject outlineObject = new GameObject("Panel Top Accent");
        outlineObject.transform.SetParent(panel.transform, false);
        Image outlineImage = outlineObject.AddComponent<Image>();
        outlineImage.color = new Color(1f, 1f, 1f, 0.6f);
        outlineImage.raycastTarget = false;
        RectTransform outlineRect = outlineObject.GetComponent<RectTransform>();
        outlineRect.anchorMin = new Vector2(0f, 1f);
        outlineRect.anchorMax = new Vector2(1f, 1f);
        outlineRect.pivot = new Vector2(0.5f, 1f);
        outlineRect.anchoredPosition = Vector2.zero;
        outlineRect.sizeDelta = new Vector2(0f, 2f);

        Text speaker = CreateText("Speaker", panel.transform, string.Empty, 38, TextAnchor.LowerLeft);
        speaker.fontStyle = FontStyle.Bold;
        speaker.color = Color.white;
        SetRect(speaker.rectTransform, new Vector2(contentPaddingLeft, -36f), new Vector2(contentWidth, 52f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        // Thin speaker underline — short colored stroke under the speaker name.
        GameObject underlineObject = new GameObject("Speaker Underline");
        underlineObject.transform.SetParent(panel.transform, false);
        Image underlineImage = underlineObject.AddComponent<Image>();
        underlineImage.color = Color.white;
        underlineImage.raycastTarget = false;
        RectTransform underlineRect = underlineObject.GetComponent<RectTransform>();
        underlineRect.anchorMin = new Vector2(0f, 1f);
        underlineRect.anchorMax = new Vector2(0f, 1f);
        underlineRect.pivot = new Vector2(0f, 1f);
        underlineRect.anchoredPosition = new Vector2(contentPaddingLeft, -94f);
        underlineRect.sizeDelta = new Vector2(96f, 3f);

        Text line = CreateText("Line", panel.transform, string.Empty, 36, TextAnchor.UpperLeft);
        line.color = new Color(0.96f, 0.96f, 0.96f);
        line.lineSpacing = 1.2f;
        SetRect(line.rectTransform, new Vector2(contentPaddingLeft, -118f), new Vector2(contentWidth, 110f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        Text result = CreateText("Result", panel.transform, string.Empty, 32, TextAnchor.UpperLeft);
        result.color = new Color(0.95f, 0.92f, 0.72f);
        result.lineSpacing = 1.2f;
        result.fontStyle = FontStyle.Bold;
        SetRect(result.rectTransform, new Vector2(contentPaddingLeft, -250f), new Vector2(contentWidth, 150f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        Button[] buttons = new Button[3];
        Text[] labels = new Text[3];
        for (int i = 0; i < buttons.Length; i++)
        {
            (buttons[i], labels[i]) = CreateButton("Option " + (i + 1), panel.transform, new Vector2(contentPaddingLeft, -252f - i * 60f), contentWidth);
        }

        dialogueUI.Configure(group, speaker, line, result, buttons, labels, status, ending, player, barImage, underlineImage, outlineImage);
        canvasObject.AddComponent<InteractionPrompt>().Configure(player, prompt);

        // Persistent control hints in the bottom-right corner.
        Text helpHint = CreateText("Help Hint", canvasObject.transform, "ESC 退出   ·   R 重新开始", 22, TextAnchor.LowerRight);
        helpHint.color = new Color(0.75f, 0.75f, 0.75f, 0.75f);
        RectTransform hintRect = helpHint.rectTransform;
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-28f, 22f);
        hintRect.sizeDelta = new Vector2(420f, 32f);

        BuildIntroOverlay(canvasObject.transform);
    }

    private void BuildCrosshair(Transform canvasTransform)
    {
        GameObject crosshair = new GameObject("Crosshair");
        crosshair.transform.SetParent(canvasTransform, false);
        Image dot = crosshair.AddComponent<Image>();
        dot.color = new Color(1f, 1f, 1f, 0.6f);
        dot.raycastTarget = false;
        RectTransform rect = crosshair.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(6f, 6f);
    }

    private void BuildIntroOverlay(Transform canvasTransform)
    {
        // Full-screen black panel that sits above every other UI element.
        GameObject introPanel = new GameObject("Intro Black Overlay");
        introPanel.transform.SetParent(canvasTransform, false);
        Image introImage = introPanel.AddComponent<Image>();
        introImage.color = Color.black;
        introImage.raycastTarget = true;
        CanvasGroup introGroup = introPanel.AddComponent<CanvasGroup>();
        introGroup.alpha = 1f;
        introGroup.blocksRaycasts = true;
        RectTransform introRect = introPanel.GetComponent<RectTransform>();
        introRect.anchorMin = Vector2.zero;
        introRect.anchorMax = Vector2.one;
        introRect.offsetMin = Vector2.zero;
        introRect.offsetMax = Vector2.zero;

        Text introText = CreateText("Intro Text", introPanel.transform, string.Empty, 64, TextAnchor.MiddleCenter);
        introText.color = Color.white;
        introText.fontStyle = FontStyle.Bold;
        introText.lineSpacing = 1.35f;
        RectTransform introTextRect = introText.rectTransform;
        introTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        introTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        introTextRect.pivot = new Vector2(0.5f, 0.5f);
        introTextRect.anchoredPosition = Vector2.zero;
        introTextRect.sizeDelta = new Vector2(1400f, 700f);

        Text skipHint = CreateText("Intro Skip Hint", introPanel.transform, "按任意键跳过", 20, TextAnchor.LowerCenter);
        skipHint.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
        RectTransform skipRect = skipHint.rectTransform;
        skipRect.anchorMin = new Vector2(0.5f, 0f);
        skipRect.anchorMax = new Vector2(0.5f, 0f);
        skipRect.pivot = new Vector2(0.5f, 0f);
        skipRect.anchoredPosition = new Vector2(0f, 60f);
        skipRect.sizeDelta = new Vector2(400f, 36f);

        IntroSequence intro = introPanel.AddComponent<IntroSequence>();
        intro.Configure(introGroup, introText, player);
    }

    private Text CreateText(string objectName, Transform parent, string text, int size, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        Text label = textObject.AddComponent<Text>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.font = chineseUiFont != null ? chineseUiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.supportRichText = false;
        label.color = Color.white;
        return label;
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

    private (Button, Text) CreateButton(string objectName, Transform parent, Vector2 anchoredPosition, float width = 1016f)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        // Very subtle background so the row reads as a flat strip rather than a button "card".
        image.color = new Color(0.10f, 0.10f, 0.12f, 0.55f);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 1.25f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        const float buttonHeight = 52f;
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetRect(rect, anchoredPosition, new Vector2(width, buttonHeight), new Vector2(0f, 1f), new Vector2(0f, 1f));

        Text label = CreateText("Label", buttonObject.transform, string.Empty, 28, TextAnchor.MiddleLeft);
        label.color = new Color(0.96f, 0.96f, 0.96f);
        SetRect(label.rectTransform, new Vector2(20f, 0f), new Vector2(width - 40f, buttonHeight), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        return (button, label);
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor, Vector2 pivot)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
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
        bloom.intensity.Override(0.45f);
        bloom.threshold.Override(0.72f);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.28f);
        vignette.smoothness.Override(0.48f);

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.contrast.Override(36f);
        color.saturation.Override(24f);
        color.postExposure.Override(-0.15f);
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

    // Simple cube-based door panel. No FBX, no prefab, no bounds recentring.
    private GameObject CreateDoorPanel(string objectName, Vector3 position, Quaternion rotation, Material material, Transform parent, float panelHeight, float panelWidth, float panelDepth)
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = objectName;
        panel.transform.SetParent(parent);
        panel.transform.SetPositionAndRotation(position, rotation);
        panel.transform.localScale = new Vector3(panelWidth, panelHeight, panelDepth);
        panel.GetComponent<Renderer>().sharedMaterial = material;
        return panel;
    }

    // Each character of the door label is its own GameObject with a TextMesh + trigger volume.
    // The letters float ~0.4m in front of the door panel — the player walking toward the door
    // naturally collides with them, and each letter flies away on contact (Edith Finch style).
    // The label group is mirrored on X so the word reads correctly from the player side.
    private void CreateDoorLabel(GameObject panel, string text, float panelDepth)
    {
        GameObject labelGroup = new GameObject("Door Label");
        labelGroup.transform.SetParent(panel.transform, false);

        // Position the label group ~1.0m in front of the panel's player-facing face (local Z = +0.5 is the face).
        // Far enough out that the player encounters and shatters the word well before reaching the door itself.
        const float frontOffsetWorld = 1.0f;
        float frontLocalZ = 0.5f + frontOffsetWorld / panelDepth;
        labelGroup.transform.localPosition = new Vector3(0f, 0f, frontLocalZ);
        labelGroup.transform.localRotation = Quaternion.identity;

        // Counter the panel's localScale so text isn't distorted; flip X so the word reads in player's left-to-right order.
        Vector3 panelScale = panel.transform.localScale;
        labelGroup.transform.localScale = new Vector3(-1f / panelScale.x, 1f / panelScale.y, 1f / panelScale.z);

        const float characterSize = 0.18f;
        const float letterSpacing = 1.20f;    // letters span well beyond a single glyph's width — clearly discrete objects floating in space
        const float triggerWidth = 1.40f;     // overlap neighbouring triggers so the cascade catches any approach angle
        const float triggerHeight = 0.50f;
        const float triggerDepth = 1.00f;     // generous depth so the trigger fires while the player is still well away from the door
        Font font = chineseUiFont != null ? chineseUiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");

        int count = text.Length;
        float startX = -letterSpacing * (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            char ch = text[i];
            GameObject letter = new GameObject("Letter " + ch);
            letter.transform.SetParent(labelGroup.transform, false);
            letter.transform.localPosition = new Vector3(startX + i * letterSpacing, 0f, 0f);
            letter.transform.localRotation = Quaternion.identity;
            letter.transform.localScale = Vector3.one;

            TextMesh textMesh = letter.AddComponent<TextMesh>();
            textMesh.text = ch.ToString();
            textMesh.fontSize = 80;
            textMesh.characterSize = characterSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.font = font;

            MeshRenderer renderer = letter.GetComponent<MeshRenderer>();
            if (renderer != null && font != null)
            {
                renderer.sharedMaterial = font.material;
            }

            BoxCollider trigger = letter.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.size = new Vector3(triggerWidth, triggerHeight, triggerDepth);

            letter.AddComponent<LetterDisperse>();
        }
    }

    // Attaches a pivot GameObject at the hinge edge of a cube panel so that rotating the pivot swings the panel cleanly.
    private static Transform CreateDoorLeafPivot(GameObject panel, string pivotName, float hingeSide)
    {
        Transform originalParent = panel.transform.parent;
        float halfWidth = panel.transform.localScale.x * 0.5f;
        Vector3 hingePoint = panel.transform.position + panel.transform.right * (halfWidth * hingeSide);

        GameObject pivot = new GameObject(pivotName);
        pivot.transform.SetParent(originalParent, true);
        pivot.transform.SetPositionAndRotation(hingePoint, panel.transform.rotation);
        panel.transform.SetParent(pivot.transform, true);
        return pivot.transform;
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
