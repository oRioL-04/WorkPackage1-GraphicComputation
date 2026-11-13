using UnityEngine;

// Wall that blocks a corridor until it is hit by a ball.
public class DestructibleWall : MonoBehaviour
{
    public Color debrisColor = new Color(0.6f, 0.35f, 0.15f);
    public int debrisCount = 12;
    public AudioClip breakSound;   // optional

    [HideInInspector] public Maze3DBuilder maze;
    [HideInInspector] public Vector2Int cell;
    bool destroyed;

    public void Hit()
    {
        if (destroyed) return;
        destroyed = true;

        // Free the cell in the maze grid so the player can walk through
        if (maze != null && maze.grid != null) maze.grid[cell.x, cell.y] = 0;

        Vector3 center = transform.position;
        Effects.Burst(center + Vector3.up * 0.5f, debrisColor, 60, 6f, 0.3f);

        // Physical debris
        for (int i = 0; i < debrisCount; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.transform.position = center + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, 1.6f), Random.Range(-0.6f, 0.6f));
            piece.transform.localScale = Vector3.one * Random.Range(0.2f, 0.45f);
            piece.transform.rotation = Random.rotation;
            piece.GetComponent<Renderer>().material.color = debrisColor * Random.Range(0.7f, 1.1f);

            Rigidbody pieceRb = piece.AddComponent<Rigidbody>();
            pieceRb.AddExplosionForce(250f, center, 3f);
            Destroy(piece, 3f);
        }

        if (breakSound) AudioSource.PlayClipAtPoint(breakSound, center);
        if (GameManager.Instance != null) GameManager.Instance.WallDestroyed();
        Destroy(gameObject);
    }
}
