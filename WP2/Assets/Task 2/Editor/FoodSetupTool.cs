using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Task 2 helper (editor only).
//
// It generates the food assets that are hard to create by hand:
//   * Food.png        -> a glowing white pellet
//   * FoodPulse.anim  -> looping AnimationClip that pulses the scale (idle)
//   * FoodEat.anim    -> one-shot AnimationClip that pops and shrinks (eaten)
//   * Food.prefab     -> SpriteRenderer + Animation (legacy) with both clips
// and then wires the FoodManager into the currently open scene.
//
// Run it whenever you want to (re)generate the assets:
//   Tools > WP2 > Setup Food Elements (Task 2)
public static class FoodSetupTool
{
    const string folder = "Assets/Task 2";
    const string spritePath = folder + "/Food.png";
    const string pulseClipPath = folder + "/FoodPulse.anim";
    const string eatClipPath = folder + "/FoodEat.anim";
    const string prefabPath = folder + "/Food.prefab";

    [MenuItem("Tools/WP2/Setup Food Elements (Task 2)")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Task 2] Stop the game before setting up the food assets.");
            return;
        }

        CreateFoodSprite();
        AssetDatabase.Refresh();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        if (sprite == null)
        {
            Debug.LogError("[Task 2] Could not create/load " + spritePath);
            return;
        }

        AnimationClip pulse = CreatePulseAnimation();
        AnimationClip eat = CreateEatAnimation();
        GameObject prefab = CreateFoodPrefab(sprite, pulse, eat);

        WireScene(prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Task 2] Food elements ready. Save the scene (Cmd/Ctrl+S) and press Play.");
    }

    // ---- Sprite ---------------------------------------------------------
    // A bright white core with a soft glow around it.
    static void CreateFoodSprite()
    {
        const int size = 56;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color[] pixels = new Color[size * size];
        Vector2 centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        float coreRadius = 16f;
        float glowRadius = 26f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), centre);
                float alpha;

                if (distance <= coreRadius)
                {
                    // Solid core (soft 1 pixel border)
                    alpha = Mathf.Clamp01(coreRadius - distance + 0.5f);
                }
                else
                {
                    // Soft glow fading out
                    float t = Mathf.Clamp01((distance - coreRadius) / (glowRadius - coreRadius));
                    alpha = Mathf.Pow(1f - t, 2f) * 0.55f;
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(spritePath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(spritePath);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    // ---- Idle animation: pulse the scale --------------------------------
    static AnimationClip CreatePulseAnimation()
    {
        AnimationClip clip = GetOrCreateClip(pulseClipPath, "FoodPulse");

        clip.frameRate = 30f;
        clip.wrapMode = WrapMode.Loop;
        clip.legacy = true;

        AnimationCurve pulse = new AnimationCurve(
            new Keyframe(0f, 0.65f),
            new Keyframe(0.6f, 1f),
            new Keyframe(1.2f, 0.65f));

        AnimationCurve constant = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1.2f, 1f));

        SetScaleCurves(clip, pulse, constant);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    // ---- Eaten animation: pop, then shrink to nothing -------------------
    static AnimationClip CreateEatAnimation()
    {
        AnimationClip clip = GetOrCreateClip(eatClipPath, "FoodEat");

        clip.frameRate = 30f;
        clip.wrapMode = WrapMode.Once;
        clip.legacy = true;

        AnimationCurve pop = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.08f, 1.65f),
            new Keyframe(0.28f, 0f));

        AnimationCurve constant = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.28f, 1f));

        SetScaleCurves(clip, pop, constant);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    static AnimationClip GetOrCreateClip(string path, string name)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.name = name;
        return clip;
    }

    static void SetScaleCurves(AnimationClip clip, AnimationCurve xy, AnimationCurve z)
    {
        AnimationUtility.SetEditorCurve(clip,
            EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale.x"), xy);

        AnimationUtility.SetEditorCurve(clip,
            EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale.y"), xy);

        AnimationUtility.SetEditorCurve(clip,
            EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale.z"), z);
    }

    // ---- Prefab ---------------------------------------------------------
    static GameObject CreateFoodPrefab(Sprite sprite, AnimationClip pulse, AnimationClip eat)
    {
        GameObject go = new GameObject("Food");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 0;

        Animation anim = go.AddComponent<Animation>();
        anim.AddClip(pulse, pulse.name);
        anim.AddClip(eat, eat.name);
        anim.clip = pulse;
        anim.playAutomatically = true;
        anim.wrapMode = WrapMode.Loop;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);

        return prefab;
    }

    // ---- Scene wiring ---------------------------------------------------
    static void WireScene(GameObject prefab)
    {
        Maze_Task2 maze = UnityEngine.Object.FindFirstObjectByType<Maze_Task2>();
        PacManController pacman = UnityEngine.Object.FindFirstObjectByType<PacManController>();

        if (maze == null || pacman == null)
        {
            Debug.LogWarning("[Task 2] Could not find Maze_Task2 / PacManController in the open scene. " +
                             "Add an empty GameObject with the FoodManager script and assign the references by hand.");
            return;
        }

        FoodManager manager = UnityEngine.Object.FindFirstObjectByType<FoodManager>();

        if (manager == null)
        {
            GameObject go = new GameObject("FoodManager");
            manager = go.AddComponent<FoodManager>();
        }

        manager.maze = maze;
        manager.foodPrefab = prefab;
        pacman.food = manager;

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(pacman);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }
}
