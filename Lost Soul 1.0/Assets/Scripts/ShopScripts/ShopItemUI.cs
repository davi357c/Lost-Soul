using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    [HideInInspector] public int price;
    [HideInInspector] public string itemName;

    // novos campos para enviar ao inventário
    private Sprite itemSprite;
    private string itemDescription;

    private void Start()
    {
        buyButton.onClick.AddListener(OnBuyPressed);
    }

    // note que Setup já tinha sprite e desc, agora os guardamos
    public void Setup(Sprite iconSprite, string name, string desc, int price)
    {
        icon.sprite = iconSprite;
        nameText.text = name;
        priceText.text = price.ToString() + " moedas";
        itemName = name;
        this.price = price;

        // guardar para usar ao adicionar no inventário
        itemSprite = iconSprite;
        itemDescription = desc;
    }

    public void OnBuyPressed()
    {
        // checa se tem CoinManager
        if (CoinManager.Instance == null)
        {
            Debug.LogWarning("CoinManager não encontrado na cena! Comece pela primeira cena.");
            return;
        }

        // checa moedas antes de tentar adicionar no inventário
        if (CoinManager.Instance.totalCoins < price)
        {
            Debug.Log("Moedas insuficientes!");
            return;
        }

        // tenta adicionar ao inventário
        InventoryManager inventory = FindObjectOfType<InventoryManager>();
        if (inventory == null)
        {
            Debug.LogWarning("InventoryManager não encontrado na cena!");
            return;
        }

        int quantityToBuy = 1; // quantos comprar por clique (mude se quiser)
        int leftOver = inventory.AddItem(itemName, quantityToBuy, itemSprite, itemDescription);

        if (leftOver == quantityToBuy)
        {
            // inventário não aceitou o item (está cheio ou sem slot adequado)
            Debug.Log("Inventário cheio — compra cancelada.");
            return;
        }

        // compra bem-sucedida -> debita moedas e atualiza UI
        CoinManager.Instance.totalCoins -= price;
        PlayerPrefs.SetInt("TotalCoins", CoinManager.Instance.totalCoins);
        PlayerPrefs.Save();

        CoinManager.Instance.UpdateUI();

        // Atualiza o texto da loja se estiver aberta
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
            shopManager.SendMessage("UpdateCoinsUI", SendMessageOptions.DontRequireReceiver);

        Debug.Log($"Comprou {itemName} por {price} moedas!");
    }
}
