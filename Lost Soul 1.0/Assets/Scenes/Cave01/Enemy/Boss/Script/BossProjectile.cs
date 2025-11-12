using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [Header("Configuração")]
    public float speed = 10f;      // velocidade do projétil
    public int damage = 1;         // dano que ele causa
    public float lifeTime = 4f;    // tempo até desaparecer sozinho

    private Vector2 direction = Vector2.right;

    // Chamado pelo boss logo após instanciar o projétil (opcional)
    public void Init(Vector2 dir, float newSpeed, int newDamage, float newLifeTime)
    {
        if (dir.sqrMagnitude > 0.0001f)
            direction = dir.normalized;

        speed = newSpeed;
        damage = newDamage;
        lifeTime = newLifeTime;

        // Rotaciona o sprite para a direção do disparo
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void Start()
    {
        // Garante que o projétil não fique pra sempre na cena
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Acertou o player
        if (collision.CompareTag("Player"))
        {
            PlayerHealth hp = collision.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                // direção projétil -> player (pro knockback ir pro lado certo)
                Vector2 hitDirection = (collision.transform.position - transform.position).normalized;

                // Usa seu método: TakeDamage(int damage, Vector2 hitDirection)
                hp.TakeDamage(damage, hitDirection);
            }

            Destroy(gameObject);
            return;
        }

        // Bateu em chão/parede/etc
        if (collision.CompareTag("Ground") || collision.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
