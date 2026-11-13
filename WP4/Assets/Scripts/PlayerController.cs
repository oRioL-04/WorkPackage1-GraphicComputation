using UnityEngine;
using UnityEngine.InputSystem;

// Physics-based player movement (Rigidbody) and ball throwing, using the new Input System.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float turnSpeed = 720f;       // degrees per second
    public float acceleration = 40f;
    public float deceleration = 60f;
    public float skin = 0.03f;           // safety gap kept between the player and the walls

    [Header("Collider")]
    public float colliderRadius = 0.33f;
    public float colliderHeight = 1.2f;

    [Header("Ball")]
    public GameObject ballPrefab;
    public float throwForce = 15f;
    public float throwDelay = 0.3f;      // delay before the ball is released (synced with the throw animation)
    public float throwCooldown = 0.6f;
    public AudioClip throwSound;         // optional

    [Header("References")]
    public Animator animator;            // parameters: Speed (float), Throw (trigger)
    public Transform cameraTransform;    // movement is relative to the camera

    [HideInInspector] public Maze3DBuilder maze;   // assigned by MazeSpawner

    Rigidbody rb;
    CapsuleCollider cap;
    Transform hips;
    Vector3 horizontalVelocity;
    Vector3 spawnPosition;
    Vector2 moveInput;
    float nextThrowTime;

    InputAction moveAction, throwAction, restartAction;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (!animator) animator = GetComponentInChildren<Animator>();
        spawnPosition = transform.position;

        // Frictionless capsule so the player does not stick to the walls
        cap = GetComponent<CapsuleCollider>();
        if (!cap) cap = gameObject.AddComponent<CapsuleCollider>();
        cap.radius = colliderRadius;
        cap.height = colliderHeight;
        cap.center = new Vector3(0f, colliderHeight * 0.5f, 0f);
        cap.sharedMaterial = new PhysicsMaterial("PlayerNoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };

        // Input actions (New Input System)
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        throwAction = new InputAction("Throw", InputActionType.Button);
        throwAction.AddBinding("<Keyboard>/space");
        throwAction.AddBinding("<Gamepad>/buttonSouth");

        restartAction = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");
    }

    void OnEnable()
    {
        moveAction.Enable();
        throwAction.Enable();
        restartAction.Enable();
        throwAction.performed += OnThrow;
        restartAction.performed += OnRestart;
    }

    void OnDisable()
    {
        throwAction.performed -= OnThrow;
        restartAction.performed -= OnRestart;
        moveAction.Disable();
        throwAction.Disable();
        restartAction.Disable();
    }

    void Update()
    {
        bool dead = GameManager.Instance != null && GameManager.Instance.PlayerDead;
        moveInput = dead ? Vector2.zero : moveAction.ReadValue<Vector2>();
        if (animator) animator.SetFloat("Speed", moveInput.magnitude);
    }

    // The animation clips contain horizontal movement of the hips. We cancel it every frame
    // so the model always stays on top of the physics collider.
    void LateUpdate()
    {
        if (!animator || animator.transform == transform) return;

        Transform model = animator.transform;
        model.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (hips == null) hips = FindHips(model);
        if (hips == null) return;

        Vector3 offset = hips.position - transform.position;
        offset.y = 0f;
        model.position -= offset;
    }

    static Transform FindHips(Transform root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
            if (t.name.ToLowerInvariant().Contains("hips")) return t;
        return null;
    }

    void FixedUpdate()
    {
        ConfineToMaze();

        // Desired direction, relative to the camera
        Vector3 forward = Vector3.forward, right = Vector3.right;
        if (cameraTransform)
        {
            forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }
        Vector3 direction = forward * moveInput.y + right * moveInput.x;
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        // Smooth acceleration / deceleration
        Vector3 target = direction * moveSpeed;
        float rate = (target.sqrMagnitude > 0.001f ? acceleration : deceleration) * Time.fixedDeltaTime;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, rate);

        // Clip the movement against the walls and slide along them
        Vector3 allowed = CollideAndSlide(horizontalVelocity * Time.fixedDeltaTime);
        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(allowed.x, 0f, allowed.z) / Time.fixedDeltaTime + Vector3.up * v.y;

        // Face the direction of the input
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, look, turnSpeed * Time.fixedDeltaTime));
        }
    }

    // Returns the part of "move" that can be done without entering a wall (up to 3 slide passes for corners).
    Vector3 CollideAndSlide(Vector3 move)
    {
        Vector3 total = Vector3.zero;
        Vector3 remaining = new Vector3(move.x, 0f, move.z);

        for (int i = 0; i < 3 && remaining.sqrMagnitude > 1e-8f; i++)
        {
            float distance = remaining.magnitude;
            Vector3 dir = remaining / distance;

            Vector3 center = transform.TransformPoint(cap.center) + total;
            float half = Mathf.Max(0f, cap.height * 0.5f - cap.radius);
            Vector3 p1 = center + Vector3.up * (-half + 0.05f);   // lifted a bit to ignore the floor
            Vector3 p2 = center + Vector3.up * half;

            // Closest solid wall in front of us
            float closest = float.MaxValue;
            Vector3 normal = Vector3.zero;
            foreach (RaycastHit hit in Physics.CapsuleCastAll(p1, p2, cap.radius, dir, distance + skin, ~0, QueryTriggerInteraction.Ignore))
            {
                bool isWall = hit.collider.attachedRigidbody == null && !hit.collider.transform.IsChildOf(transform);
                if (!isWall || Mathf.Abs(hit.normal.y) > 0.5f) continue;
                if (hit.distance < closest) { closest = hit.distance; normal = hit.normal; }
            }

            if (closest == float.MaxValue) { total += remaining; break; }

            float step = Mathf.Max(0f, closest - skin);
            total += dir * step;
            normal.y = 0f;
            remaining = Vector3.ProjectOnPlane(dir * (distance - step), normal.normalized);
        }
        return total;
    }

    // Safety net based on the maze grid (independent from the physics engine):
    // keeps the player inside the map and out of wall cells (1 = wall, 2 = destructible wall).
    void ConfineToMaze()
    {
        if (maze == null || maze.grid == null) return;

        Vector3 p = rb.position;
        if (p.y < -3f)   // fell out of the world: back to the start
        {
            rb.position = spawnPosition;
            rb.linearVelocity = Vector3.zero;
            horizontalVelocity = Vector3.zero;
            return;
        }

        int w = maze.grid.GetLength(0), h = maze.grid.GetLength(1);
        float cs = maze.cellSize, half = cs * 0.5f, r = cap.radius;
        Vector3 original = p;

        // Keep inside the outer border walls
        p.x = Mathf.Clamp(p.x, cs - half + r, (w - 1) * cs - half - r);
        p.z = Mathf.Clamp(p.z, cs - half + r, (h - 1) * cs - half - r);

        // Push the player circle out of the neighbouring wall cells (each cell is a square)
        for (int pass = 0; pass < 3; pass++)
        {
            int cx = Mathf.RoundToInt(p.x / cs), cz = Mathf.RoundToInt(p.z / cs);
            for (int i = cx - 1; i <= cx + 1; i++)
            {
                for (int j = cz - 1; j <= cz + 1; j++)
                {
                    if (i < 0 || j < 0 || i >= w || j >= h || maze.grid[i, j] == 0) continue;

                    float closestX = Mathf.Clamp(p.x, i * cs - half, i * cs + half);
                    float closestZ = Mathf.Clamp(p.z, j * cs - half, j * cs + half);
                    float dx = p.x - closestX, dz = p.z - closestZ;
                    float sqrDist = dx * dx + dz * dz;
                    if (sqrDist >= r * r) continue;

                    if (sqrDist > 1e-6f)
                    {
                        float dist = Mathf.Sqrt(sqrDist);
                        float push = (r - dist) / dist;
                        p.x += dx * push;
                        p.z += dz * push;
                    }
                    else
                    {
                        // Center is inside the cell: leave through the nearest face
                        float left = p.x - (i * cs - half), rightSide = (i * cs + half) - p.x;
                        float bottom = p.z - (j * cs - half), top = (j * cs + half) - p.z;
                        float min = Mathf.Min(Mathf.Min(left, rightSide), Mathf.Min(bottom, top));
                        if (min == left) p.x = i * cs - half - r;
                        else if (min == rightSide) p.x = i * cs + half + r;
                        else if (min == bottom) p.z = j * cs - half - r;
                        else p.z = j * cs + half + r;
                    }
                }
            }
        }

        if ((p - original).sqrMagnitude > 1e-8f) rb.position = p;
    }

    // Sends the player back to the start of the maze (used when an enemy catches them)
    public void Respawn()
    {
        rb.position = spawnPosition;
        rb.linearVelocity = Vector3.zero;
        horizontalVelocity = Vector3.zero;
    }

    void OnThrow(InputAction.CallbackContext ctx)
    {
        if (PauseMenu.IsPaused || (GameManager.Instance != null && GameManager.Instance.PlayerDead) || Time.time < nextThrowTime || ballPrefab == null) return;
        nextThrowTime = Time.time + throwCooldown;
        if (animator) animator.SetTrigger("Throw");
        Invoke(nameof(SpawnBall), throwDelay);
    }

    void SpawnBall()
    {
        // If a wall is right in front of us, spawn the ball closer so it does not start inside the wall
        Vector3 origin = transform.position + Vector3.up * 0.7f;
        float distance = 0.7f;
        if (Physics.SphereCast(origin, 0.2f, transform.forward, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<PlayerController>() == null)
            distance = Mathf.Max(0f, hit.distance - 0.05f);

        Vector3 position = origin + transform.forward * distance;
        GameObject ball = Instantiate(ballPrefab, position, Quaternion.identity);

        // The ball must not collide with the player who throws it
        foreach (Collider playerCol in GetComponentsInChildren<Collider>())
            foreach (Collider ballCol in ball.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(playerCol, ballCol);

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb) ballRb.linearVelocity = transform.forward * throwForce + Vector3.up * 1.5f;
        if (throwSound) AudioSource.PlayClipAtPoint(throwSound, position);
    }

    void OnRestart(InputAction.CallbackContext ctx)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
    }
}
