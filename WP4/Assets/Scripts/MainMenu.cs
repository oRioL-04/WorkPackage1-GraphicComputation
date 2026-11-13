using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// Start menu: Play, Credits and Quit game. The buttons are wired to these methods in the scene.
public class MainMenu : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject creditsPanel;
    public string gameScene = "Game";

    void Awake()
    {
        Time.timeScale = 1f;
        UIInput.Ensure();
        ShowMain();
    }

    public void Play() { SceneManager.LoadScene(gameScene); }

    public void ShowCredits()
    {
        mainPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void ShowMain()
    {
        mainPanel.SetActive(true);
        creditsPanel.SetActive(false);
    }

    public void Quit()
    {
        Debug.Log("Quit game");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

// Makes sure the EventSystem has input actions assigned (needed for UI clicks with the new Input System).
public static class UIInput
{
    public static void Ensure()
    {
        EventSystem es = EventSystem.current != null ? EventSystem.current : Object.FindAnyObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

        InputSystemUIInputModule module = es.GetComponent<InputSystemUIInputModule>();
        if (module == null) module = es.gameObject.AddComponent<InputSystemUIInputModule>();
        if (module.point == null || module.leftClick == null) module.AssignDefaultActions();
    }
}
