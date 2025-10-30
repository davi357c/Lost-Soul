using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteraction2D : MonoBehaviour
{
    [Header("Configurações da Porta")]
    public GameObject interactionUI;
    public string sceneToLoad;
    public string spawnPointName;

    private bool playerInRange = false;
    private bool canInteract = false;

    void Start()
    {
        if (interactionUI != null)
            interactionUI.SetActive(false);

        // Previne interação imediata (delay de 0.5s)
        Invoke(nameof(EnableInteraction), 0.5f);
    }

    void EnableInteraction()
    {
        canInteract = true;
    }

    void Update()
    {
        if (canInteract && playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            EnterDoor();
        }
    }

    private void EnterDoor()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("Cena de destino não configurada na porta!");
            return;
        }

        PlayerPrefs.SetString("LastSpawnPoint", spawnPointName);
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneToLoad);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (canInteract && other.CompareTag("Player"))
        {
            playerInRange = true;
            if (interactionUI != null)
                interactionUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactionUI != null)
                interactionUI.SetActive(false);
        }
    }
}
