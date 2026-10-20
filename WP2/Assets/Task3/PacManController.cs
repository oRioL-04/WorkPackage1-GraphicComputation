using UnityEngine;

public enum Dir { None, Up, Down, Left, Right }

public class PacManController : MonoBehaviour
{
    public Maze_Task2 maze;
    public FoodManager food;
    public float moveSpeed = 4f;
    public float rotateSpeed = 720f;

    public Sprite mouthClosed;
    public Sprite mouthHalf;
    public Sprite mouthOpen;
    public float mouthInterval = 0.1f;

    int row, col;
    Dir currentDir = Dir.None;
    Dir desiredDir = Dir.None;

    bool isMoving = false;
    bool isTurning = false;
    Vector3 targetPos;
    Quaternion targetRot;

    SpriteRenderer sr;
    float mouthTimer = 0f;
    int mouthFrameIndex = 0;
    Sprite[] mouthFrames;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // Safety net: the reference is normally assigned in the Inspector
        if (food == null)
            food = FindFirstObjectByType<FoodManager>();

        // EXTRA: the maze tiles sit at the same Z and Sorting Order as the
        // PacMan, so force a higher order to be sure it is never drawn
        // behind a corridor square.
        sr.sortingOrder = 1;

        mouthFrames = new Sprite[] { mouthClosed, mouthHalf, mouthOpen, mouthHalf };

        // EXTRA: every time a new maze is generated (Space, or "all food
        // eaten" in Task 2) the PacMan must go back to the entrance,
        // otherwise its logical (row, col) could point at a wall of the new
        // maze and it would be able to walk through it.
        maze.onMazeGenerated += ResetToEntrance;
        ResetToEntrance();
    }

    void OnDestroy()
    {
        if (maze != null)
            maze.onMazeGenerated -= ResetToEntrance;
    }

    // Puts the PacMan back at the entrance, standing still and facing right.
    // (public: the GhostManager uses it when a ghost catches the PacMan)
    public void ResetToEntrance()
    {
        isMoving = false;
        isTurning = false;
        currentDir = Dir.None;
        desiredDir = Dir.None;

        row = maze.EntranceRow;
        col = maze.EntranceCol;
        transform.position = maze.GridToWorld(row, col);
        transform.rotation = Quaternion.identity;

        sr.sprite = mouthClosed;
        mouthFrameIndex = 0;
        mouthTimer = 0f;
    }

    void Update()
    {
        ReadInput();

        if (isTurning)
        {
            StepTurn();
            return;
        }

        if (!isMoving)
        {
            TryStartNextMove();
        }
        else
        {
            StepMove();
        }

        AnimateMouth();
    }

    // Decides what to do when the PacMan is standing on a tile centre:
    // turn towards the requested direction if possible, otherwise keep going
    // in the current direction, otherwise stay still.
    void TryStartNextMove()
    {
        if (desiredDir != Dir.None && desiredDir != currentDir && CanMove(desiredDir))
        {
            StartTurn(desiredDir);
        }
        else if (currentDir != Dir.None && CanMove(currentDir))
        {
            StartMove(currentDir);
        }
    }

    void ReadInput()
    {
        if (Input.GetKey(KeyCode.W)) desiredDir = Dir.Up;
        else if (Input.GetKey(KeyCode.S)) desiredDir = Dir.Down;
        else if (Input.GetKey(KeyCode.A)) desiredDir = Dir.Left;
        else if (Input.GetKey(KeyCode.D)) desiredDir = Dir.Right;
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

    bool CanMove(Dir d)
    {
        Vector2Int delta = DirToDelta(d);
        return maze.IsCorridor(row + delta.x, col + delta.y);
    }

    // EXTRA: the ghosts need to know where the PacMan is
    public int Row { get { return row; } }
    public int Col { get { return col; } }

    void StartMove(Dir d)
    {
        currentDir = d;
        Vector2Int delta = DirToDelta(d);
        row += delta.x;
        col += delta.y;
        targetPos = maze.GridToWorld(row, col);
        isMoving = true;
    }

    void StepMove()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        if (transform.position == targetPos)
        {
            isMoving = false;

            // Task 2: the PacMan has reached the centre of the new square, so
            // it eats the food element it contains (if there is one).
            if (food != null)
                food.EatFood(row, col);

            // Continue straight away instead of idling for one frame at every
            // tile centre, so both the movement and the mouth stay smooth.
            TryStartNextMove();
        }
    }

    void StartTurn(Dir d)
    {
        Quaternion newRot = DirToRotation(d);
        currentDir = d;

        if (newRot == transform.rotation)
        {
            StartMove(d);
        }
        else
        {
            targetRot = newRot;
            isTurning = true;
        }
    }

    void StepTurn()
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        if (transform.rotation == targetRot)
        {
            isTurning = false;
            StartMove(currentDir);
        }
        AnimateMouth();
    }

    Quaternion DirToRotation(Dir d)
    {
        switch (d)
        {
            case Dir.Right: return Quaternion.Euler(0, 0, 0);
            case Dir.Up:    return Quaternion.Euler(0, 0, 90);
            case Dir.Left:  return Quaternion.Euler(0, 0, 180);
            case Dir.Down:  return Quaternion.Euler(0, 0, -90);
            default:        return transform.rotation;
        }
    }

    void AnimateMouth()
    {
        // Keep chomping while there is movement in progress OR while the
        // PacMan is on a tile centre with somewhere still to go. Checking
        // CanMove(currentDir) avoids the one-frame "stop" at every tile
        // centre that used to reset (and blink) the animation.
        bool active = isMoving || isTurning ||
                      (currentDir != Dir.None && CanMove(currentDir));

        if (active)
        {
            mouthTimer += Time.deltaTime;
            if (mouthTimer >= mouthInterval)
            {
                mouthTimer = 0f;
                mouthFrameIndex = (mouthFrameIndex + 1) % mouthFrames.Length;
                sr.sprite = mouthFrames[mouthFrameIndex];
            }
        }
        else
        {
            sr.sprite = mouthClosed;
            mouthFrameIndex = 0;
            mouthTimer = 0f;
        }
    }
}