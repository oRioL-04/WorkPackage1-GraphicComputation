using UnityEngine;

// Bouncy ball. It bounces off normal walls and destroys destructible walls on impact.
public class Ball : MonoBehaviour
{
    public float lifetime = 8f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Enemy enemy = collision.collider.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.Die();
            Destroy(gameObject);
            return;
        }

        DestructibleWall wall = collision.collider.GetComponentInParent<DestructibleWall>();
        if (wall == null) return;

        wall.Hit();
        Destroy(gameObject);
    }
}
