using UnityEngine;

// Goal of the maze. It only counts once all the food has been collected.
[RequireComponent(typeof(Collider))]
public class ExitGoal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;

        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.AllFoodCollected)
        {
            gm.Win();
            Effects.Burst(transform.position + Vector3.up, Color.green, 60, 6f, 0.25f);
        }
        else
        {
            gm.ShowHint($"You still need food! ({gm.foodCollected}/{gm.foodTotal})");
        }
    }
}
