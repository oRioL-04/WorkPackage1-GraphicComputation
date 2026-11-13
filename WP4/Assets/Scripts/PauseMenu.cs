using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Pause HUD (P key) and "back to start menu" (Esc key). The pause panel belongs to the game scene:
// it is shown by enabling it and hidden by disabling it.
public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;
    public string menuScene = "MainMenu";

    public static bool IsPaused { get; private set; }

    InputAction pauseAction, menuAction;

    void Awake()
    {
        UIInput.Ensure();
        pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/p");
        menuAction = new InputAction("Menu", InputActionType.Button, "<Keyboard>/escape");
    }

    void OnEnable()
    {
        pauseAction.Enable(); menuAction.Enable();
        pauseAction.performed += OnPause;
        menuAction.performed += OnMenu;
    }

    void OnDisable()
    {
        pauseAction.performed -= OnPause;
        menuAction.performed -= OnMenu;
        pauseAction.Disable(); menuAction.Disable();
    }

    void Start()
    {
        Resume();   // a freshly loaded scene always starts unpaused
    }

    void OnPause(InputAction.CallbackContext ctx)
    {
        if (IsPaused) Resume(); else Pause();
    }

    void OnMenu(InputAction.CallbackContext ctx) { GoToMenu(); }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pausePanel) pausePanel.SetActive(true);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);
    }

    public void GoToMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }
}
