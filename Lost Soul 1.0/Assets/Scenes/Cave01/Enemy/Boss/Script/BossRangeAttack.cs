using UnityEngine;

public class BossRangeAttack : MonoBehaviour
{
    [Header("Refer�ncias")]
    [Tooltip("Ponto de origem do disparo (objeto RangePoint na hierarquia).")]
    public Transform rangePoint;

    [Tooltip("Transform do Player. Se deixar vazio, procura pela Tag.")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Proj�til")]
    [Tooltip("Prefab do proj�til que ser� instanciado.")]
    public GameObject projectilePrefab;

    [Tooltip("Velocidade do proj�til.")]
    public float projectileSpeed = 12f;

    [Tooltip("Dano que o proj�til vai causar no Player.")]
    public int projectileDamage = 1;

    [Tooltip("Tempo para destruir o proj�til (se o script dele n�o tratar isso).")]
    public float projectileLifetime = 4f;

    [Header("Debug")]
    public bool logDebug = true;

    private void Awake()
    {
        // Acha Player pela Tag, se n�o tiver sido arrastado no Inspector
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
                player = p.transform;
        }

        // Tenta achar o RangePoint autom�tico se o campo estiver vazio
        if (rangePoint == null)
        {
            Transform found = transform.Find("RangePoint");
            if (found != null)
                rangePoint = found;
        }
    }

    // =================================
    //  FUN��O PARA ANIMATION EVENT
    // =================================
    public void SpawnProjectile()
    {
        if (rangePoint == null || projectilePrefab == null)
        {
            if (logDebug)
                Debug.LogWarning("[BossRangeAttack] SpawnProjectile -> rangePoint ou projectilePrefab n�o atribu�do.");
            return;
        }

        // Posi��o de spawn
        Vector3 spawnPos = rangePoint.position;

        // Dire��o em dire��o ao player
        Vector2 dir;
        if (player != null)
        {
            dir = (player.position - spawnPos);
        }
        else
        {
            // Se n�o tiver player, atira pra frente com base no scale X do boss
            float xDir = transform.localScale.x >= 0 ? 1f : -1f;
            dir = new Vector2(xDir, 0f);
        }

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;

        dir = dir.normalized;

        // Instancia o proj�til
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // Primeiro tenta usar um script pr�prio de proj�til
        BossProjectile bp = proj.GetComponent<BossProjectile>();
        if (bp != null)
        {
            bp.Init(dir, projectileSpeed, projectileDamage, projectileLifetime);
        }
        else
        {
            // Fallback: movimenta pelo Rigidbody2D se n�o tiver BossProjectile
            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = dir * projectileSpeed;
            }

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            proj.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            Destroy(proj, projectileLifetime);
        }

        if (logDebug)
            Debug.Log("[BossRangeAttack] SpawnProjectile -> Disparou proj�til em dire��o ao player.");
    }
}
