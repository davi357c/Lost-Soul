using UnityEngine;

public class NpcInteraction : MonoBehaviour
{
    [Header("Referências")]
    public ShopManager shopManager;
    public GameObject interactionUI; // Canvas ou imagem com a letra "E"

    [Header("Configurações")]
    public float interactCooldown = 0.3f; // tempo mínimo entre apertos

    private bool playerNearby;
    private float lastInteractTime;

    private void Start()
    {
        // Garante que o "E" começa desativado
        if (interactionUI != null)
            interactionUI.SetActive(false);
    }

    private void Update()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            // cooldown pra evitar bug de abrir e fechar rápido
            if (Time.time - lastInteractTime < interactCooldown)
                return;

            lastInteractTime = Time.time;

            if (shopManager == null || shopManager.shopPanel == null)
            {
                Debug.LogWarning("ShopManager ou ShopPanel não atribuído!");
                return;
            }

            // Alterna entre abrir e fechar loja
            if (shopManager.shopPanel.activeSelf)
            {
                shopManager.CloseShop();
                if (interactionUI != null)
                    interactionUI.SetActive(true); // reexibe o "E"
            }
            else
            {
                shopManager.OpenShop();
                if (interactionUI != null)
                    interactionUI.SetActive(false); // esconde o "E"
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            if (interactionUI != null && !shopManager.shopPanel.activeSelf)
                interactionUI.SetActive(true); // mostra o "E"
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            if (interactionUI != null)
                interactionUI.SetActive(false); // esconde o "E" ao sair

            // fecha a loja se estiver aberta
            if (shopManager != null && shopManager.shopPanel != null)
                shopManager.CloseShop();
        }
    }
}
