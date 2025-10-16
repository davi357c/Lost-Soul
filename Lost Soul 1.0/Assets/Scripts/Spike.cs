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
            PlayerMovement movement = collision.GetComponent<PlayerMovement>();

            if (health != null)
                health.TakeDamage();

            if (movement != null)
                StartCoroutine(RespawnWithDelay(movement));
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
