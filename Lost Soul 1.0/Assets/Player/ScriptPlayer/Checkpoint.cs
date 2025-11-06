using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement; // ADICIONADO

public class Checkpoint : MonoBehaviour
{
    public string checkpointID; // ID único
    public bool isActive = false;
    private Animator animator;

    private bool playerNearby = false;
    private Transform playerTransform;

    [Header("Particle System")]
    public ParticleSystem activeParticles; // arrasta o particle system aqui no inspector

    [Header("Light")]
    public Light2D checkpointLight; // se estiver usando Light 2D

    [Header("UI de Interação")]
    public GameObject interactionUI; // Canvas com a letra "E"

    private void Start()
    {
        animator = GetComponent<Animator>();

        // Força começar inativo
        isActive = false;
        UpdateVisual(false);

        // Desliga partículas
        if (activeParticles != null)
            activeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Esconde o UI de interação
        if (interactionUI != null)
            interactionUI.SetActive(false);

        // Checa se este é o checkpoint salvo
        string savedID = PlayerPrefs.GetString("LastCheckpointID", "");
        if (!string.IsNullOrEmpty(savedID) && savedID == checkpointID)
        {
            isActive = true;
            UpdateVisual(true);
        }
    }

    private void Update()
    {
        // Se o jogador estiver perto e apertar E
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            // Só ativa se ainda não estiver ativo
            if (!isActive)
            {
                ActivateCheckpoint(playerTransform);

                // Esconde o E após ativar
                if (interactionUI != null)
                    interactionUI.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            playerTransform = other.transform;

            // Mostra o UI de interação apenas se não estiver ativo
            if (!isActive && interactionUI != null)
                interactionUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            playerTransform = null;

            // Esconde o UI de interação
            if (interactionUI != null)
                interactionUI.SetActive(false);
        }
    }

    private void ActivateCheckpoint(Transform player)
    {
        if (isActive) return; // já ativo

        // Desativa todos os outros checkpoints na cena
        Checkpoint[] all = FindObjectsOfType<Checkpoint>();
        foreach (Checkpoint cp in all)
        {
            if (cp != this)
            {
                cp.isActive = false;
                cp.UpdateVisual(false);

                // Garante que o "E" dos outros desapareça
                if (cp.interactionUI != null)
                    cp.interactionUI.SetActive(false);
            }
        }

        // Ativa este checkpoint
        isActive = true;
        UpdateVisual(true);

        // Salva posição, ID e nome da cena
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        PlayerPrefs.SetFloat("LastCheckpointX", transform.position.x);
        PlayerPrefs.SetFloat("LastCheckpointY", transform.position.y);
        PlayerPrefs.SetString("LastCheckpointID", checkpointID);
        PlayerPrefs.SetString("LastCheckpointScene", sceneName);
        PlayerPrefs.Save();

        Debug.Log($"[Checkpoint] Checkpoint salvo: ID='{checkpointID}', Pos={transform.position}, Scene='{sceneName}'");
    }

    private void UpdateVisual(bool active)
    {
        if (animator != null)
            animator.SetBool("Active", active);

        // Partículas
        if (activeParticles != null)
        {
            if (active && !activeParticles.isPlaying)
                activeParticles.Play();
            else if (!active && activeParticles.isPlaying)
                activeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Luz
        if (checkpointLight != null)
            checkpointLight.enabled = active;
    }
}
