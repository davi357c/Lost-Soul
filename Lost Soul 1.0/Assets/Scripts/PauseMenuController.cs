using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject pauseMenuPanel;
    public Slider volumeSlider;
    public Slider brightnessSlider;

    [Header("Valores Padrão")]
    public float defaultVolume = 1f;
    public float defaultBrightness = 0.5f;

    private bool isPaused = false;

    void Start()
    {
        pauseMenuPanel.SetActive(false);

        // Inicializa sliders com valores atuais
        volumeSlider.value = defaultVolume;
        brightnessSlider.value = defaultBrightness;

        // Aplica os valores iniciais no jogo
        SetVolume(volumeSlider.value);
        SetBrightness(brightnessSlider.value);
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
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f; // pausa o jogo
        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f; // continua o jogo
        isPaused = false;
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Exit Game chamado!");
    }

    // Chamado pelo slider de volume
    public void SetVolume(float value)
    {
        AudioListener.volume = value; // ajusta o volume global
    }

    // Chamado pelo slider de brightness
    public void SetBrightness(float value)
    {
        RenderSettings.ambientLight = new Color(value, value, value);
    }

    // Botão de reset para valores padrão
    public void ResetDefaults()
    {
        volumeSlider.value = defaultVolume;
        brightnessSlider.value = defaultBrightness;

        // Aplica imediatamente
        SetVolume(volumeSlider.value);
        SetBrightness(brightnessSlider.value);
    }
}
