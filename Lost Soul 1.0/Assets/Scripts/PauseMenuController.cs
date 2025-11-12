using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [Header("Painel do Menu de Pausa (Canvas ou Panel)")]
    public GameObject pauseMenuPanel;

    private bool isPaused = false;
    private static PauseMenuController instance;

    void Awake()
    {
        // Singleton leve para persistir entre cenas
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        else
            Debug.LogWarning("⚠️ PauseMenuPanel não atribuído no inspector!");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Exit Game chamado!");
    }
}
