using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("UI")]
    public GameObject shopPanel; // Painel da loja
    public TMP_Text coinsText;   // Texto que mostra as moedas
    public Transform contentParent; // Content do ScrollView
    public GameObject shopItemPrefab; // Prefab do item

    [Header("Itens da Loja")]
    public List<ItemData> storeItems = new List<ItemData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateCoinsUI();
        PopulateShop();
        shopPanel.SetActive(false);
    }

    public void OpenShop()
    {
        UpdateCoinsUI();
        shopPanel.SetActive(true);
        // opcional: Time.timeScale = 0;
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        // opcional: Time.timeScale = 1;
    }

    public void UpdateCoinsUI()
    {
        if (coinsText != null && CoinManager.Instance != null)
        {
            coinsText.text = "Moedas: " + CoinManager.Instance.totalCoins.ToString();
        }
    }

    void PopulateShop()
    {
        foreach (Transform t in contentParent)
            Destroy(t.gameObject);

        for (int i = 0; i < storeItems.Count; i++)
        {
            var data = storeItems[i];
            GameObject go = Instantiate(shopItemPrefab, contentParent);
            var ui = go.GetComponent<ShopItemUI>();
            if (ui != null)
                ui.Setup(data, i);
        }
    }

    public void TryBuy(int index)
    {
        if (index < 0 || index >= storeItems.Count) return;
        var data = storeItems[index];

        if (CoinManager.Instance.totalCoins >= data.price)
        {
            // Compra aprovada
            CoinManager.Instance.totalCoins -= data.price;
            PlayerPrefs.SetInt("TotalCoins", CoinManager.Instance.totalCoins);
            PlayerPrefs.Save();
            UpdateCoinsUI();

            Debug.Log("Comprou: " + data.itemName);
            // Aqui você pode dar o item ao jogador (ex: Inventory.AddItem(data))
        }
        else
        {
            Debug.Log("Sem moedas suficientes para " + data.itemName);
            // Pode mostrar um popup de aviso na UI
        }
    }
}
