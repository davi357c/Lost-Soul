using UnityEngine;

public class PlayerFireball : MonoBehaviour
{
    [Header("Fireball")]
    public GameObject fireballPrefab;
    public Transform firePoint; // posição de onde sai a fireball (crie um empty na mão do player)

    [Header("Cooldown")]
    public float cooldown = 10f;
    private float lastFireTime = -Mathf.Infinity;

    [Header("Desbloqueio")]
    public bool fireballUnlocked = false; // 🔥 só pode atirar se tiver desbloqueado

    void Update()
    {
        if (!fireballUnlocked) return; // ❌ ainda não desbloqueou

        if (Input.GetKeyDown(KeyCode.R) && Time.time - lastFireTime >= cooldown)
        {
            Shoot();
            lastFireTime = Time.time;
        }
    }

    void Shoot()
    {
        GameObject fb = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);

        Vector2 direction = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        Fireball fireballComp = fb.GetComponent<Fireball>();
        if (fireballComp != null)
        {
            fireballComp.SetDirection(direction);
        }
        else
        {
            Rigidbody2D rb = fb.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = direction * 10f;
        }
    }

    // 🔓 Função chamada quando o item é coletado
    public void UnlockFireball()
    {
        fireballUnlocked = true;
        Debug.Log("🔥 Fireball desbloqueada!");
    }
}
