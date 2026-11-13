using UnityEngine;

// Enemy that patrols the maze cell by cell. A ball kills it; touching the player sends the player back to the start.
[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    public float speed = 2.2f;
    public float catchDistance = 1.1f;   // player radius + enemy radius

    [HideInInspector] public Maze3DBuilder maze;
    Vector2Int current, target;
    Vector2Int previous = new Vector2Int(-1, -1);
    Rigidbody rb;
    PlayerController player;
    bool dead;

    // Creates an enemy (red glowing sphere) from code, so no extra prefab is needed.
    public static Enemy Create(Maze3DBuilder maze, Vector2Int cell, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Enemy";
        go.transform.SetParent(parent);
        go.transform.localScale = Vector3.one * 1.6f;   // big: easy to hit with a ball
        go.transform.position = maze.CellToWorld(cell.x, cell.y, 0.8f);

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.85f, 0.05f, 0.05f));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.05f) * 1.2f);
        go.GetComponent<Renderer>().sharedMaterial = mat;

        Rigidbody body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;   // moved by code, but still reports collisions with the balls
        Enemy enemy = go.AddComponent<Enemy>();
        enemy.maze = maze;
        enemy.current = enemy.target = cell;
        return enemy;
    }

    void Awake() { rb = GetComponent<Rigidbody>(); }

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        if (GameManager.Instance != null) GameManager.Instance.EnemyAdded();
    }

    void FixedUpdate()
    {
        if (dead || maze == null) return;

        Vector3 goal = maze.CellToWorld(target.x, target.y, 0.8f);
        Vector3 pos = Vector3.MoveTowards(rb.position, goal, speed * Time.fixedDeltaTime);
        pos.y = 0.8f + Mathf.Sin(Time.time * 6f) * 0.08f;   // little bounce
        rb.MovePosition(pos);

        Vector3 flat = new Vector3(pos.x - goal.x, 0f, pos.z - goal.z);
        if (flat.sqrMagnitude < 0.0004f) ChooseNextCell();
    }

    void Update()
    {
        if (dead || (GameManager.Instance != null && GameManager.Instance.PlayerDead)) return;
        if (player == null) { player = FindAnyObjectByType<PlayerController>(); return; }

        Vector3 d = player.transform.position - transform.position;
        d.y = 0f;
        if (d.magnitude < catchDistance)
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerKilled();   // game over
        }
    }

    // Random walk: keep going to a free neighbour cell, avoiding turning back unless it is a dead end.
    void ChooseNextCell()
    {
        current = target;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        var options = new System.Collections.Generic.List<Vector2Int>();
        foreach (Vector2Int d in dirs)
        {
            Vector2Int n = current + d;
            if (n.x < 0 || n.y < 0 || n.x >= maze.grid.GetLength(0) || n.y >= maze.grid.GetLength(1)) continue;
            if (maze.grid[n.x, n.y] != 0 || n == previous) continue;
            options.Add(n);
        }
        if (options.Count == 0)
        {
            Vector2Int back = previous;   // dead end (or walled in): turn around if possible
            if (back.x >= 0 && maze.grid[back.x, back.y] == 0) options.Add(back);
            else return;                  // stay and try again next physics step
        }
        previous = current;
        target = options[Random.Range(0, options.Count)];
    }

    public void Die()
    {
        if (dead) return;
        dead = true;
        Effects.Burst(transform.position, new Color(1f, 0.15f, 0.1f), 50, 6f, 0.3f);
        if (GameManager.Instance != null) GameManager.Instance.EnemyKilled();
        Destroy(gameObject);
    }
}
