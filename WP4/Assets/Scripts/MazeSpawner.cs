using System.Collections.Generic;
using UnityEngine;

// Places the player, the food, the destructible walls and the start/goal pads once the maze is built.
[RequireComponent(typeof(Maze3DBuilder))]
public class MazeSpawner : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject foodPrefab;
    public GameObject destructibleWallPrefab;
    public Material goalMaterial;   // Shader Graph material (pulsing goal); optional
    public int foodCount = 15;
    public int destructibleCount = 12;
    public int enemyCount = 4;
    public FollowCamera followCamera;

    void Awake()
    {
        GetComponent<Maze3DBuilder>().OnBuilt += Spawn;
    }

    void Spawn(Maze3DBuilder maze)
    {
        int w = maze.grid.GetLength(0), h = maze.grid.GetLength(1);
        Vector2Int start = new Vector2Int(1, 1);

        // Collect free corridor cells; straight corridor cells are good places to block the path
        var corridorCells = new List<Vector2Int>();
        var straightCells = new List<Vector2Int>();
        for (int x = 1; x < w - 1; x++)
        {
            for (int y = 1; y < h - 1; y++)
            {
                if (maze.grid[x, y] != 0) continue;
                Vector2Int cell = new Vector2Int(x, y);
                if ((cell - start).sqrMagnitude < 9) continue;   // keep the start area free

                bool horizontal = maze.grid[x - 1, y] == 0 && maze.grid[x + 1, y] == 0
                               && maze.grid[x, y - 1] == 1 && maze.grid[x, y + 1] == 1;
                bool vertical = maze.grid[x, y - 1] == 0 && maze.grid[x, y + 1] == 0
                             && maze.grid[x - 1, y] == 1 && maze.grid[x + 1, y] == 1;
                if (horizontal || vertical) straightCells.Add(cell);
                corridorCells.Add(cell);
            }
        }
        Shuffle(straightCells);
        Shuffle(corridorCells);

        // Destructible walls (they block the corridor until the player destroys them with a ball)
        var wallCells = new HashSet<Vector2Int>();
        foreach (Vector2Int cell in straightCells)
        {
            if (wallCells.Count >= destructibleCount) break;
            GameObject go = Instantiate(destructibleWallPrefab, maze.CellToWorld(cell.x, cell.y, 1f), Quaternion.identity, transform);
            go.transform.localScale = new Vector3(maze.cellSize, 2f, maze.cellSize);

            maze.grid[cell.x, cell.y] = 2;   // also block the cell in the grid
            DestructibleWall wall = go.GetComponent<DestructibleWall>();
            wall.maze = maze;
            wall.cell = cell;
            wallCells.Add(cell);
        }

        // Food
        int food = 0;
        foreach (Vector2Int cell in corridorCells)
        {
            if (food >= foodCount) break;
            if (wallCells.Contains(cell)) continue;
            Instantiate(foodPrefab, maze.CellToWorld(cell.x, cell.y, 0.7f), Quaternion.identity, transform);
            food++;
        }

        // Enemies: patrol the corridors, far from the start. A ball kills them.
        int enemies = 0;
        foreach (Vector2Int cell in corridorCells)
        {
            if (enemies >= enemyCount) break;
            if (wallCells.Contains(cell) || (cell - start).sqrMagnitude < 64) continue;
            Enemy.Create(maze, cell, transform);
            enemies++;
        }

        // Start and goal pads (the goal is the reachable cell that is farthest from the start)
        Vector2Int goal = FarthestCell(maze, start, wallCells);
        CreatePad(maze.CellToWorld(start.x, start.y, 0.03f), new Color(0.2f, 0.7f, 1f), false);
        CreatePad(maze.CellToWorld(goal.x, goal.y, 0.03f), new Color(0.1f, 1f, 0.2f), true);

        // Player
        GameObject player = Instantiate(playerPrefab, maze.CellToWorld(start.x, start.y, 0.1f), Quaternion.identity);
        PlayerController controller = player.GetComponent<PlayerController>();
        controller.maze = maze;
        if (followCamera)
        {
            followCamera.target = player.transform;
            controller.cameraTransform = followCamera.transform;
        }

        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm) gm.Setup(food, wallCells.Count);
    }

    // Breadth-first search from the start; returns the farthest cell that has no destructible wall on it.
    static Vector2Int FarthestCell(Maze3DBuilder maze, Vector2Int start, HashSet<Vector2Int> exclude)
    {
        int w = maze.grid.GetLength(0), h = maze.grid.GetLength(1);
        int[,] dist = new int[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                dist[x, y] = -1;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        dist[start.x, start.y] = 0;
        Vector2Int best = start;

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            if (dist[cur.x, cur.y] > dist[best.x, best.y] && !exclude.Contains(cur)) best = cur;

            foreach (Vector2Int d in dirs)
            {
                Vector2Int n = cur + d;
                if (n.x < 0 || n.y < 0 || n.x >= w || n.y >= h) continue;
                if (maze.grid[n.x, n.y] == 1 || dist[n.x, n.y] >= 0) continue;
                dist[n.x, n.y] = dist[cur.x, cur.y] + 1;
                queue.Enqueue(n);
            }
        }
        return best;
    }

    void CreatePad(Vector3 position, Color color, bool isGoal)
    {
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = isGoal ? "Goal" : "Start";
        pad.transform.SetParent(transform);
        pad.transform.position = position;
        pad.transform.localScale = new Vector3(1.6f, 0.03f, 1.6f);

        Material mat;
        if (isGoal && goalMaterial != null)
        {
            mat = goalMaterial;   // Shader Graph: pulsing glow
        }
        else
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.5f);
        }
        pad.GetComponent<Renderer>().sharedMaterial = mat;

        Destroy(pad.GetComponent<Collider>());
        if (!isGoal) return;

        // Goal: trigger zone + a light column so it can be seen from above
        CapsuleCollider trigger = pad.AddComponent<CapsuleCollider>();
        trigger.isTrigger = true;
        trigger.height = 60f;
        trigger.radius = 0.5f;
        pad.AddComponent<ExitGoal>();

        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "GoalBeam";
        beam.transform.SetParent(transform);
        beam.transform.position = position + Vector3.up * 1.5f;
        beam.transform.localScale = new Vector3(0.25f, 1.5f, 0.25f);
        Destroy(beam.GetComponent<Collider>());
        beam.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
