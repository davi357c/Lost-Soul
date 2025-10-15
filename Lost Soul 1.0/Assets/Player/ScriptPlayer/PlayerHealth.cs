using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida")]
    public int maxLives = 5;
    public int currentLives;

    [Header("UI dos Corações")]
    public Image[] hearts; // arraste as 5 imagens no inspector

    void Start()
    {
        currentLives = maxLives;
        UpdateHeartsUI();
    }

    public void TakeDamage()
    {
        if (currentLives <= 0) return;

        currentLives--;
        UpdateHeartsUI();

        if (currentLives <= 0)
        {
            // Player morreu — pode colocar algo tipo "Game Over"
            Debug.Log("Player morreu!");
        }
    }

    void UpdateHeartsUI()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            // mostra apenas o número atual de corações
            hearts[i].enabled = i < currentLives;
        }
    }
}
