using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Maze_Task2 : MonoBehaviour
{
    public GameObject corridorSquare;

    // Tilemap used to draw the walls
    public Tilemap wallTilemap;

    // Rule Tile used for the walls
    public TileBase wallRuleTile;

    // ROWS and COLS must be ODD numbers
    int ROWS = 11;
    int COLS = 21;

    int[,] maze;
    bool[,] visited;
    ArrayList _squares;

    public Color entranceColor = Color.green;
    public Color exitColor = Color.red;

    // EXTRA: counts how many mazes have been generated
    int mazeCount = 0;

    void Start()
    {
        SetMap();
    }

    void SetMap()
    {
        // EXTRA: increase the maze counter
        mazeCount++;

        // EXTRA: show the number of generated mazes in the Console
        Debug.Log("Maze generated: " + mazeCount);

        InitMaze();
        BuildRandomMaze();
        DrawMaze();
    }

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
        // Destroy corridor GameObjects
        for (int i = 0; i < _squares.Count; i++)
            Destroy((GameObject)_squares[i]);

        _squares.Clear();

        // Remove all wall tiles
        wallTilemap.ClearAllTiles();
    }

    void InitMaze()
    {
        maze = new int[ROWS, COLS];
        visited = new bool[ROWS, COLS];

        // Everything starts as a wall (1)
        for (int i = 0; i < ROWS; i++)
        {
            for (int j = 0; j < COLS; j++)
            {
                maze[i, j] = 1;
            }
        }
    }

    void BuildRandomMaze()
    {
        int startI = 1 + 2 * Random.Range(0, (ROWS - 1) / 2);
        int startJ = 1 + 2 * Random.Range(0, (COLS - 1) / 2);

        Carve(startI, startJ);

        AddLoops();
    }

    void Carve(int i, int j)
    {
        maze[i, j] = 0;
        visited[i, j] = true;

        int[] order = { 0, 1, 2, 3 };
        Shuffle(order);

        for (int k = 0; k < 4; k++)
        {
            int ni = i;
            int nj = j;

            switch (order[k])
            {
                case 0:
                    ni = i - 2;
                    break;

                case 1:
                    ni = i + 2;
                    break;

                case 2:
                    nj = j - 2;
                    break;

                case 3:
                    nj = j + 2;
                    break;
            }

            if (ni <= 0 || ni >= ROWS - 1 ||
                nj <= 0 || nj >= COLS - 1)
            {
                continue;
            }

            if (visited[ni, nj])
            {
                continue;
            }

            // Remove the wall between the two rooms
            maze[(i + ni) / 2, (j + nj) / 2] = 0;

            Carve(ni, nj);
        }
    }

    void AddLoops()
    {
        float loopChance = 0.1f;

        for (int i = 1; i < ROWS - 1; i++)
        {
            for (int j = 1; j < COLS - 1; j++)
            {
                bool isHorizontalWall =
                    (i % 2 == 1) && (j % 2 == 0);

                bool isVerticalWall =
                    (i % 2 == 0) && (j % 2 == 1);

                if ((isHorizontalWall || isVerticalWall) &&
                    maze[i, j] == 1)
                {
                    if (Random.value < loopChance)
                    {
                        maze[i, j] = 0;
                    }
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
        _squares = new ArrayList();

        for (int i = 0; i < ROWS; i++)
        {
            for (int j = 0; j < COLS; j++)
            {
                // CORRIDOR
                if (maze[i, j] == 0)
                {
                    GameObject obj =
                        Instantiate(corridorSquare) as GameObject;

                    // Entrance
                    if (i == 1 && j == 1)
                    {
                        obj.GetComponent<SpriteRenderer>()
                            .color = entranceColor;
                    }

                    // Exit
                    else if (i == ROWS - 2 &&
                             j == COLS - 2)
                    {
                        obj.GetComponent<SpriteRenderer>()
                            .color = exitColor;
                    }

                    obj.transform.position =
                        new Vector3(
                            (float)j - COLS / 2,
                            (float)i - ROWS / 2,
                            0.0f
                        );

                    obj.transform.parent =
                        this.gameObject.transform;

                    _squares.Add(obj);
                }

                // WALL
                else
                {
                    Vector3Int tilePosition =
                        new Vector3Int(
                            j - COLS / 2,
                            i - ROWS / 2,
                            0
                        );

                    // Draw wall using the Rule Tile
                    wallTilemap.SetTile(
                        tilePosition,
                        wallRuleTile
                    );
                }
            }
        }

        // Refresh Rule Tile neighbours
        wallTilemap.RefreshAllTiles();
    }
}