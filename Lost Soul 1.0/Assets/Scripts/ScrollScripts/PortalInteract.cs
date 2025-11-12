using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalInteract : MonoBehaviour
{
    [Header("Configuração do Portal")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("Nome da cena que será carregada (configure no Inspector).")]
    public string sceneToLoad;
    [Tooltip("UI que mostra 'Pressione E para entrar' (opcional).")]
    public GameObject promptUI;

    private bool playerNearby = false;
    private bool isLoading = false;

    void Start()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void Update()
    {
        if (playerNearby && !isLoading)
        {
            if (Input.GetKeyDown(interactKey))
            {
                if (!string.IsNullOrEmpty(sceneToLoad))
                {
                    isLoading = true;
                    SceneManager.sceneLoaded += OnSceneLoaded; // Quando a cena carregar, chama a função abaixo
                    SceneManager.LoadScene(sceneToLoad);
                }
                else
                {
                    Debug.LogWarning("PortalInteract: 'sceneToLoad' não configurada.");
                }
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // remove o evento pra não repetir

        // Quando a cena nova carregar, move o player até o objeto "PlayerSpawn"
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        GameObject spawn = GameObject.Find("PlayerSpawn");

        if (player != null && spawn != null)
        {
            player.transform.position = spawn.transform.position;
        }
        else if (spawn == null)
        {
            Debug.LogWarning("Nenhum objeto chamado 'PlayerSpawn' foi encontrado na nova cena!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            playerNearby = true;
            if (promptUI != null) promptUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            playerNearby = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }
}
