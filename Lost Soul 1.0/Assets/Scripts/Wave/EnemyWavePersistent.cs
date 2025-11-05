using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyWaveSpawnerPersistent : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject[] groundEnemyPrefabs;
    public GameObject[] flyEnemyPrefabs;

    [Header("Spawn Points")]
    public Transform[] groundSpawnPoints;
    public Transform[] airSpawnPoints;

    [Header("Waves")]
    [Tooltip("Quantidade de inimigos terrestres por wave")]
    public int[] groundEnemiesPerWave = { 3, 4, 4 };
    [Tooltip("Quantidade de inimigos voadores por wave")]
    public int[] flyEnemiesPerWave = { 1, 2, 2 };

    [Header("Configuração Geral")]
    public float timeBetweenSpawns = 0.5f;
    public string playerTag = "Player";

    [Header("Save Config")]
    [Tooltip("ID único dessa wave (ex: 'wave_floresta1', 'wave_vila', etc)")]
    public string waveID = "wave_default";

    private const string SaveKeyPrefix = "Wave_Completed_";
    private bool playerEntered = false;
    private bool isSpawning = false;
    private int currentWave = 0;

    private List<GameObject> currentEnemies = new List<GameObject>();

    private void Start()
    {
        // Se a wave já foi completada antes, não executa de novo
        if (PlayerPrefs.GetInt(SaveKeyPrefix + waveID, 0) == 1)
        {
            Debug.Log($"Wave '{waveID}' já concluída — não será executada novamente.");
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!playerEntered && collision.CompareTag(playerTag))
        {
            playerEntered = true;
            StartCoroutine(StartWaves());
        }
    }

    private IEnumerator StartWaves()
    {
        yield return new WaitForSeconds(1f);

        int maxWaves = Mathf.Max(groundEnemiesPerWave.Length, flyEnemiesPerWave.Length);

        while (currentWave < maxWaves)
        {
            int groundCount = (currentWave < groundEnemiesPerWave.Length) ? groundEnemiesPerWave[currentWave] : 0;
            int flyCount = (currentWave < flyEnemiesPerWave.Length) ? flyEnemiesPerWave[currentWave] : 0;

            Debug.Log($"Iniciando wave {currentWave} -> ground: {groundCount}, fly: {flyCount}");
            yield return StartCoroutine(SpawnWave(groundCount, flyCount));

            // limpa referências nulas só por segurança
            CleanupNullEnemies();

            // espera até que a lista esteja vazia
            yield return new WaitUntil(() => {
                CleanupNullEnemies();
                return currentEnemies.Count == 0;
            });

            Debug.Log($"Wave {currentWave} completada (todos inimigos removidos).");
            currentWave++;
        }

        // Marca wave como concluída
        PlayerPrefs.SetInt(SaveKeyPrefix + waveID, 1);
        PlayerPrefs.Save();

        Debug.Log($"✅ Wave '{waveID}' concluída e salva permanentemente!");
    }

    private IEnumerator SpawnWave(int groundCount, int flyCount)
    {
        isSpawning = true;

        // Spawn inimigos terrestres
        for (int i = 0; i < groundCount; i++)
        {
            if (groundSpawnPoints.Length == 0 || groundEnemyPrefabs.Length == 0)
            {
                Debug.LogWarning("Nenhum spawn point ou prefab terrestre configurado.");
                break;
            }

            Transform spawnPoint = groundSpawnPoints[Random.Range(0, groundSpawnPoints.Length)];
            GameObject prefab = groundEnemyPrefabs[Random.Range(0, groundEnemyPrefabs.Length)];

            GameObject enemy = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            currentEnemies.Add(enemy);
            Debug.Log($"[Spawn] Inimigo terrestre adicionado. Total: {currentEnemies.Count}");
            RegisterDeathCallback(enemy);

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        // Spawn inimigos voadores
        for (int i = 0; i < flyCount; i++)
        {
            if (airSpawnPoints.Length == 0 || flyEnemyPrefabs.Length == 0)
            {
                Debug.LogWarning("Nenhum spawn point ou prefab voador configurado.");
                break;
            }

            Transform spawnPoint = airSpawnPoints[Random.Range(0, airSpawnPoints.Length)];
            GameObject prefab = flyEnemyPrefabs[Random.Range(0, flyEnemyPrefabs.Length)];

            GameObject enemy = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            currentEnemies.Add(enemy);
            Debug.Log($"[Spawn] Inimigo voador adicionado. Total: {currentEnemies.Count}");
            RegisterDeathCallback(enemy);

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        isSpawning = false;
    }

    private void RegisterDeathCallback(GameObject enemy)
    {
        if (enemy == null) return;

        // Tenta achar componentes de vida e aguardar por IsDead
        EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            StartCoroutine(WaitForDeath(eh, enemy));
            return;
        }

        FlyEnemyHealth fh = enemy.GetComponent<FlyEnemyHealth>();
        if (fh != null)
        {
            StartCoroutine(WaitForDeath(fh, enemy));
            return;
        }

        // Se nenhum componente de vida for encontrado, usa fallback: aguarda o Destroy do GameObject
        Debug.LogWarning($"Prefab '{enemy.name}' não tem EnemyHealth nem FlyEnemyHealth — usando fallback WaitForDestroy.");
        StartCoroutine(WaitForDestroy(enemy));
    }

    private IEnumerator WaitForDeath(EnemyHealth eh, GameObject enemy)
    {
        Debug.Log($"Aguardando IsDead em {enemy.name} (EnemyHealth).");
        yield return new WaitUntil(() => eh != null && eh.IsDead);
        RemoveEnemyFromList(enemy);
        Debug.Log($"Removido {enemy.name} da lista (morreu via EnemyHealth). Restam {currentEnemies.Count}");
    }

    private IEnumerator WaitForDeath(FlyEnemyHealth fh, GameObject enemy)
    {
        Debug.Log($"Aguardando IsDead em {enemy.name} (FlyEnemyHealth).");
        yield return new WaitUntil(() => fh != null && fh.IsDead);
        RemoveEnemyFromList(enemy);
        Debug.Log($"Removido {enemy.name} da lista (morreu via FlyEnemyHealth). Restam {currentEnemies.Count}");
    }

    private IEnumerator WaitForDestroy(GameObject enemy)
    {
        string initialName = (enemy != null) ? enemy.name : "(null)";
        Debug.Log($"Aguardando Destroy em {initialName} (fallback).");

        yield return new WaitUntil(() => enemy == null);

        string removedName = initialName; // se preferir mostrar o mesmo nome capturado antes
        Debug.Log($"Removido {removedName} da lista (destruído). Restam {currentEnemies.Count}");
        RemoveEnemyFromList(enemy);
    }


    private void RemoveEnemyFromList(GameObject enemy)
    {
        // remove explicitamente pelo objeto e também limpa entradas null
        if (enemy != null)
            currentEnemies.Remove(enemy);

        CleanupNullEnemies();
    }

    private void CleanupNullEnemies()
    {
        // remove quaisquer referências que viraram null (objetos destruídos)
        currentEnemies.RemoveAll(e => e == null);
    }
}
