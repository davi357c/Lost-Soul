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
        if (statToChange == StatToChange.health)
        {
            PlayerHealth playerHealth = GameObject.Find("Player").GetComponent<PlayerHealth>();

            // Corrigido: agora usa as variáveis corretas
            if (playerHealth.currentLives == playerHealth.maxLives)
            {
                return false; // já está com vida cheia
            }
            else
            {
                playerHealth.ChangeHealth(amountToChangeStat);
                return true; // item foi usado
            }
        }

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
