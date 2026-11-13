using UnityEngine;

// Collectible food item (trigger). Spins and disappears when the player touches it.
public class Food : MonoBehaviour
{
    public Color particleColor = new Color(1f, 0.8f, 0.1f);
    public AudioClip pickupSound;   // optional

    void Update()
    {
        transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;

        Effects.Burst(transform.position, particleColor, 20, 3f, 0.15f);
        if (pickupSound) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (GameManager.Instance != null) GameManager.Instance.FoodCollected();
        Destroy(gameObject);
    }
}
