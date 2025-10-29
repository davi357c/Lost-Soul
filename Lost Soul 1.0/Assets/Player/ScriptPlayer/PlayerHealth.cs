using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vidas")]
    public int maxLives = 5;
    public int currentLives = -1; // inicia com -1 pra sabermos se já foi inicializado

    [Header("UI de Corações")]
    public Animator[] hearts;

    [Header("Opcional: Prefab do Hearts (UI)")]
    [Tooltip("Se deixado vazio, PlayerHealth tentará encontrar os animators na cena. Se preenchido, será instanciado apenas quando necessário.")]
    public GameObject heartsPrefab;

    [Header("Configurações de Dano")]
    public float invulnerableTime = 1f;
    public float knockbackForce = 5f;

    private bool isInvulnerable = false;
    private bool isDead = false;

    private Animator animator;
    private Rigidbody2D rb;
    private PlayerMovement movement;

    // Singleton público para acesso seguro
    private static PlayerHealth instance;
    public static PlayerHealth Instance => instance;

    void Awake()
    {
        // singleton robusto
        if (instance != null && instance != this)
        {
            Debug.Log("[PlayerHealth] Outro PlayerHealth encontrado - destruindo este.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (currentLives <= 0)
            currentLives = maxLives;

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();

        // tenta localizar os animators de coração de forma tolerante
        StartCoroutine(LocateHeartsAndUpdate());
        Debug.Log($"[PlayerHealth] Start - currentLives={currentLives}, heartsArrayLength={hearts?.Length ?? 0}");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[PlayerHealth] Cena carregada: " + scene.name);
        // ao carregar cena, tenta localizar/atualizar (coroutine cuida da ordem de inicialização)
        StartCoroutine(LocateHeartsAndUpdate());
    }

    // Coroutine que tenta localizar os animators de hearts de várias maneiras (e atualiza a UI quando encontrado)
    private IEnumerator LocateHeartsAndUpdate()
    {
        // se já tem referência válida, só atualiza
        if (hearts != null && hearts.Length > 0)
        {
            UpdateHeartsUI();
            yield break;
        }

        float timeout = 1.0f; // tempo máximo para tentar encontrar (em segundos)
        float timer = 0f;

        // 1) tentativa rápida por GameObject nomeado "ObjectAleatorio" (você já usou esse)
        GameObject obj = GameObject.Find("ObjectAleatorio");
        if (obj != null)
        {
            hearts = obj.GetComponentsInChildren<Animator>(true);
            Debug.Log($"[PlayerHealth] Encontrou ObjectAleatorio. Hearts count = {hearts.Length}");
            UpdateHeartsUI();
            yield break;
        }

        // 2) tenta procurar nos root GameObjects da cena um conjunto de animators que cai bem (prefere igual a maxLives)
        while ((hearts == null || hearts.Length == 0) && timer < timeout)
        {
            Animator[] best = null;
            int bestCount = 0;

            var currentScene = SceneManager.GetActiveScene();
            var roots = currentScene.GetRootGameObjects();

            foreach (var root in roots)
            {
                var anims = root.GetComponentsInChildren<Animator>(true);
                if (anims == null || anims.Length == 0) continue;

                // preferir conjunto com contagem igual a maxLives
                if (anims.Length == maxLives)
                {
                    best = anims;
                    bestCount = anims.Length;
                    break;
                }

                if (anims.Length > bestCount)
                {
                    best = anims;
                    bestCount = anims.Length;
                }
            }

            if (best != null && best.Length > 0)
            {
                hearts = best;
                Debug.Log($"[PlayerHealth] Hearts encontrados em root object. Hearts count = {hearts.Length}");
                UpdateHeartsUI();
                yield break;
            }

            // 3) tenta encontrar por nome igual ao prefab (se prefab foi informado e já existe na cena)
            if (heartsPrefab != null)
            {
                GameObject existing = GameObject.Find(heartsPrefab.name);
                if (existing != null)
                {
                    hearts = existing.GetComponentsInChildren<Animator>(true);
                    Debug.Log($"[PlayerHealth] Encontrou objeto com nome do prefab ({heartsPrefab.name}). Hearts count = {hearts.Length}");
                    UpdateHeartsUI();
                    yield break;
                }
            }

            // espera um frame e tenta novamente (ajuda com ordem de Start/Awake)
            yield return null;
            timer += Time.deltaTime;
        }

        // 4) se ainda não achou e tiver prefab, instancia (apenas se não houver outro com mesmo nome)
        if ((hearts == null || hearts.Length == 0) && heartsPrefab != null)
        {
            if (GameObject.Find(heartsPrefab.name) == null)
            {
                GameObject heartsObj = Instantiate(heartsPrefab);
                heartsObj.name = heartsPrefab.name;
                DontDestroyOnLoad(heartsObj);

                hearts = heartsObj.GetComponentsInChildren<Animator>(true);
                Debug.Log($"[PlayerHealth] Hearts prefab instanciado. Hearts count = {hearts.Length}");
                UpdateHeartsUI();
                yield break;
            }
            else
            {
                GameObject existing = GameObject.Find(heartsPrefab.name);
                if (existing != null)
                {
                    hearts = existing.GetComponentsInChildren<Animator>(true);
                    Debug.Log($"[PlayerHealth] Hearts encontrado por nome (existing). Hearts count = {hearts.Length}");
                    UpdateHeartsUI();
                    yield break;
                }
            }
        }

        // 5) fallback: tentar qualquer Animator na cena (se nada melhor foi encontrado)
        if (hearts == null || hearts.Length == 0)
        {
            var all = FindObjectsOfType<Animator>(true);
            if (all != null && all.Length > 0)
            {
                // agrupa por parent root e escolhe o maior conjunto como heurística
                Animator[] best = null;
                int bestCount = 0;
                foreach (var a in all)
                {
                    var parentRoot = a.transform.root;
                    var anims = parentRoot.GetComponentsInChildren<Animator>(true);
                    if (anims.Length > bestCount)
                    {
                        best = anims;
                        bestCount = anims.Length;
                    }
                }

                if (best != null && best.Length > 0)
                {
                    hearts = best;
                    Debug.Log($"[PlayerHealth] Fallback: conjunto de animators escolhido. Hearts count = {hearts.Length}");
                    UpdateHeartsUI();
                    yield break;
                }
            }
        }

        // se chegou aqui, não encontrou nada — UpdateHeartsUI fará warning caso chamado em outro momento
        Debug.LogWarning("[PlayerHealth] Não conseguiu localizar hearts automaticamente. Configure hearts manualmente no Inspector ou forneça heartsPrefab.");
        yield break;
    }

    public void TakeDamage(Vector2 hitDirection)
    {
        if (isInvulnerable || isDead) return;

        currentLives--;
        UpdateHeartsUI();

        if (currentLives <= 0)
        {
            Die();
            return;
        }

        if (animator != null) animator.SetTrigger("Hit");
        StartCoroutine(InvulnerabilityRoutine());

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(hitDirection.x * knockbackForce, knockbackForce), ForceMode2D.Impulse);
        }

        if (movement != null) movement.Respawn(0.3f);
    }

    void UpdateHeartsUI()
    {
        if (hearts == null || hearts.Length == 0)
        {
            Debug.LogWarning("[PlayerHealth] UpdateHeartsUI: hearts array vazio.");
            return;
        }

        // ativa/desativa corações conforme currentLives
        for (int i = 0; i < hearts.Length; i++)
        {
            bool isAlive = i < currentLives;
            if (hearts[i] != null && hearts[i].gameObject != null)
                hearts[i].gameObject.SetActive(isAlive);
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null) animator.SetTrigger("Die");
        if (movement != null) movement.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float blinkInterval = 0.1f;
        float timer = 0f;
        while (timer < invulnerableTime)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }
        if (sr != null) sr.enabled = true;
        isInvulnerable = false;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ChangeHealth(int amount)
    {
        if (isDead) return;

        currentLives += amount;
        currentLives = Mathf.Clamp(currentLives, 0, maxLives);

        UpdateHeartsUI();

        if (currentLives <= 0)
        {
            Die();
        }
    }
}
