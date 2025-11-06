using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("Interação com NPC")]
    public float interactionRange = 3f;              // Distância para interagir
    public GameObject interactionUI;                 // UI que mostra a tecla "E"
    public DialogueLine[] dialogueLines;             // Falas do NPC

    private Transform player;
    private DialogueManager dialogueManager;
    private bool playerInRange = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        dialogueManager = FindObjectOfType<DialogueManager>();

        // Garante que o UI começa desativado
        if (interactionUI != null)
            interactionUI.SetActive(false);
    }

    void Update()
    {
        // Verifica distância do jogador
        float distance = Vector3.Distance(player.position, transform.position);
        bool isNear = distance <= interactionRange;

        // Ativa/desativa o UI
        if (isNear && !playerInRange)
        {
            playerInRange = true;
            if (interactionUI != null)
                interactionUI.SetActive(true);
        }
        else if (!isNear && playerInRange)
        {
            playerInRange = false;
            if (interactionUI != null)
                interactionUI.SetActive(false);
        }

        // Interação com o NPC
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (dialogueManager != null && !dialogueManager.IsDialogueActive())
            {
                dialogueManager.StartDialogue(dialogueLines);
            }
        }
    }
}
