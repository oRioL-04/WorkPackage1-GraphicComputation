using UnityEngine;

// Keeps track of food and walls, and draws a simple HUD.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int foodTotal, foodCollected;
    public int wallsTotal, wallsDestroyed;

    public bool AllFoodCollected => foodCollected >= foodTotal;
    public bool Won { get; private set; }

    string hint;
    float hintUntil;

    void Awake()
    {
        Instance = this;
    }

    public void Setup(int food, int walls)
    {
        foodTotal = food;
        wallsTotal = walls;
        foodCollected = 0;
        wallsDestroyed = 0;
        Won = false;
    }

    public void FoodCollected() { foodCollected++; }
    public void WallDestroyed() { wallsDestroyed++; }
    public void Win() { Won = true; }

    public void ShowHint(string message)
    {
        hint = message;
        hintUntil = Time.time + 2f;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(15, 10, 500, 30), $"Food: {foodCollected}/{foodTotal}", style);
        GUI.Label(new Rect(15, 40, 500, 30), $"Walls destroyed: {wallsDestroyed}/{wallsTotal}", style);
        GUI.Label(new Rect(15, 70, 700, 30), "WASD move | Space throw ball | R restart", style);
        GUI.Label(new Rect(15, 100, 700, 30),
            AllFoodCollected ? "Go to the green goal!" : "Collect all the food and reach the goal", style);

        if (!Won && Time.time < hintUntil)
        {
            GUIStyle small = new GUIStyle(style) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
            small.normal.textColor = Color.red;
            GUI.Label(new Rect(0, Screen.height / 2f - 20, Screen.width, 50), hint, small);
        }

        if (Won)
        {
            GUIStyle big = new GUIStyle(style) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Color.yellow;
            GUI.Label(new Rect(0, Screen.height / 2f - 40, Screen.width, 80), "YOU WIN! (press R to restart)", big);
        }
    }
}
