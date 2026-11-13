using UnityEngine;

// Keeps track of food and walls, and draws a simple HUD.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int foodTotal, foodCollected;
    public int wallsTotal, wallsDestroyed;
    public int enemiesTotal, enemiesKilled;

    public bool AllFoodCollected => foodCollected >= foodTotal;
    public bool Won { get; private set; }
    public bool PlayerDead { get; private set; }

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
        enemiesTotal = enemiesKilled = 0;
        Won = false;
        PlayerDead = false;
    }

    public void FoodCollected() { foodCollected++; }
    public void WallDestroyed() { wallsDestroyed++; }
    public void EnemyAdded() { enemiesTotal++; }
    public void EnemyKilled() { enemiesKilled++; }
    public void Win() { Won = true; }
    public void PlayerKilled() { if (PlayerDead || Won) return; PlayerDead = true; }

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
        GUI.Label(new Rect(15, 130, 500, 30), $"Enemies killed: {enemiesKilled}/{enemiesTotal}", style);
        GUI.Label(new Rect(15, 70, 700, 30), "WASD move | Space throw ball | P pause | R restart | Esc menu", style);
        GUI.Label(new Rect(15, 100, 700, 30),
            AllFoodCollected ? "Go to the green goal!" : "Collect all the food and reach the goal", style);

        if (!Won && Time.time < hintUntil)
        {
            GUIStyle small = new GUIStyle(style) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
            small.normal.textColor = Color.red;
            GUI.Label(new Rect(0, Screen.height / 2f - 20, Screen.width, 50), hint, small);
        }

        if (PlayerDead)
        {
            GUIStyle dead = new GUIStyle(style) { fontSize = 56, alignment = TextAnchor.MiddleCenter };
            dead.normal.textColor = Color.red;
            GUI.Label(new Rect(0, Screen.height / 2f - 40, Screen.width, 80), "YOU DIED! (press R to restart)", dead);
        }

        if (Won)
        {
            GUIStyle big = new GUIStyle(style) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Color.yellow;
            GUI.Label(new Rect(0, Screen.height / 2f - 40, Screen.width, 80), "YOU WIN! (press R to restart)", big);
        }
    }
}
