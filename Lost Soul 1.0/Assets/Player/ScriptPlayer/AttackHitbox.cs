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
        Transform targetTransform = collision.transform;

        // 1) BOSS – procura BossHealth no objeto OU nos pais
        BossHealth boss = collision.GetComponentInParent<BossHealth>();
        if (boss != null)
        {
            boss.TakeDamage(knockbackDirection.normalized, damage);
            targetTransform = boss.transform;
            hitEnemy = true;
        }
        else
        {
            // 2) INIMIGO terrestre comum
            EnemyHealth enemy = collision.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(knockbackDirection.normalized, damage);
                targetTransform = enemy.transform;
                hitEnemy = true;
            }
            else
            {
                // 3) INIMIGO voador
                FlyEnemyHealth flyEnemy = collision.GetComponentInParent<FlyEnemyHealth>();
                if (flyEnemy != null)
                {
                    flyEnemy.TakeDamage(knockbackDirection.normalized, damage);
                    targetTransform = flyEnemy.transform;
                    hitEnemy = true;
                }
                else
                {
                    // 4) INIMIGO veloz
                    VeloEnemyHealth veloEnemy = collision.GetComponentInParent<VeloEnemyHealth>();
                    if (veloEnemy != null)
                    {
                        veloEnemy.TakeDamage(knockbackDirection.normalized, damage);
                        targetTransform = veloEnemy.transform;
                        hitEnemy = true;
                    }
                }
            }
        }

        if (!hitEnemy) return;

        // Se não for ataque pra baixo, não tenta pogo
        if (!enablePogoOnHit) return;
        if (player == null) return;

        // Faz pogo no alvo acertado (boss ou inimigo)
        player.OnDownAttackHitEnemy(targetTransform);
    }
}
