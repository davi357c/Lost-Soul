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

    // Dentro do ShopManager
    public void OpenShop()
    {
        shopPanel.SetActive(true);
        shopPanel.transform.SetAsLastSibling();
        UpdateCoinsUI();
        // Time.timeScale = 0f; // teste: desativa isso
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        // Time.timeScale = 1f; // teste: desativa isso
    }


}
