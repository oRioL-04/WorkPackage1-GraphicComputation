using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menu WP3 > Setup Project: creates the physics material, the prefabs (Ball, Food, DestructibleWall) and wires the scene.
public static class WP3Setup
{
    const string P = "Assets/Prefabs/";

    [MenuItem("WP3/Setup Project")]
    public static void Setup()
    {
        // Physics material for the ball (bounciness = 1)
        var pm = new PhysicsMaterial("BouncyBall") { bounciness = 1f, bounceCombine = PhysicsMaterialCombine.Maximum,
            dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum };
        AssetDatabase.DeleteAsset("Assets/Materials/BouncyBall.asset");
        AssetDatabase.CreateAsset(pm, "Assets/Materials/BouncyBall.asset");

        // Ball (small black sphere with Rigidbody)
        var ballGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballGo.name = "Ball";
        ballGo.transform.localScale = Vector3.one * 0.4f;
        ballGo.GetComponent<Renderer>().sharedMaterial = MakeMat("BallMat", Color.black);
        ballGo.GetComponent<SphereCollider>().sharedMaterial = pm;
        var rb = ballGo.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ballGo.AddComponent<Ball>();
        var ballPrefab = Save(ballGo, "Ball");

        // Food (golden sphere with a trigger collider)
        var foodGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foodGo.name = "Food";
        foodGo.transform.localScale = Vector3.one * 0.5f;
        foodGo.GetComponent<Renderer>().sharedMaterial = MakeMat("FoodMat", new Color(1f, 0.8f, 0.1f));
        var sc = foodGo.GetComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 0.8f;
        foodGo.AddComponent<Food>();
        var foodPrefab = Save(foodGo, "Food");

        // Destructible wall
        var wallGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallGo.name = "DestructibleWall";
        wallGo.GetComponent<Renderer>().sharedMaterial = MakeMat("DestructibleWallMat", new Color(0.6f, 0.35f, 0.15f));
        wallGo.AddComponent<DestructibleWall>();
        var wallPrefab = Save(wallGo, "DestructibleWall");

        // Player: if the prefab already exists (with Remy) it is NOT recreated, so the model is not lost
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(P + "Player.prefab");
        if (playerPrefab == null)
        {
        var playerGo = new GameObject("Player");
        var cap = playerGo.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0, 0.9f, 0); cap.height = 1.8f; cap.radius = 0.35f;
        playerGo.AddComponent<Rigidbody>();
        var pc = playerGo.AddComponent<PlayerController>();
        pc.ballPrefab = ballPrefab;
        var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        vis.name = "PlaceholderVisual";
        Object.DestroyImmediate(vis.GetComponent<Collider>());
        vis.transform.SetParent(playerGo.transform);
        vis.transform.localPosition = new Vector3(0, 0.9f, 0);
        vis.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
        playerPrefab = Save(playerGo, "Player");
        }

        // Scene
        var mm = Object.FindAnyObjectByType<Maze3DBuilder>();
        if (mm != null)
        {
            var sp = mm.GetComponent<MazeSpawner>() ?? mm.gameObject.AddComponent<MazeSpawner>();
            sp.playerPrefab = playerPrefab; sp.foodPrefab = foodPrefab; sp.destructibleWallPrefab = wallPrefab;
            if (mm.GetComponent<GameManager>() == null) mm.gameObject.AddComponent<GameManager>();
            var cam = Camera.main;
            if (cam != null)
            {
                var fc = cam.GetComponent<FollowCamera>() ?? cam.gameObject.AddComponent<FollowCamera>();
                sp.followCamera = fc;
            }
            EditorUtility.SetDirty(sp);
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("WP3 setup finished. Save the scene and press Play.");
    }

    static GameObject Save(GameObject go, string name)
    {
        string path = P + name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Material MakeMat(string name, Color c)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(sh);
        m.SetColor("_BaseColor", c);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
