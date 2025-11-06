using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class ShopManager : MonoBehaviour
{
    public GameObject shopPanel;
    public TextMeshProUGUI coinsText;
    public Transform contentParent;
    public GameObject shopItemPrefab;

    [Header("Interação")]
    public float interactionRange = 3f;          // Distância para interagir com a loja
    public GameObject interactionUI;             // Canvas com a letra "E"
    public KeyCode interactKey = KeyCode.E;      // Tecla usada para abrir/fechar a loja

    private Transform player;
    private bool playerInRange = false;
    private bool isShopOpen = false;

    [System.Serializable]
    public class ShopItem
    {
        public string name;
        public string description;
        public int price;
        public Sprite icon;
    }

    public List<ShopItem> items = new List<ShopItem>();

    private IEnumerator Start()
    {
        // Espera até o CoinManager realmente existir
        yield return new WaitUntil(() => CoinManager.Instance != null);

        UpdateCoinsUI();
        PopulateShop();
        shopPanel.SetActive(false);

        // Garante que o UI do "E" comece desativado
        if (interactionUI != null)
            interactionUI.SetActive(false);

        // Encontra o jogador
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);
        bool isNear = distance <= interactionRange;

        // Mostra/esconde o "E"
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

        // Interação com a loja
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            if (!isShopOpen)
                OpenShop();
            else
                CloseShop();
        }
    }

    private void UpdateCoinsUI()
    {
        coinsText.text = "Moedas: " + CoinManager.Instance.totalCoins;
    }

    private void PopulateShop()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (ShopItem item in items)
        {
            GameObject newItem = Instantiate(shopItemPrefab, contentParent);
            newItem.GetComponent<ShopItemUI>().Setup(item.icon, item.name, item.description, item.price);
        }
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        shopPanel.transform.SetAsLastSibling();
        UpdateCoinsUI();
        isShopOpen = true;

        // Esconde o "E" enquanto a loja está aberta
        if (interactionUI != null)
            interactionUI.SetActive(false);
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        isShopOpen = false;

        // Reaparece o "E" se o jogador ainda estiver por perto
        if (playerInRange && interactionUI != null)
            interactionUI.SetActive(true);
    }
}
