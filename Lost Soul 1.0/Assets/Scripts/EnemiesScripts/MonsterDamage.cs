using UnityEngine;

public class MonsterDamage : MonoBehaviour
{
    public int damage = 1;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector2 hitDirection = (collision.transform.position - transform.position).normalized;

                playerHealth.TakeDamage(damage, hitDirection);
            }
        }
    }
}
