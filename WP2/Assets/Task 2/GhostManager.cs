using System.Collections.Generic;
using UnityEngine;

// EXTRA (bonus): spawns the ghosts, controls the global "frightened" mode
// (the moment when the ghosts change colour and the PacMan can eat them) and
// handles the contacts between the PacMan and the ghosts.
//
// By default the ghosts only wander around the maze; tick "chasePacMan" if you
// want them to go after the PacMan again.
//
// If a normal ghost catches the PacMan it is GAME OVER: the message is shown
// for a moment and then a brand new random map is generated, which resets the
// food, the food counter and everybody's position.
public class GhostManager : MonoBehaviour
{
    public Maze_Task2 maze;
    public PacManController pacman;

    // Prefab of a single ghost
    public GameObject ghostPrefab;

    // Both ghosts use this same sprite (same colour for all of them).
    // If it is left empty, the sprite stored in the prefab is used.
    public Sprite ghostSprite;

    // Sprites used while a ghost is edible (frightened). The second one must be
    // a WHITE-bodied ghost, because it is tinted with the two colours below.
    public Sprite frightenedSprite;
    public Sprite frightenedBlinkSprite;

    // Colours the edible ghosts flash between (change them here, in the Inspector)
    public Color frightenedColorA = new Color(1f, 0.25f, 0.25f);   // red
    public Color frightenedColorB = new Color(1f, 1f, 1f);         // white
    public float frightenedBlinkInterval = 0.25f;                  // seconds between flashes

    // false -> the ghosts simply wander around the maze
    // true  -> they chase the PacMan
    public bool chasePacMan = false;

    public float chaseDuration = 9f;                // seconds chasing the PacMan
    public float frightenedDuration = 6f;           // seconds being edible
    public float eatenRespawnDelay = 6f;            // seconds before an eaten ghost returns
    public float hitDistance = 0.55f;               // contact distance PacMan <-> ghost
    public float gameOverDuration = 1.8f;           // seconds the GAME OVER message is shown

    GhostController[] ghosts;
    float timer = 0f;
    bool frightened = false;

    float gameOverTimer = 0f;
    bool gameOver = false;

    // BFS distance from every square to the PacMan (used by the chase)
    int[,] distance;
    static readonly Vector2Int[] offsets =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    public bool IsFrightened { get { return frightened; } }
    public bool ChasePacMan { get { return chasePacMan; } }
    public float RespawnDelay { get { return eatenRespawnDelay; } }
    public Color FrightenedColorA { get { return frightenedColorA; } }
    public Color FrightenedColorB { get { return frightenedColorB; } }
    public float FrightenedBlinkInterval { get { return frightenedBlinkInterval; } }

    void Start()
    {
        if (maze == null)
            maze = FindFirstObjectByType<Maze_Task2>();

        if (pacman == null)
            pacman = FindFirstObjectByType<PacManController>();

        if (maze == null)
        {
            Debug.LogWarning("[Extra] GhostManager needs a Maze_Task2 reference.");
            return;
        }

        maze.onMazeGenerated += OnNewMaze;

        SpawnGhosts();

        timer = chaseDuration;
    }

    void OnDestroy()
    {
        if (maze != null)
            maze.onMazeGenerated -= OnNewMaze;
    }

    void SpawnGhosts()
    {
        if (ghostPrefab == null)
        {
            Debug.LogWarning("[Extra] GhostManager has no ghost prefab assigned.");
            return;
        }

        ghosts = new GhostController[2];

        // Two starting squares, as far from the entrance as possible
        int[,] homes = new int[2, 2];
        homes[0, 0] = 1;
        homes[0, 1] = maze.Cols - 2;
        homes[1, 0] = maze.Rows - 2;
        homes[1, 1] = 1;

        for (int i = 0; i < ghosts.Length; i++)
        {
            GameObject obj = Instantiate(ghostPrefab);
            obj.transform.parent = this.gameObject.transform;

            GhostController ghost = obj.GetComponent<GhostController>();

            // Same colour for both ghosts
            if (ghostSprite != null)
                ghost.normalSprite = ghostSprite;

            ghost.frightenedSprite = frightenedSprite;
            ghost.frightenedBlinkSprite = frightenedBlinkSprite;

            ghost.Init(this, maze, homes[i, 0], homes[i, 1]);
            ghosts[i] = ghost;
        }
    }

