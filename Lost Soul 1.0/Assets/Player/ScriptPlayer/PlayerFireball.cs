using UnityEngine;

public class PlayerFireball : MonoBehaviour
{
    [Header("Fireball")]
    public GameObject fireballPrefab;
    public Transform firePoint; // posi��o de onde sai a fireball (crie um empty na m�o do player)

    [Header("Cooldown")]
    public float cooldown = 10f;
    private float lastFireTime = -Mathf.Infinity;

    void Update()
    {
        // aperta R
        if (Input.GetKeyDown(KeyCode.R) && Time.time - lastFireTime >= cooldown)
        {
            Shoot();
            lastFireTime = Time.time;
        }
    }

    void Shoot()
    {
        // instancia a fireball na posi��o do firePoint
        GameObject fb = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);

        // tenta configurar dire��o pela escala X do player (flip)
        Vector2 direction = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        Fireball fireballComp = fb.GetComponent<Fireball>();
        if (fireballComp != null)
        {
            fireballComp.SetDirection(direction);
        }
        else
        {
            // fallback: aplica velocidade direto no Rigidbody2D se n�o tiver o componente Fireball
            Rigidbody2D rb = fb.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = direction * 10f;
        }
    }
}
