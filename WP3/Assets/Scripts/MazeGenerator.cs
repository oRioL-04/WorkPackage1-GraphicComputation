using System.Collections.Generic;
using UnityEngine;

public static class MazeGenerator
{
    // Returns a grid where 1 = wall and 0 = corridor.
    // width and height are forced to be odd numbers.
    // extraOpenings (0..1): probability of opening extra walls to create loops.
    public static int[,] Generate(int width, int height, float extraOpenings = 0.05f)
    {
        if (width % 2 == 0) width++;
        if (height % 2 == 0) height++;

        int[,] grid = new int[width, height];

        // Everything starts as a wall
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = 1;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // Iterative depth-first search (recursive backtracker)
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int start = new Vector2Int(1, 1);
        grid[start.x, start.y] = 0;
        stack.Push(start);

        while (stack.Count > 0)
        {
            Vector2Int cur = stack.Peek();

            // Neighbours two cells away that are still walls
            List<Vector2Int> options = new List<Vector2Int>();
            foreach (Vector2Int d in dirs)
            {
                Vector2Int n = cur + d * 2;
                if (n.x > 0 && n.x < width - 1 && n.y > 0 && n.y < height - 1 && grid[n.x, n.y] == 1)
                    options.Add(d);
            }

            if (options.Count == 0)
            {
                stack.Pop();   // dead end: backtrack
                continue;
            }

            Vector2Int chosen = options[Random.Range(0, options.Count)];
            grid[cur.x + chosen.x, cur.y + chosen.y] = 0;          // open the wall in between
            grid[cur.x + chosen.x * 2, cur.y + chosen.y * 2] = 0;  // open the new cell
            stack.Push(cur + chosen * 2);
        }

        // Extra openings so there are several paths (a less "perfect" maze)
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (grid[x, y] != 1) continue;
                bool horizontal = grid[x - 1, y] == 0 && grid[x + 1, y] == 0;
                bool vertical = grid[x, y - 1] == 0 && grid[x, y + 1] == 0;
                if ((horizontal || vertical) && Random.value < extraOpenings)
                    grid[x, y] = 0;
            }
        }

        return grid;
    }
}