    void Update()
    {
        if (maze == null || !maze.IsReady)
            return;

        if (ghosts == null)
            return;

        // GAME OVER: show the message for a moment, then start over
        if (gameOver)
        {
            gameOverTimer -= Time.deltaTime;

            if (gameOverTimer <= 0f)
            {
                gameOver = false;

                if (pacman != null)
                    pacman.enabled = true;

                // New random map: food, food counter and positions all reset
                maze.RegenerateMaze();
            }

            return;
        }

        // The distance field is only needed when the ghosts chase the PacMan
        if (chasePacMan)
            ComputeDistanceField();

        UpdateModeTimer();
        HandleContacts();
    }

    // ---- Frightened mode ------------------------------------------------
    void UpdateModeTimer()
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        // Chase <-> frightened
        frightened = !frightened;

        for (int i = 0; i < ghosts.Length; i++)
        {
            if (ghosts[i] != null)
                ghosts[i].SetFrightened(frightened);
        }

        timer = frightened ? frightenedDuration : chaseDuration;
    }

    void OnNewMaze()
    {
        // The ghosts reset themselves through their own onMazeGenerated
        // subscription, we only restart the timer in "chase" mode.
        frightened = false;
        timer = chaseDuration;
    }

    // ---- Contacts -------------------------------------------------------
    void HandleContacts()
    {
        if (pacman == null)
            return;

        for (int i = 0; i < ghosts.Length; i++)
        {
            GhostController ghost = ghosts[i];

            if (ghost == null || ghost.IsEaten)
                continue;

            float d = Vector2.Distance(pacman.transform.position, ghost.transform.position);

            if (d > hitDistance)
                continue;

            if (frightened)
            {
                // The PacMan eats the ghost
                ghost.GetEaten();
            }
            else
            {
                // A normal ghost catches the PacMan -> GAME OVER
                StartGameOver();
                return;
            }
        }
    }

    void StartGameOver()
    {
        gameOver = true;
        gameOverTimer = gameOverDuration;

        // Freeze the PacMan and hide the ghosts while the message is shown
        if (pacman != null)
            pacman.enabled = false;

        for (int i = 0; i < ghosts.Length; i++)
        {
            if (ghosts[i] != null)
                ghosts[i].gameObject.SetActive(false);
        }
    }

    // "GAME OVER" text in the middle of the screen
    void OnGUI()
    {
        if (!gameOver)
            return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 52;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(1f, 0.25f, 0.25f);

        Rect rect = new Rect(0f, Screen.height * 0.38f, Screen.width, 80f);

        GUIStyle shadow = new GUIStyle(style);
        shadow.normal.textColor = new Color(0f, 0f, 0f, 0.85f);

        GUI.Label(new Rect(rect.x + 3f, rect.y + 3f, rect.width, rect.height), "GAME OVER", shadow);
        GUI.Label(rect, "GAME OVER", style);
    }

    // ---- Chase helper ---------------------------------------------------
    // Breadth first search from the PacMan, so a ghost just has to walk to
    // the neighbour with the smallest distance.
    void ComputeDistanceField()
    {
        int rows = maze.Rows;
        int cols = maze.Cols;

        if (distance == null ||
            distance.GetLength(0) != rows ||
            distance.GetLength(1) != cols)
        {
            distance = new int[rows, cols];
        }

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                distance[i, j] = -1;
            }
        }

        if (pacman == null)
            return;

        int startRow = pacman.Row;
        int startCol = pacman.Col;

        if (!maze.IsCorridor(startRow, startCol))
            return;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        distance[startRow, startCol] = 0;
        queue.Enqueue(new Vector2Int(startRow, startCol));

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            for (int k = 0; k < offsets.Length; k++)
            {
                int nr = current.x + offsets[k].x;
                int nc = current.y + offsets[k].y;

                if (!maze.IsCorridor(nr, nc))
                    continue;

                if (distance[nr, nc] != -1)
                    continue;

                distance[nr, nc] = distance[current.x, current.y] + 1;
                queue.Enqueue(new Vector2Int(nr, nc));
            }
        }
    }

    public int DistanceToPacMan(int r, int c)
    {
        if (distance == null)
            return int.MaxValue;

        if (r < 0 || r >= distance.GetLength(0) ||
            c < 0 || c >= distance.GetLength(1))
            return int.MaxValue;

        int value = distance[r, c];
        return value < 0 ? int.MaxValue : value;
    }
}
