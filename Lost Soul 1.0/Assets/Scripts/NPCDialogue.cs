using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    public float interactionRange = 3f;
    public string[] dialogueLines;

    private Transform player;
    private DialogueManager dialogueManager;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        dialogueManager = FindObjectOfType<DialogueManager>();
    }

    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= interactionRange && Input.GetKeyDown(KeyCode.E))
        {
            // Só inicia diálogo se não estiver ativo
            if (dialogueManager != null && !dialogueManager.IsDialogueActive())
            {
                dialogueManager.StartDialogue(dialogueLines);
            }
        }
    }
}
