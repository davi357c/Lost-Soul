using UnityEngine;

public class Arrow : MonoBehaviour
{
    public int damage = 1;
    public float lifeTime = 4f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = PlayerHealth.Instance != null ? PlayerHealth.Instance : other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                // calcula direção do impacto (da flecha até o player)
                Vector2 hitDir = (other.transform.position - transform.position).normalized;

                // aplica o dano
                ph.TakeDamage(damage, hitDir);
            }
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
