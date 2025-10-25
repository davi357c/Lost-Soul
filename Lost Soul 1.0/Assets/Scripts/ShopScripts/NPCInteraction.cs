using UnityEngine;


public class NPCInteraction : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    public float interactionRange = 2f;
    public Transform player;
    public bool playerNearby = false;


    void Update()
    {
        if (player == null) return;
        float d = Vector3.Distance(player.position, transform.position);
        playerNearby = d <= interactionRange;


        if (playerNearby && Input.GetKeyDown(interactKey))
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OpenShop();
            }
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}