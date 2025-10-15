using UnityEngine;

public class Spike : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth health = collision.GetComponent<PlayerHealth>();
            PlayerMovement movement = collision.GetComponent<PlayerMovement>();

            if (health != null)
                health.TakeDamage();

            if (movement != null)
                movement.Respawn(); // só o espinho faz o respawn
        }
    }
}
