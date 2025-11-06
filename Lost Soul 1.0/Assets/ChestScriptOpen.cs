using UnityEngine;

public class ChestScriptOpen : MonoBehaviour
{
    public float interactDistance = 2f;
    public KeyCode interactKey = KeyCode.E;

    private Animator animator;
    private Transform player;
    private bool isOpen = false;

    [Header("Moedas")]
    public GameObject coinPrefab;
    public int coinsToSpawn = 10;
    public float spawnRadius = 0.5f;
    public Vector2 forceX = new Vector2(-2f, 2f);
    public Vector2 forceY = new Vector2(4f, 7f);

    [Header("UI de Interação")]
    public GameObject interactionUI; // Canvas com a letra "E"

    void Start()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Garante que o UI comece desativado
        if (interactionUI != null)
            interactionUI.SetActive(false);
    }

    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        // Mostra/esconde o UI de interação
        if (!isOpen && interactionUI != null)
        {
            if (distance <= interactDistance)
                interactionUI.SetActive(true);
            else
                interactionUI.SetActive(false);
        }

        // Interação com o baú
        if (distance <= interactDistance && Input.GetKeyDown(interactKey) && !isOpen)
        {
            animator.SetTrigger("Open");
            isOpen = true;
            if (interactionUI != null)
                interactionUI.SetActive(false); // Esconde o "E" depois de abrir
            SpawnCoins();
        }
    }

    void SpawnCoins()
    {
        for (int i = 0; i < coinsToSpawn; i++)
        {
            Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
            GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);

            Rigidbody2D rb = coin.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 force = new Vector2(Random.Range(forceX.x, forceX.y), Random.Range(forceY.x, forceY.y));
                rb.AddForce(force, ForceMode2D.Impulse);
            }
        }
    }
}
