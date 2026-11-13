using UnityEngine;

// Elevated follow camera. It stays above the walls (2 m high), so it never goes through them.
public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 12f, -3.5f);
    public float smoothing = 8f;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 focus = target.position + Vector3.up * 0.5f;
        Vector3 desired = focus + offset;
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        transform.LookAt(focus);
    }
}
