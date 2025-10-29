using UnityEngine;
using UnityEngine.Rendering.Universal;

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


    private void Start()
    {
        animator = GetComponent<Animator>();

        // Força começar inativo
        isActive = false;
        UpdateVisual(false);

        // Desliga partículas
        if (activeParticles != null)
            activeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

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
            ActivateCheckpoint(playerTransform);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            playerTransform = other.transform;
            // opcional: mostrar dica "Pressione E"
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            playerTransform = null;
            // opcional: esconder dica
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
            }
        }

        // Ativa este checkpoint
        isActive = true;
        UpdateVisual(true);

        // Salva posição e ID
        PlayerPrefs.SetFloat("LastCheckpointX", transform.position.x);
        PlayerPrefs.SetFloat("LastCheckpointY", transform.position.y);
        PlayerPrefs.SetString("LastCheckpointID", checkpointID);
        PlayerPrefs.Save();

        Debug.Log($"[Checkpoint] Checkpoint salvo: {checkpointID} ({transform.position})");
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
