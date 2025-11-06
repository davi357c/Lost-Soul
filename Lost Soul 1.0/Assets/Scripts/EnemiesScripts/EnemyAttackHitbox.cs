using UnityEngine;

public class EnemyAttackHitbox : MonoBehaviour
{
    public int damage = 1;
    public float knockbackForce = 4f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[EnemyAttackHitbox] Player atingido por {gameObject.name}");

            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector2 hitDir = (other.transform.position - transform.position).normalized;
                playerHealth.TakeDamage(damage, hitDir * knockbackForce);
                Debug.Log($"[EnemyAttackHitbox] Dano {damage} aplicado ao Player!");
            }
            else
            {
                Debug.LogWarning("[EnemyAttackHitbox] PlayerHealth não encontrado no Player!");
            }
        }
    }
}
