using UnityEngine;
using System.Collections;

public class NPCInteractionWithCutscene : MonoBehaviour
{
    [Header("Interação com NPC")]
    public float interactionRange = 3f;
    public GameObject interactionUI;
    public DialogueLine[] dialogueLines;

    [Header("Cutscene após diálogo (opcional)")]
    public NPCCutscene cutsceneAfterDialogue;

    private Transform player;
    private DialogueManager dialogueManager;
    private bool playerInRange = false;
    private bool dialogueStarted = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        dialogueManager = FindObjectOfType<DialogueManager>();

        if (interactionUI != null)
            interactionUI.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        // Distância do jogador
        float distance = Vector3.Distance(player.position, transform.position);
        bool isNear = distance <= interactionRange;

        // Ativa/desativa UI
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

        // Interação
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !dialogueStarted)
        {
            if (dialogueManager != null && !dialogueManager.IsDialogueActive())
            {
                dialogueManager.StartDialogue(dialogueLines);
                dialogueStarted = true;
                StartCoroutine(WaitForDialogueEnd());
            }
        }
    }

    private IEnumerator WaitForDialogueEnd()
    {
        // Espera até o diálogo terminar
        while (dialogueManager.IsDialogueActive())
        {
            yield return null;
        }

        // Dispara cutscene se tiver
        if (cutsceneAfterDialogue != null)
            cutsceneAfterDialogue.StartCutscene(player);

        dialogueStarted = false;
    }
}
