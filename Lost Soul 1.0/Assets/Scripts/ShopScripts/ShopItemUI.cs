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

    private void Start()
    {
        buyButton.onClick.AddListener(OnBuyPressed);
    }

    public void Setup(Sprite iconSprite, string name, string desc, int price)
    {
        icon.sprite = iconSprite;
        nameText.text = name;
        priceText.text = price.ToString() + " moedas";
        itemName = name;
        this.price = price;
    }

    public void OnBuyPressed()
    {
        if (CoinManager.Instance == null)
        {
            Debug.LogWarning("CoinManager não encontrado na cena! Comece pela primeira cena.");
            return;
        }

        if (CoinManager.Instance.totalCoins >= price)
        {
            CoinManager.Instance.totalCoins -= price;
            PlayerPrefs.SetInt("TotalCoins", CoinManager.Instance.totalCoins);
            PlayerPrefs.Save();

            CoinManager.Instance.UpdateUI();

            // 🔹 Atualiza o texto da loja se estiver aberta
            ShopManager shopManager = FindObjectOfType<ShopManager>();
            if (shopManager != null)
                shopManager.SendMessage("UpdateCoinsUI", SendMessageOptions.DontRequireReceiver);

            Debug.Log($"Comprou {itemName} por {price} moedas!");
        }
        else
        {
            Debug.Log("Moedas insuficientes!");
        }
    }


}
