using System.Collections.Generic;
using UnityEngine;

// EXTRA (bonus): a ghost that walks through the maze using the same tile
// information as the PacMan (no colliders).
//
// It has two behaviours:
//   * normal -> wanders around the maze (it only chases the PacMan when the
//     "chasePacMan" option of the GhostManager is ticked)
//   * frightened -> is drawn with the "frightened" sprite, tinted and flashing
//     between two colours, and runs around at random, so the PacMan can eat it
public class GhostController : MonoBehaviour
{
    // Sprites: the normal one and the frightened one (must be a WHITE-bodied
    // sprite, because it gets tinted with the two flashing colours below).
    public Sprite normalSprite;
    public Sprite frightenedSprite;
    public Sprite frightenedBlinkSprite;

    public float speed = 3.4f;

    // Small negative Z so the ghosts are drawn in front of the PacMan
    const float zPos = -0.05f;

    GhostManager manager;
    Maze_Task2 maze;
    SpriteRenderer sr;

    int row, col;               // square the ghost is standing on
    int homeRow, homeCol;       // square it starts / respawns in

    int targetRow, targetCol;
    Vector3 targetPos;
    bool moving = false;
    Dir dir = Dir.None;

    bool frightened = false;
    bool eaten = false;
    float respawnTimer = 0f;

    bool blinkOn = false;
    float blinkTimer = 0f;

    public bool IsEaten { get { return eaten; } }
    public bool IsFrightened { get { return frightened; } }

    // Called by the GhostManager right after instantiation
    public void Init(GhostManager owner, Maze_Task2 m, int hRow, int hCol)
    {
        manager = owner;
        maze = m;
        homeRow = hRow;
        homeCol = hCol;

        sr = GetComponent<SpriteRenderer>();

        // EXTRA: ghosts must also go home when a new map is generated
        if (maze != null)
            maze.onMazeGenerated += ResetToHome;

        ResetToHome();
    }

    void OnDestroy()
    {
        if (maze != null)
            maze.onMazeGenerated -= ResetToHome;
    }

    // Turns the "frightened" (edible) behaviour on or off
    public void SetFrightened(bool value)
    {
        if (eaten)
            return;

        frightened = value;
        blinkOn = false;
        blinkTimer = 0f;

        if (sr == null)
            return;

        if (frightened)
        {
            // The "blink" sprite is the white-bodied one, so it can be tinted
            // with either of the two flashing colours.
            sr.sprite = frightenedBlinkSprite != null ? frightenedBlinkSprite : frightenedSprite;
            sr.color = manager != null ? manager.FrightenedColorA : new Color(1f, 0.25f, 0.25f);
        }
        else
        {
            sr.sprite = normalSprite;
            sr.color = Color.white;
        }
    }

    // The PacMan eats this ghost: hide it and bring it back after a while.
    // IMPORTANT: only the SpriteRenderer is disabled, never the GameObject,
    // otherwise this component's Update() would stop and the respawn timer
    // would never run.
    public void GetEaten()
    {
        eaten = true;
        frightened = false;

        if (sr != null)
            sr.enabled = false;

        respawnTimer = manager.RespawnDelay;
    }

    // Back to its starting square (also called when a new map is generated)
    public void ResetToHome()
    {
        row = homeRow;
        col = homeCol;

        dir = Dir.None;
        moving = false;
        eaten = false;
        frightened = false;
        blinkOn = false;
        blinkTimer = 0f;

        gameObject.SetActive(true);

        Vector3 pos = maze.GridToWorld(row, col);
        pos.z = zPos;
        transform.position = pos;

        if (sr != null)
        {
            sr.enabled = true;
            sr.sprite = normalSprite;
            sr.color = Color.white;
        }
    }

    void Update()
    {
        // Nothing to do until the maze has been generated
        if (maze == null || !maze.IsReady)
            return;

        if (eaten)
        {
            respawnTimer -= Time.deltaTime;

            if (respawnTimer <= 0f)
                ResetToHome();

            return;
        }

        // While the ghosts are edible they flash between two colours
        if (frightened)
            Blink();

        if (!moving)
            StartNextMove();

        StepMove();
    }

    void Blink()
    {
        if (sr == null || manager == null)
            return;

        blinkTimer += Time.deltaTime;

        if (blinkTimer < manager.FrightenedBlinkInterval)
            return;

        blinkTimer = 0f;
        blinkOn = !blinkOn;

        // Flash between the two colours chosen in the GhostManager
        sr.color = blinkOn ? manager.FrightenedColorB : manager.FrightenedColorA;
    }

    void StartNextMove()
    {
        Dir next = ChooseDirection();

        if (next == Dir.None)
            return;

        dir = next;

        Vector2Int delta = DirToDelta(next);
        targetRow = row + delta.x;
        targetCol = col + delta.y;

        targetPos = maze.GridToWorld(targetRow, targetCol);
        targetPos.z = zPos;
        moving = true;
    }

    void StepMove()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        if (transform.position == targetPos)
        {
            row = targetRow;
            col = targetCol;
            moving = false;
        }
    }

    // Same idea as the original game: ghosts cannot turn back, they decide
    // the next direction when they reach a square centre.
    Dir ChooseDirection()
    {
        Dir reverse = Opposite(dir);

        List<Dir> options = new List<Dir>();
        Dir[] all = { Dir.Up, Dir.Down, Dir.Left, Dir.Right };

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == reverse)
                continue;

            Vector2Int d = DirToDelta(all[i]);

            if (maze.IsCorridor(row + d.x, col + d.y))
                options.Add(all[i]);
        }

        // Dead end: the only way out is to turn back
        if (options.Count == 0)
        {
            if (reverse != Dir.None)
            {
                Vector2Int d = DirToDelta(reverse);

                if (maze.IsCorridor(row + d.x, col + d.y))
                    return reverse;
            }

            return Dir.None;
        }

        // Wander around at random when edible, or when the chase is disabled
        if (frightened || manager == null || !manager.ChasePacMan)
            return options[Random.Range(0, options.Count)];

        // Chase: move to the neighbour that is closest to the PacMan
        Dir best = options[0];
        int bestDistance = int.MaxValue;

        for (int i = 0; i < options.Count; i++)
        {
            Vector2Int d = DirToDelta(options[i]);
            int distance = manager.DistanceToPacMan(row + d.x, col + d.y);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = options[i];
            }
        }

        return best;
    }

    Vector2Int DirToDelta(Dir d)
    {
        switch (d)
        {
            case Dir.Up:    return new Vector2Int(1, 0);
            case Dir.Down:  return new Vector2Int(-1, 0);
            case Dir.Left:  return new Vector2Int(0, -1);
            case Dir.Right: return new Vector2Int(0, 1);
            default:        return Vector2Int.zero;
        }
    }

    Dir Opposite(Dir d)
    {
        switch (d)
        {
            case Dir.Up:    return Dir.Down;
            case Dir.Down:  return Dir.Up;
            case Dir.Left:  return Dir.Right;
            case Dir.Right: return Dir.Left;
            default:        return Dir.None;
        }
    }
}
