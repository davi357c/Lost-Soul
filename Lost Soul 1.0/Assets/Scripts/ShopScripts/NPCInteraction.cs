using UnityEngine;

public class NpcInteraction : MonoBehaviour
{
    public ShopManager shopManager;
    private bool playerNearby;
    private float lastInteractTime;
    private float interactCooldown = 0.3f; // tempo mínimo entre apertos

    private void Update()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Tentando abrir loja. ShopManager: " + shopManager);
            Debug.Log("Painel: " + (shopManager != null ? shopManager.shopPanel : null));

            if (shopManager == null || shopManager.shopPanel == null)
            {
                Debug.LogWarning("ShopManager ou ShopPanel não atribuído!");
                return;
            }

            if (shopManager.shopPanel.activeSelf)
                shopManager.CloseShop();
            else
                shopManager.OpenShop();
        }
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            if (shopManager != null && shopManager.shopPanel != null)
                shopManager.CloseShop();
        }
    }
    
}
