using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public float lifetime = 5f; // tempo até sumir se não pegar
    private bool canBeCollected = false; // espera um tempinho antes de coletar
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            // garante que a moeda pode cair normalmente
            rb.gravityScale = 1f;
        }
        else
        {
            Debug.LogWarning("Coin: Rigidbody2D não encontrado no prefab da moeda.");
        }

        // destrói a moeda depois de um tempo
        Destroy(gameObject, lifetime);

        // pequena espera pra não coletar no instante que nasce
        StartCoroutine(EnableCollectAfterDelay(0.3f));
    }

    private IEnumerator EnableCollectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canBeCollected = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (canBeCollected && collision.CompareTag("Player"))
        {
            if (CoinManager.Instance != null)
                CoinManager.Instance.AddCoin(1);

            Destroy(gameObject);
        }
    }


}
