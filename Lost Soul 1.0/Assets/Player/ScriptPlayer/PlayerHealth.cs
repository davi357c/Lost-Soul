using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using System.IO;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vidas")]
    public int maxLives = 5;
    public int currentLives = -1; // inicia com -1 pra sabermos se já foi inicializado

    [Header("UI de Corações")]
    public Animator[] hearts;

    [Header("Opcional: Prefab do Hearts (UI)")]
    [Tooltip("Se deixado vazio, PlayerHealth tentará encontrar os animators de coração na cena. Se preenchido, será instanciado apenas quando necessário.")]
    public GameObject heartsPrefab;

    [Header("Configurações de Dano")]
    public float invulnerableTime = 1f;
    public float knockbackForce = 5f;

    private bool isInvulnerable = false;
    private bool isDead = false;
    public bool IsDead => isDead;

    private Animator animator;
    private Rigidbody2D rb;
    private PlayerMovement movement;

    // Singleton público para acesso seguro
    private static PlayerHealth instance;
    public static PlayerHealth Instance => instance;

    // --- FADE / RESPAWN ---

    [Header("Fade ao morrer (opcional)")]
    [Tooltip("Coloque aqui uma Image preta que cubra toda a tela, idealmente uma Image chamada 'FadeImage' ou a maior Image na cena.")]
    public Image fadeImage;               // pode ser atribuído no Inspector (opcional)
    public float fadeDuration = 3f;       // duração do fade para preto antes do respawn
    public float fadeInAfterRespawn = 0.5f; // tempo para desvanecer após respawn

    [Header("Respawn em relação ao checkpoint")]
    [Tooltip("Quanto o player volta para trás no eixo X ao respawnar no checkpoint, na direção contrária de onde morreu.")]
    public float respawnBackOffsetX = 1f;

    // fade persistente compartilhado entre cenas (evita duplicatas)
    private static Image persistentFadeImage = null;
    private const string defaultFadeObjectName = "FadeImage";
    // -------------------------------------------------------

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

        // Se já houver uma fade persistente, utilize-a
        if (persistentFadeImage != null)
        {
            fadeImage = persistentFadeImage;
        }
        else if (fadeImage != null)
        {
            // Se foi atribuído no Inspector, torne persistente
            MakeFadePersistent(fadeImage);
        }
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

        // Tenta garantir que exista uma fadeImage persistente e sem duplicatas
        HandleFadeAcrossScenes();

        // ao carregar cena, tenta localizar/atualizar (coroutine cuida da ordem de inicialização)
        StartCoroutine(LocateHeartsAndUpdate());
    }

    private void HandleFadeAcrossScenes()
    {
        // se já temos persistente, mas a cena trouxe um novo objeto de mesmo nome, destrua o novo
        if (persistentFadeImage != null)
        {
            // procura na cena por objeto com o mesmo nome
            GameObject found = GameObject.Find(defaultFadeObjectName);
            if (found != null)
            {
                Image img = found.GetComponent<Image>();
                if (img != null && img != persistentFadeImage)
                {
                    Debug.Log("[PlayerHealth] Encontrada FadeImage extra na cena enquanto já existe uma persistente. Destruindo a nova para evitar duplicata.");
                    Destroy(found);
                }
            }

            // assegura referência local aponta pra persistente
            fadeImage = persistentFadeImage;
            return;
        }

        // se não existe persistente, tente achar na cena uma imagem de fade
        // 1) por nome
        GameObject byName = GameObject.Find(defaultFadeObjectName);
        if (byName != null)
        {
            Image img = byName.GetComponent<Image>();
            if (img != null)
            {
                MakeFadePersistent(img);
                fadeImage = persistentFadeImage;
                Debug.Log("[PlayerHealth] FadeImage encontrada por nome e marcada como persistente.");
                return;
            }
        }

        // 2) tenta achar a maior Image disponível (heurística para fullscreen)
        Image candidate = FindBestFullscreenImageInScene();
        if (candidate != null)
        {
            MakeFadePersistent(candidate);
            fadeImage = persistentFadeImage;
            Debug.Log("[PlayerHealth] Nenhuma FadeImage nomeada; encontrada maior Image e marcada como persistente.");
            return;
        }

        // se não encontrou nada, permanece sem fade (comportamento antigo)
        Debug.Log("[PlayerHealth] Nenhuma Image para fade encontrada nesta cena.");
    }

    // marca a image como persistente e guarda referência estática
    private void MakeFadePersistent(Image img)
    {
        if (img == null) return;
        var root = img.transform.root.gameObject;
        DontDestroyOnLoad(root);
        persistentFadeImage = img;
        fadeImage = img;
    }

    // heurística: escolhe a Image com maior area rectTransform (se houver várias)
    private Image FindBestFullscreenImageInScene()
    {
        Image[] all = FindObjectsOfType<Image>(true);
        if (all == null || all.Length == 0) return null;

        Image best = null;
        float bestArea = 0f;

        foreach (var img in all)
        {
            RectTransform rt = img.rectTransform;
            if (rt == null) continue;
            // calcula área aproximada (em unidades locais)
            float w = Mathf.Abs(rt.rect.width);
            float h = Mathf.Abs(rt.rect.height);
            float area = w * h;
            if (area > bestArea)
            {
                bestArea = area;
                best = img;
            }
        }

        return best;
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

        // 1) tentativa rápida por GameObject nomeado "ObjectAleatorio"
        GameObject obj = GameObject.Find("ObjectAleatorio");
        if (obj != null)
        {
            hearts = obj.GetComponentsInChildren<Animator>(true);
            Debug.Log($"[PlayerHealth] Encontrou ObjectAleatorio. Hearts count = {hearts.Length}");
            UpdateHeartsUI();
            yield break;
        }

        // 2) tenta procurar nos root GameObjects da cena um conjunto de animators que caia bem (prefere igual a maxLives)
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
                hearts = heartsObj.GetComponentsInChildren<Animator>(true);
                Debug.Log($"[PlayerHealth] Hearts instanciados a partir do prefab ({heartsPrefab.name}). Hearts count = {hearts.Length}");
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
                        bestCount = anims.Length;
                        best = anims;
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
        Debug.LogWarning("[PlayerHealth] Não conseguiu localizar hearts automaticamente. Configure hearts manualmente no Inspector.");
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
        StartCoroutine(RespawnAfterDeath());
    }

    private IEnumerator RespawnAfterDeath()
    {
        // posição em que o player morreu (antes de qualquer teleporte)
        Vector2 deathPosition = transform.position;

        // Debug inicial: mostra o que está salvo
        Debug.Log(
            $"[PlayerHealth] Iniciando RespawnAfterDeath(). " +
            $"Scene='{PlayerPrefs.GetString("LastCheckpointScene", "<vazio>")}', " +
            $"ID='{PlayerPrefs.GetString("LastCheckpointID", "<vazio>")}', " +
            $"X={PlayerPrefs.GetFloat("LastCheckpointX", float.NaN)}, " +
            $"Y={PlayerPrefs.GetFloat("LastCheckpointY", float.NaN)}"
        );

        // 1) Fade ou espera
        if (fadeImage == null)
        {
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Clamp01(t / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
            c.a = 1f;
            fadeImage.color = c;
        }

        // 2) Pega dados salvos
        string targetScene = PlayerPrefs.GetString("LastCheckpointScene", SceneManager.GetActiveScene().name);
        string savedID = PlayerPrefs.GetString("LastCheckpointID", "");
        float savedX = PlayerPrefs.GetFloat("LastCheckpointX", transform.position.x);
        float savedY = PlayerPrefs.GetFloat("LastCheckpointY", transform.position.y);
        Vector2 fallbackPos = new Vector2(savedX, savedY);

        // 3) Carrega cena se existir no Build Settings
        bool sceneLoaded = false;
        if (!string.IsNullOrEmpty(targetScene) && SceneManager.GetActiveScene().name != targetScene)
        {
            int buildIndex = GetBuildIndexByName(targetScene);
            if (buildIndex == -1)
            {
                Debug.LogWarning($"[PlayerHealth] Cena '{targetScene}' não encontrada no Build Settings. Irei usar a cena atual e coords salvas como fallback {fallbackPos}.");
            }
            else
            {
                Debug.Log($"[PlayerHealth] Carregando cena '{targetScene}' (buildIndex {buildIndex})...");
                if (rb != null) rb.simulated = false; // evita fisgações

                var op = SceneManager.LoadSceneAsync(buildIndex);
                if (op == null)
                {
                    Debug.LogWarning("[PlayerHealth] LoadSceneAsync retornou null.");
                }
                else
                {
                    while (!op.isDone)
                        yield return null;
                    // deixa a cena rodar Start/Awake
                    yield return null;
                    sceneLoaded = true;
                    Debug.Log("[PlayerHealth] Cena carregada com sucesso.");
                }
            }
        }

        // 4) Garante que só o Player persistente fique na cena
        GameObject scenePlayer = null;
        try
        {
            scenePlayer = GameObject.FindWithTag("Player");
        }
        catch
        {
            scenePlayer = null;
        }

        if (scenePlayer != null && scenePlayer != this.gameObject)
        {
            Debug.Log("[PlayerHealth] Encontrado Player de cena diferente do persistente. Vou destruir o player da cena e usar o persistente.");
            Destroy(scenePlayer);
            // aguarda um frame pra garantir destruição
            yield return null;
        }

        // 5) Tenta achar o checkpoint por ID
        Vector2 targetPos = fallbackPos;
        bool foundByID = false;

        if (!string.IsNullOrEmpty(savedID))
        {
            float timeout = 2f;
            float elapsed = 0f;
            Checkpoint foundCp = null;

            while (elapsed < timeout && foundCp == null)
            {
                Checkpoint[] allCp = null;
                try
                {
                    allCp = FindObjectsOfType<Checkpoint>();
                }
                catch
                {
                    allCp = FindObjectsOfType<Checkpoint>();
                }

                if (allCp != null)
                {
                    foreach (var cp in allCp)
                    {
                        if (cp != null && cp.checkpointID == savedID)
                        {
                            foundCp = cp;
                            break;
                        }
                    }
                }

                if (foundCp != null) break;

                yield return null;
                elapsed += Time.deltaTime;
            }

            if (foundCp != null)
            {
                targetPos = foundCp.transform.position;
                foundByID = true;
                Debug.Log($"[PlayerHealth] Checkpoint encontrado por ID '{savedID}' na cena. Pos={targetPos}");
            }
            else
            {
                Debug.LogWarning($"[PlayerHealth] Não foi possível achar o checkpoint ID '{savedID}' na cena. Usando fallback {fallbackPos}.");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] Nenhum ID de checkpoint salvo. Usando fallback.");
        }

        // 6) Aplica deslocamento no eixo X na direção CONTRÁRIA de onde morreu (se houver diferença)
        if (respawnBackOffsetX != 0f)
        {
            float deltaX = deathPosition.x - targetPos.x;
            if (Mathf.Abs(deltaX) > 0.01f)
            {
                float dir = Mathf.Sign(deltaX);
                // se morreu à direita do checkpoint (deltaX > 0) -> empurra respawn pra ESQUERDA
                // se morreu à esquerda (deltaX < 0) -> empurra respawn pra DIREITA
                targetPos.x -= dir * respawnBackOffsetX;
            }
        }

        // 7) Reposiciona o player persistente (this.gameObject)
        transform.position = targetPos;
        Debug.Log($"[PlayerHealth] Player persistente reposicionado para {targetPos}.");

        // 8) Restaura física e movimento
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }
        if (movement != null) movement.enabled = true;

        // 9) Restaurar estado de vida e UI
        currentLives = maxLives;
        UpdateHeartsUI();
        isDead = false;

        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.Play("playerIdle");
        }

        Debug.Log($"[PlayerHealth] Respawn concluído na cena {SceneManager.GetActiveScene().name} em {targetPos} (foundByID={foundByID}).");

        // 10) Fade-in
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            float t = 0f;
            float inDuration = Mathf.Max(0.01f, fadeInAfterRespawn);
            while (t < inDuration)
            {
                t += Time.deltaTime;
                c.a = 1f - Mathf.Clamp01(t / inDuration);
                fadeImage.color = c;
                yield return null;
            }
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }
    }

    // Função auxiliar para achar o build index pelo nome da cena (procura no Build Settings)
    private int GetBuildIndexByName(string sceneName)
    {
        int total = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < total; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return i;
        }
        return -1;
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

        if (movement != null)
            movement.StartKnockback(hitDirection);
        else if (rb != null)
        {
            // fallback simples caso não haja PlayerMovement
            rb.linearVelocity = Vector2.zero;
        }

        // Mantém o comportamento antigo de respawn rápido, se você ainda usar este overload
        if (movement != null)
            movement.Respawn(0.3f);
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (isInvulnerable || isDead) return;

        // subtrai o dano recebido em vez de apenas 1 vida
        currentLives -= damage;
        UpdateHeartsUI();

        if (currentLives <= 0)
        {
            Die();
            return;
        }

        if (animator != null)
            animator.SetTrigger("Hit");

        StartCoroutine(InvulnerabilityRoutine());

        if (movement != null)
            movement.StartKnockback(hitDirection);
        else if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
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
