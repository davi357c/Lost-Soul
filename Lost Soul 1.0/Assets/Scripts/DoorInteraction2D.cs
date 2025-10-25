using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteraction2D : MonoBehaviour
{
    [Header("Configurações da Porta")]
    [Tooltip("Canvas (World Space) com o 'E' - arraste aqui")]
    public GameObject interactionUI;

    [Tooltip("Nome da cena para onde o player será levado")]
    public string sceneToLoad;

    [Tooltip("Nome do ponto de spawn na próxima cena")]
    public string spawnPointName;

    private bool playerInRange = false;

    void Start()
    {
        if (interactionUI != null)
            interactionUI.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
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

        // Salva o nome do ponto onde o player deve nascer na próxima cena
        PlayerPrefs.SetString("LastSpawnPoint", spawnPointName);
        PlayerPrefs.Save();

        // Carrega a próxima cena
        SceneManager.LoadScene(sceneToLoad);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
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
