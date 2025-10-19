using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxLives = 5;
    public int currentLives;

    public Animator[] hearts; // Arraste os animators dos corações

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
    }

    void UpdateHeartsUI()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            bool isAlive = i < currentLives;

            // Ativa/desativa o coração
            hearts[i].gameObject.SetActive(isAlive);

            // (opcional) se quiser manter o Animator ativo e só parar a animação:
            // hearts[i].SetBool("isAlive", isAlive);
        }
    }
}
