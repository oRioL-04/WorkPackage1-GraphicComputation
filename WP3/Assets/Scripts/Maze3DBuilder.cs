using UnityEngine;

// Generates a random maze and builds it in 3D with wall and floor prefabs.
public class Maze3DBuilder : MonoBehaviour
{
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public float cellSize = 2f;
    public int width = 21;
    public int height = 21;
    [Range(0f, 0.3f)] public float extraOpenings = 0.05f;

    // 0 = corridor, 1 = wall, 2 = destructible wall (set by MazeSpawner)
    public int[,] grid;

    // Raised once the maze is built (used by MazeSpawner)
    public event System.Action<Maze3DBuilder> OnBuilt;

    void Start()
    {
        grid = MazeGenerator.Generate(width, height, extraOpenings);
        Build();
        OnBuilt?.Invoke(this);
    }

    void Build()
    {
        int w = grid.GetLength(0);
        int h = grid.GetLength(1);

        // Floor: one big flat cube under the whole maze
        GameObject floor = Instantiate(floorPrefab, transform);
        floor.transform.position = new Vector3((w - 1) * cellSize / 2f, -0.5f, (h - 1) * cellSize / 2f);
        floor.transform.localScale = new Vector3(w * cellSize, 1f, h * cellSize);

        // Walls: one cube per wall cell
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (grid[x, y] != 1) continue;
                GameObject wall = Instantiate(wallPrefab, CellToWorld(x, y, 1f), Quaternion.identity, transform);
                wall.transform.localScale = new Vector3(cellSize, 2f, cellSize);
            }
        }
    }

    public Vector3 CellToWorld(int x, int y, float height = 0f)
    {
        return new Vector3(x * cellSize, height, y * cellSize);
    }
}
