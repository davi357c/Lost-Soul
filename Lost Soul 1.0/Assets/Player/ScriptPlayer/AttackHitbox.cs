using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public int damage = 1;
    public float knockbackForce = 5f;
    public Vector2 knockbackDirection = Vector2.right;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(knockbackDirection.normalized, damage);
            return;
        }

        FlyEnemyHealth flyEnemy = collision.GetComponent<FlyEnemyHealth>();
        if (flyEnemy != null)
        {
            flyEnemy.TakeDamage(knockbackDirection.normalized, damage);
        }
    }

}
