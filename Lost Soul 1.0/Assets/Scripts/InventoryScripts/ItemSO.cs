using UnityEngine;
using static ItemSO;

[CreateAssetMenu]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public StatToChange statToChange = new StatToChange();
    public int amountToChangeStat;

    public AttributesToChange attributesToChange = new AttributesToChange();
    public int amountToChangeAtribbute;

    public bool UseItem()
    {
        // tenta o singleton primeiro (se você aplicou a mudança acima)
        PlayerHealth playerHealth = PlayerHealth.Instance;

        // se não houver singleton (ou não foi aplicado), tenta achar na cena
        if (playerHealth == null)
        {
            playerHealth = GameObject.FindObjectOfType<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogWarning($"[ItemSO] Não encontrou PlayerHealth ao usar {itemName}.");
            return false;
        }

        Debug.Log($"[ItemSO] Tentando usar {itemName} em Player (vidas antes = {playerHealth.currentLives}).");

        if (statToChange == StatToChange.health)
        {
            if (playerHealth.currentLives >= playerHealth.maxLives)
            {
                Debug.Log($"[ItemSO] Jogador já com vida cheia ({playerHealth.currentLives}/{playerHealth.maxLives}).");
                return false; // não usar se vida cheia
            }
            playerHealth.ChangeHealth(amountToChangeStat);
            Debug.Log($"[ItemSO] Item usado. Vidas agora = {playerHealth.currentLives}.");
            return true;
        }

        // outros tipos de stat aqui...
        return false;
    }


    public enum StatToChange
    {
        none,
        health,
        mana,
        stamina
    };

    public enum AttributesToChange
    {
        none,
        defense,
        intelligence,
        agility
    };
}
