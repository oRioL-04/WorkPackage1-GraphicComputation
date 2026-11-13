using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Menu WP4 > Setup Project: textures + materials, the quad-based Wall prefab, the Shader Graph material,
// the Game scene (pause HUD) and the MainMenu scene. It can be run as many times as needed.
public static class WP4Setup
{
    const string Mats = "Assets/Materials/";
    const string Tex = "Assets/Textures/";
    const string GameScenePath = "Assets/Scenes/Game.unity";
    const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("WP4/Setup Project")]
    public static void Setup()
    {
        SetupTextures();
        Material bricks = SetupMaterial("BricksMat", "Bricks_Color", "Bricks_Normal", new Vector2(0.7f, 0.35f));
        Material top = SetupMaterial("WallTopMat", "WallTop_Concrete", null, new Vector2(1f, 0.5f));
        SetupMaterial("FloorMat", "Floor_Color", "Floor_Normal", new Vector2(14f, 14f));        // tiling is adjusted from Maze3DBuilder
        SetupMaterial("DestructibleWallMat", "Planks_Color", "Planks_Normal", Vector2.one);
        Material goal = SetupGoalMaterial();

        BuildWallPrefab(bricks, top);

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene.unity") != null && !System.IO.File.Exists(GameScenePath))
            AssetDatabase.RenameAsset("Assets/Scenes/SampleScene.unity", "Game");

        SetupGameScene(goal);
        SetupMenuScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MenuScenePath);
        Debug.Log("WP4 setup finished. Open MainMenu and press Play.");
    }

    // ---------------------------------------------------------------- textures / materials

    static void SetupTextures()
    {
        AssetDatabase.Refresh();
        foreach (string normal in new[] { "Bricks_Normal", "Floor_Normal", "Planks_Normal" })
        {
            string path = Tex + normal + ".jpg";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            if (imp.textureType != TextureImporterType.NormalMap)
            {
                imp.textureType = TextureImporterType.NormalMap;
                imp.SaveAndReimport();
            }
        }
    }

    static Material SetupMaterial(string name, string albedo, string normal, Vector2 tiling)
    {
        string path = Mats + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + albedo + ".jpg"));   // "Albedo" slot
        m.SetTextureScale("_BaseMap", tiling);                                                        // "Tiling"
        if (normal != null)
        {
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + normal + ".jpg"));
            m.EnableKeyword("_NORMALMAP");
        }
        m.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material SetupGoalMaterial()
    {
        AssetDatabase.ImportAsset("Assets/Shaders/GoalPulse.shadergraph", ImportAssetOptions.ForceUpdate);
        Shader shader = Shader.Find("Shader Graphs/GoalPulse");
        if (shader == null)
        {
            Debug.LogError("Shader Graph 'GoalPulse' not found / failed to import.");
            return null;
        }
        string path = Mats + "GoalPulseMat.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        EditorUtility.SetDirty(m);

        int errors = ShaderUtil.GetShaderMessageCount(shader);
        Debug.Log($"GoalPulse shader supported={shader.isSupported}, messages={errors}");
        for (int i = 0; i < errors; i++) Debug.Log("  shader message: " + ShaderUtil.GetShaderMessages(shader)[i].message);
        return m;
    }

    // ---------------------------------------------------------------- wall prefab (5 quads)

    static void BuildWallPrefab(Material side, Material top)
    {
        var root = new GameObject("Wall");
        var box = root.AddComponent<BoxCollider>();
        box.size = Vector3.one;

        // Unity quads face -Z. Each face is placed on the cube surface, looking outwards.
        Quad(root, "Front",  new Vector3(0, 0, -0.5f),  Quaternion.Euler(0, 0, 0),   side);
        Quad(root, "Back",   new Vector3(0, 0, 0.5f),   Quaternion.Euler(0, 180, 0), side);
        Quad(root, "Right",  new Vector3(0.5f, 0, 0),   Quaternion.Euler(0, -90, 0), side);
        Quad(root, "Left",   new Vector3(-0.5f, 0, 0),  Quaternion.Euler(0, 90, 0),  side);
        Quad(root, "Top",    new Vector3(0, 0.5f, 0),   Quaternion.Euler(90, 0, 0),  top);   // different material

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Wall.prefab");
        Object.DestroyImmediate(root);
    }

    static void Quad(GameObject parent, string name, Vector3 pos, Quaternion rot, Material mat)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(parent.transform, false);
        q.transform.localPosition = pos;
        q.transform.localRotation = rot;
        q.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // ---------------------------------------------------------------- scenes

    static void SetupGameScene(Material goal)
    {
        EditorSceneManager.OpenScene(GameScenePath);

        var builder = Object.FindAnyObjectByType<Maze3DBuilder>();
        if (builder != null)
        {
            var spawner = builder.GetComponent<MazeSpawner>();
            if (spawner != null) { spawner.goalMaterial = goal; EditorUtility.SetDirty(spawner); }
        }

        // Pause HUD (part of the game scene, shown/hidden by enabling/disabling it)
        var old = GameObject.Find("PauseCanvas");
        if (old != null) Object.DestroyImmediate(old);

        GameObject canvas = CreateCanvas("PauseCanvas");
        var pause = canvas.AddComponent<PauseMenu>();

        GameObject panel = CreatePanel(canvas.transform, "PausePanel", new Color(0f, 0f, 0f, 0.7f));
        CreateText(panel.transform, "Title", "PAUSED", 90, new Vector2(0, 220), new Vector2(900, 120));
        CreateText(panel.transform, "Hint", "Press P to resume", 34, new Vector2(0, 130), new Vector2(900, 60));
        Button resume = CreateButton(panel.transform, "ResumeButton", "Resume", new Vector2(0, 20));
        Button menu = CreateButton(panel.transform, "MenuButton", "Main menu", new Vector2(0, -110));
        UnityEventTools.AddPersistentListener(resume.onClick, pause.Resume);
        UnityEventTools.AddPersistentListener(menu.onClick, pause.GoToMenu);
        pause.pausePanel = panel;
        panel.SetActive(false);

        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    static void SetupMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.12f);
        camGo.AddComponent<AudioListener>();

        GameObject canvas = CreateCanvas("MenuCanvas");
        var menu = canvas.AddComponent<MainMenu>();

        // Main panel
        GameObject main = CreatePanel(canvas.transform, "MainPanel", Color.clear);
        CreateText(main.transform, "Title", "3D MAZE", 120, new Vector2(0, 280), new Vector2(1200, 160));
        CreateText(main.transform, "Subtitle", "Collect the food, break the walls, reach the goal", 36, new Vector2(0, 190), new Vector2(1200, 60));
        Button play = CreateButton(main.transform, "PlayButton", "Play", new Vector2(0, 50));
        Button credits = CreateButton(main.transform, "CreditsButton", "Credits", new Vector2(0, -80));
        Button quit = CreateButton(main.transform, "QuitButton", "Quit game", new Vector2(0, -210));
        UnityEventTools.AddPersistentListener(play.onClick, menu.Play);
        UnityEventTools.AddPersistentListener(credits.onClick, menu.ShowCredits);
        UnityEventTools.AddPersistentListener(quit.onClick, menu.Quit);

        // Credits panel
        GameObject cr = CreatePanel(canvas.transform, "CreditsPanel", Color.clear);
        CreateText(cr.transform, "Title", "CREDITS", 90, new Vector2(0, 340), new Vector2(1200, 120));
        Text body = CreateText(cr.transform, "Body",
            "Game programmer: Oriol\n" +
            "Art director: Oriol\n" +
            "VFX designer: Oriol\n" +
            "Shader / Technical artist: Oriol\n" +
            "Level design: procedural maze generator\n\n" +
            "Character & animations: Remy (Mixamo)\n" +
            "Textures: ambientCG.com (CC0)\n" +
            "Made with Unity 6 - Computer Graphics and Multimedia, UdL",
            40, new Vector2(0, 60), new Vector2(1400, 520));
        body.alignment = TextAnchor.UpperCenter;
        Button back = CreateButton(cr.transform, "BackButton", "Back", new Vector2(0, -330));
        UnityEventTools.AddPersistentListener(back.onClick, menu.ShowMain);

        menu.mainPanel = main;
        menu.creditsPanel = cr;
        cr.SetActive(false);

        EnsureEventSystem();
        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    // ---------------------------------------------------------------- UI helpers

    static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    static GameObject CreateCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return go;
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = color.a > 0f;
        return go;
    }

    static Text CreateText(Transform parent, string name, string content, int size, Vector2 pos, Vector2 boxSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos; rt.sizeDelta = boxSize;
        var t = go.GetComponent<Text>();
        t.font = UiFont; t.text = content; t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(460, 100);
        go.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.28f, 1f);

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.55f, 0.8f, 1f);
        colors.pressedColor = new Color(0.35f, 0.55f, 0.85f);
        button.colors = colors;

        Text t = CreateText(go.transform, "Label", label, 48, Vector2.zero, new Vector2(460, 100));
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = Vector2.zero; t.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    // Event system for the new Input System, using the project's InputSystem_Actions asset
    static void EnsureEventSystem()
    {
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        var legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null) Object.DestroyImmediate(legacy);
        var module = es.GetComponent<InputSystemUIInputModule>() ?? es.gameObject.AddComponent<InputSystemUIInputModule>();

        const string assetPath = "Assets/InputSystem_Actions.inputactions";
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
        if (asset == null) return;
        var refs = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<InputActionReference>().ToList();
        InputActionReference R(string n) => refs.FirstOrDefault(r => r.name == "UI/" + n);

        module.actionsAsset = asset;
        module.point = R("Point");
        module.leftClick = R("Click");
        module.rightClick = R("RightClick");
        module.middleClick = R("MiddleClick");
        module.scrollWheel = R("ScrollWheel");
        module.move = R("Navigate");
        module.submit = R("Submit");
        module.cancel = R("Cancel");
        module.trackedDevicePosition = R("TrackedDevicePosition");
        module.trackedDeviceOrientation = R("TrackedDeviceOrientation");
        EditorUtility.SetDirty(module);
    }
}
