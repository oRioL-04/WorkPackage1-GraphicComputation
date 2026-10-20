using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// EXTRA (bonus) helper (editor only).
//
// Generates the ghost sprites (two normal colours, the red "edible" one and
// the one used while blinking), the ghost prefab, and wires the GhostManager
// into the currently open scene.
//
// Run it from:  Tools > WP2 > Setup Ghosts (Extra)
public static class GhostSetupTool
{
    const string folder = "Assets/Task 2";
    const string cyanPath = folder + "/GhostCyan.png";
    const string orangePath = folder + "/GhostOrange.png";
    const string redPath = folder + "/GhostRed.png";
    const string blinkPath = folder + "/GhostBlink.png";
    const string oldBluePath = folder + "/GhostBlue.png";
    const string prefabPath = folder + "/Ghost.prefab";

    static readonly Color eyeWhite = new Color(1f, 1f, 1f);
    static readonly Color bodyRed = new Color(1f, 0.25f, 0.25f);

    [MenuItem("Tools/WP2/Setup Ghosts (Extra)")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Extra] Stop the game before setting up the ghost assets.");
            return;
        }

        CreateGhostSprite(cyanPath, new Color(0.35f, 0.85f, 1f), eyeWhite);
        CreateGhostSprite(orangePath, new Color(1f, 0.6f, 0.15f), eyeWhite);
        CreateGhostSprite(redPath, bodyRed, eyeWhite);
        CreateGhostSprite(blinkPath, eyeWhite, bodyRed);

        AssetDatabase.Refresh();

        Sprite cyan = AssetDatabase.LoadAssetAtPath<Sprite>(cyanPath);
        Sprite orange = AssetDatabase.LoadAssetAtPath<Sprite>(orangePath);
        Sprite frightened = AssetDatabase.LoadAssetAtPath<Sprite>(redPath);
        Sprite blink = AssetDatabase.LoadAssetAtPath<Sprite>(blinkPath);

        if (cyan == null || orange == null || frightened == null || blink == null)
        {
            Debug.LogError("[Extra] Could not create/load the ghost sprites.");
            return;
        }

        GameObject prefab = CreateGhostPrefab(cyan, frightened, blink);
        WireScene(prefab, cyan, orange, frightened, blink);

        // The old blue sprite is not used any more
        AssetDatabase.DeleteAsset(oldBluePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Extra] Ghosts ready. Save the scene (Cmd/Ctrl+S) and press Play.");
    }

    // ---- Sprites --------------------------------------------------------
    static void CreateGhostSprite(string path, Color body, Color eye)
    {
        const int size = 64;

        float centre = (size - 1) * 0.5f;
        float radius = 28f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        Vector2 eyeLeft = new Vector2(centre - 11f, centre + 7f);
        Vector2 eyeRight = new Vector2(centre + 11f, centre + 7f);
        float eyeRadius = 9f;
        float pupilRadius = 4.5f;
        Color pupil = new Color(0.15f, 0.15f, 0.4f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float coverage = GhostCoverage(x, y, centre, radius);
                Color color = new Color(body.r, body.g, body.b, coverage);

                float dx = x + 0.5f;
                float dy = y + 0.5f;

                float eyeDistance = Mathf.Min(
                    Vector2.Distance(new Vector2(dx, dy), eyeLeft),
                    Vector2.Distance(new Vector2(dx, dy), eyeRight));

                // White (or coloured) of the eye
                if (eyeDistance < eyeRadius)
                {
                    float a = Mathf.Clamp01(eyeRadius - eyeDistance + 0.5f);
                    color = new Color(eye.r, eye.g, eye.b, Mathf.Max(color.a, a));
                }

                // Pupil
                if (eyeDistance < pupilRadius)
                    color = pupil;

                pixels[y * size + x] = color;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64f;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    // Fraction of the pixel that belongs to the ghost body (3x3 supersampling)
    static float GhostCoverage(int px, int py, float centre, float radius)
    {
        const int samples = 3;
        int hits = 0;

        for (int sy = 0; sy < samples; sy++)
        {
            for (int sx = 0; sx < samples; sx++)
            {
                float x = px + (sx + 0.5f) / samples;
                float y = py + (sy + 0.5f) / samples;

                if (InsideGhost(x, y, centre, radius))
                    hits++;
            }
        }

        return (float)hits / (samples * samples);
    }

    static bool InsideGhost(float x, float y, float centre, float radius)
    {
        float u = (x - centre) / radius;
        float v = (y - centre) / radius;

        if (u < -1f || u > 1f)
            return false;

        // Dome on top
        float top = Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));

        if (v > top)
            return false;

        // Wavy bottom (feet and notches)
        float wave = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * 3f * (u + 1f));
        float bottom = -1f + 0.30f * wave;

        return v >= bottom;
    }

    // ---- Prefab ---------------------------------------------------------
    static GameObject CreateGhostPrefab(Sprite normal, Sprite frightened, Sprite blink)
    {
        GameObject go = new GameObject("Ghost");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = normal;
        sr.sortingOrder = 1;

        GhostController ghost = go.AddComponent<GhostController>();
        ghost.normalSprite = normal;
        ghost.frightenedSprite = frightened;
        ghost.frightenedBlinkSprite = blink;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);

        return prefab;
    }

    // ---- Scene wiring ---------------------------------------------------
    static void WireScene(GameObject prefab, Sprite cyan, Sprite orange, Sprite frightened, Sprite blink)
    {
        Maze_Task2 maze = UnityEngine.Object.FindFirstObjectByType<Maze_Task2>();
        PacManController pacman = UnityEngine.Object.FindFirstObjectByType<PacManController>();

        if (maze == null || pacman == null)
        {
            Debug.LogWarning("[Extra] Could not find Maze_Task2 / PacManController in the open scene. " +
                             "Add an empty GameObject with the GhostManager script and assign the references by hand.");
            return;
        }

        GhostManager manager = UnityEngine.Object.FindFirstObjectByType<GhostManager>();

        if (manager == null)
        {
            GameObject go = new GameObject("GhostManager");
            manager = go.AddComponent<GhostManager>();
        }

        manager.maze = maze;
        manager.pacman = pacman;
        manager.ghostPrefab = prefab;

        // Both ghosts share the same colour (GhostOrange is left on disk as an
        // alternative: just drag it into this slot if you prefer orange)
        manager.ghostSprite = cyan;
        manager.frightenedSprite = frightened;
        manager.frightenedBlinkSprite = blink;

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }
}
