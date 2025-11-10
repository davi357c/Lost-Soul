using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Dano / Knockback")]
    public int damage = 1;
    public float knockbackForce = 5f;
    public Vector2 knockbackDirection = Vector2.right;

    [Header("Pogo (opcional)")]
    [Tooltip("Marque APENAS na hitbox do ataque para baixo.")]
    public bool enablePogoOnHit = false;

    private PlayerMovement player;

    private void Awake()
    {
        // pega o PlayerMovement no pai (player)
        player = GetComponentInParent<PlayerMovement>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        bool hitEnemy = false;
        Transform enemyTransform = collision.transform;

        // Inimigo terrestre comum
        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(knockbackDirection.normalized, damage);
            hitEnemy = true;
        }
        else
        {
            // Inimigo voador
            FlyEnemyHealth flyEnemy = collision.GetComponent<FlyEnemyHealth>();
            if (flyEnemy != null)
            {
                flyEnemy.TakeDamage(knockbackDirection.normalized, damage);
                hitEnemy = true;
            }
            else
            {
                // Inimigo veloz
                VeloEnemyHealth veloEnemy = collision.GetComponent<VeloEnemyHealth>();
                if (veloEnemy != null)
                {
                    veloEnemy.TakeDamage(knockbackDirection.normalized, damage);
                    hitEnemy = true;
                }
            }
        }

        if (!hitEnemy) return;

        // Se não for ataque pra baixo, não tenta pogo
        if (!enablePogoOnHit) return;
        if (player == null) return;

        // Faz pogo
        player.OnDownAttackHitEnemy(enemyTransform);
    }



}
