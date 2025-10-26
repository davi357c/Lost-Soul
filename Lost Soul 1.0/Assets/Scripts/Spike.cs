using UnityEngine;
using System.Collections;

public class Spike : MonoBehaviour
{
    private Collider2D spikeCollider;

    private void Awake()
    {
        spikeCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth health = collision.GetComponent<PlayerHealth>();

            if (health != null)
            {
                // Calcula direção do knockback: da spike pro player
                Vector2 hitDir = (collision.transform.position - transform.position).normalized;

                // Aplica dano + knockback + respawn com delay
                health.TakeDamage(hitDir);
            }
        }
    }



    private IEnumerator RespawnWithDelay(PlayerMovement movement)
    {
        // desativa o movimento do player
        movement.enabled = false;

        // espera mini delay antes de respawn (0.2s)
        yield return new WaitForSeconds(0.2f);

        movement.Respawn();
        movement.enabled = true;
    }
}
