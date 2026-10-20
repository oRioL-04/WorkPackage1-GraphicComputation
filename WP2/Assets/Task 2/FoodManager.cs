using System.Collections;
using UnityEngine;

// Task 2 - Food elements
// Puts one food element in the centre of every "Corridor" square, lets the
// PacMan eat them, shows how many are still left on screen and, once the whole
// map has been eaten, asks the maze for a brand new random one.
public class FoodManager : MonoBehaviour
{
    // Maze that fills the map and notifies us when a new one is generated
    public Maze_Task2 maze;

    // Prefab of a single food element (it carries an Animation component)
    public GameObject foodPrefab;

    // Name of the one-shot AnimationClip played when a food is eaten
    const string eatClipName = "FoodEat";

    // food[row, col] keeps the food element of that square (null if eaten)
    GameObject[,] food;

    int foodCount = 0;

    void Start()
    {
        // The reference is normally set in the Inspector, this is only a
        // safety net in case it was left empty.
        if (maze == null)
            maze = FindFirstObjectByType<Maze_Task2>();

        // EXTRA: same idea as the PacMan, every new map must be filled again
        if (maze != null)
            maze.onMazeGenerated += BuildFood;

        BuildFood();
    }

    void OnDestroy()
    {
        if (maze != null)
            maze.onMazeGenerated -= BuildFood;
    }

    public int FoodCount { get { return foodCount; } }

    // Puts a food element in the centre of every corridor square
    void BuildFood()
    {
        // The maze builds its map in its own Start(). If we happen to run
        // before it we just wait for the onMazeGenerated event.
        if (maze == null || !maze.IsReady)
            return;

        ClearFood();

        food = new GameObject[maze.Rows, maze.Cols];
        foodCount = 0;

        for (int i = 0; i < maze.Rows; i++)
        {
            for (int j = 0; j < maze.Cols; j++)
            {
                if (!maze.IsCorridor(i, j))
                    continue;

                GameObject obj = Instantiate(foodPrefab);

                // Centre of the square. A tiny negative Z brings the food in
                // front of the corridor square (same Sorting Order) but still
                // behind the PacMan (Sorting Order 1).
                Vector3 pos = maze.GridToWorld(i, j);
                pos.z = -0.1f;

                obj.transform.position = pos;
                obj.transform.parent = this.gameObject.transform;

                food[i, j] = obj;
                foodCount++;
            }
        }
    }

    void ClearFood()
    {
        if (food == null)
            return;

        for (int i = 0; i < food.GetLength(0); i++)
        {
            for (int j = 0; j < food.GetLength(1); j++)
            {
                if (food[i, j] != null)
                    Destroy(food[i, j]);
            }
        }

        food = null;
    }

    // Called by the PacMan every time it moves into a new square
    public void EatFood(int i, int j)
    {
        if (food == null)
            return;

        if (i < 0 || i >= food.GetLength(0) ||
            j < 0 || j >= food.GetLength(1))
            return;

        if (food[i, j] == null)
            return;

        // The food leaves the map right away (so it can never be eaten twice)
        // but the GameObject stays alive a moment to play its "pop" animation.
        GameObject obj = food[i, j];
        food[i, j] = null;
        foodCount--;

        PlayEatAnimation(obj);

        // EXTRA: the whole map has been eaten -> new random map. The maze will
        // notify us (onMazeGenerated) and BuildFood() will refill it.
        if (foodCount <= 0)
            maze.RegenerateMaze();
    }

    // Plays the one-shot "FoodEat" clip of the food element and destroys it
    // once the animation is over.
    void PlayEatAnimation(GameObject obj)
    {
        float duration = 0f;
        Animation anim = obj.GetComponent<Animation>();

        if (anim != null && anim.GetClip(eatClipName) != null)
        {
            // Stop the idle pulse and play the eating animation
            anim.Stop();
            anim.Play(eatClipName);

            duration = Mathf.Max(anim.GetClip(eatClipName).length, 0.02f);
        }

        StartCoroutine(DestroyAfter(obj, duration));
    }

    IEnumerator DestroyAfter(GameObject obj, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // It may already be gone (e.g. the whole map was regenerated)
        if (obj != null)
            Destroy(obj);
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 34;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        string text = "Food elements: " + foodCount;
        Vector2 size = style.CalcSize(new GUIContent(text));

        float padX = 16f;
        float padY = 12f;
        float x = 24f;
        float y = 20f;

        // Dark panel behind the text
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(x, y, size.x + padX * 2f, size.y + padY * 2f),
                        Texture2D.whiteTexture);

        // Black shadow + white text (easy to read over any background)
        GUI.color = Color.white;
        GUIStyle shadow = new GUIStyle(style);
        shadow.normal.textColor = new Color(0f, 0f, 0f, 0.85f);

        GUI.Label(new Rect(x + padX + 2f, y + padY + 2f, size.x, size.y), text, shadow);
        GUI.Label(new Rect(x + padX, y + padY, size.x, size.y), text, style);
    }
}
