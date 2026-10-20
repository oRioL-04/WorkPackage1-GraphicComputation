using System.Collections;
using System.Collections.Generic;
using UnityEngine;
  
 
public class Maze : MonoBehaviour
{
    public GameObject corridorSquare;
    public GameObject wallSquare;
 
    // ROWS and COLS must be ODD numbers (needed for the room/wall grid below).
    // To change the maze size, just change these two values.
    int ROWS = 11;
    int COLS = 21;
 
    int[,] maze;
    bool[,] visited;
    ArrayList _squares;

    public Color entranceColor = Color.green;
    public Color exitColor = Color.red;
 
    // Start is called before the first frame update
    void Start()
    {
        SetMap();
    }
 
 
    void SetMap()
    {
        InitMaze();
        BuildRandomMaze();
        DrawMaze();
    }
 
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DestroyMaze();
            SetMap();
        }
 
    }
 
    void DestroyMaze()
    {
        for (int i = 0; i < _squares.Count; i++)
            Destroy((GameObject)_squares[i]);
        _squares.Clear();
    }
 
    void InitMaze()
    {
        maze = new int[ROWS, COLS];
        visited = new bool[ROWS, COLS];
 
        // Everything starts as a wall (1).
        for (int i = 0; i < ROWS; i++)
            for (int j = 0; j < COLS; j++)
                maze[i, j] = 1;
    }
 
    void BuildRandomMaze()
    {
        // "Rooms" live on odd,odd cells (1,1), (1,3), (3,1), ...
        // The cells between two rooms are the walls that we can knock down.
        // Carving a random path between rooms (like below) always produces
        // a maze where every corridor cell is reachable from every other
        // one (a spanning tree), so the maze is guaranteed connected.
        int startI = 1 + 2 * Random.Range(0, (ROWS - 1) / 2);
        int startJ = 1 + 2 * Random.Range(0, (COLS - 1) / 2);
 
        Carve(startI, startJ);
 
        AddLoops();
    }
 
    void Carve(int i, int j)
    {
        maze[i, j] = 0;
        visited[i, j] = true;
 
        // Visit the 4 neighbouring rooms (2 cells away) in random order.
        int[] order = { 0, 1, 2, 3 };
        Shuffle(order);
 
        for (int k = 0; k < 4; k++)
        {
            int ni = i, nj = j;
            switch (order[k])
            {
                case 0: ni = i - 2; break; // up
                case 1: ni = i + 2; break; // down
                case 2: nj = j - 2; break; // left
                case 3: nj = j + 2; break; // right
            }
 
            if (ni <= 0 || ni >= ROWS - 1 || nj <= 0 || nj >= COLS - 1)
                continue;
 
            if (visited[ni, nj])
                continue;
 
            // Knock down the wall between (i,j) and (ni,nj).
            maze[(i + ni) / 2, (j + nj) / 2] = 0;
 
            Carve(ni, nj);
        }
    }
 
    void AddLoops()
    {
        // Go through every wall that sits between two rooms and, with a
        // small chance, knock it down too. The maze is already fully
        // connected at this point, so this only ever ADDS extra paths
        // (loops) between existing corridor cells - it can never
        // disconnect anything.
        float loopChance = 0.1f;
 
        for (int i = 1; i < ROWS - 1; i++)
        {
            for (int j = 1; j < COLS - 1; j++)
            {
                bool isHorizontalWall = (i % 2 == 1) && (j % 2 == 0);
                bool isVerticalWall = (i % 2 == 0) && (j % 2 == 1);
 
                if ((isHorizontalWall || isVerticalWall) && maze[i, j] == 1)
                {
                    if (Random.value < loopChance)
                        maze[i, j] = 0;
                }
            }
        }
    }
 
    void Shuffle(int[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }
    }
 
 
    void DrawMaze()
    {
        GameObject obj;
        _squares = new ArrayList();
 
        for (int i = 0; i < ROWS; i++)
            for (int j = 0; j < COLS; j++)
            {
                if (maze[i, j] == 0)
                    obj = Instantiate(corridorSquare) as GameObject;
                else
                    obj = Instantiate(wallSquare) as GameObject;

                if (maze[i, j] == 0 && i == 1 && j == 1)
                    obj.GetComponent<SpriteRenderer>().color = entranceColor;
                else if (maze[i, j] == 0 && i == ROWS - 2 && j == COLS - 2)
                    obj.GetComponent<SpriteRenderer>().color = exitColor;
 
                obj.transform.position = new Vector3((float)j - COLS / 2, (float)i - ROWS / 2, 0.0f);
                obj.transform.parent = this.gameObject.transform;
                _squares.Add(obj);
            }
    }
}
 