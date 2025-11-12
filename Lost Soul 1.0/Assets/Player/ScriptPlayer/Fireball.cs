using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Configurações")]
    public float speed = 10f;
    public float lifetime = 3f;
    public int damage = 1;

    [Header("Layers de colisão")]
    public LayerMask groundLayers;
    public LayerMask enemyLayer;

    private Vector2 direction = Vector2.right;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearVelocity = direction * speed;

        Destroy(gameObject, lifetime);
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (direction.x < 0 ? -1 : 1);
        transform.localScale = scale;

        if (rb != null)
            rb.linearVelocity = direction * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayer = 1 << other.gameObject.layer;

        // debug pra testar
        Debug.Log($"Fireball colidiu com {other.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");

        // inimigo (inclui boss, desde que esteja em uma das layers do enemyLayer)
        if ((enemyLayer.value & otherLayer) != 0)
        {
            Debug.Log("Fireball atingiu inimigo/boss!");

            Vector2 hitDir = (other.transform.position - transform.position).normalized;

            // 1º tenta BossHealth
            BossHealth boss = other.GetComponentInParent<BossHealth>();
            if (boss != null)
            {
                boss.TakeDamage(hitDir, damage);
            }
            else
            {
                // se não for boss, tenta EnemyHealth normal
                EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
                if (enemy != null)
                {
                    enemy.TakeDamage(hitDir, damage);
                }
            }

            Destroy(gameObject);
            return;
        }

        // chão / obstáculo
        if ((groundLayers.value & otherLayer) != 0)
        {
            Debug.Log("Fireball atingiu chão/obstáculo!");
            Destroy(gameObject);
        }
    }
}
