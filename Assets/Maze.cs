using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Maze : MonoBehaviour
{
    public GameObject corridorSquare;
    public GameObject wallSquare;

    int ROWS = 11;
    int COLS = 21;

    int[,] maze;
    ArrayList _squares;

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
        if(Input.GetKeyDown(KeyCode.Space))
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

        for (int i = 1; i < (ROWS-1); i++)
            for (int j = 1; j < (COLS-1); j++)
                maze[i, j] = 0;
    
        for (int i = 0; i < ROWS; i++)
        {
            maze[i, 0] = 1;
            maze[i, COLS - 1] = 1;
        }

        for (int j = 1; j < (COLS - 1); j++)
        {
            maze[0, j] = 1;
            maze[ROWS - 1, j] = 1;
        }
    }

    void BuildRandomMaze()
    {
        int i = Random.Range(1, ROWS - 1);
        int j = Random.Range(1, COLS - 1);

        maze[i, j] = 1;
    }


    void DrawMaze()
    {
        GameObject obj;
        _squares = new ArrayList();

        for (int i = 0; i < ROWS; i++)
            for (int j = 0; j < COLS; j++)
            {
                if (maze[i,j]==0)
                  obj = Instantiate(corridorSquare) as GameObject;
                else 
                  obj = Instantiate(wallSquare) as GameObject;
        
                obj.transform.position = new Vector3((float)j - COLS / 2, (float)i - ROWS / 2, 0.0f);
                obj.transform.parent = this.gameObject.transform;
                _squares.Add(obj);
            }
    }
}
