using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyWaveSpawner : MonoBehaviour
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

        while (currentWave < groundEnemiesPerWave.Length || currentWave < flyEnemiesPerWave.Length)
        {
            int groundCount = (currentWave < groundEnemiesPerWave.Length) ? groundEnemiesPerWave[currentWave] : 0;
            int flyCount = (currentWave < flyEnemiesPerWave.Length) ? flyEnemiesPerWave[currentWave] : 0;

            yield return StartCoroutine(SpawnWave(groundCount, flyCount));
            yield return new WaitUntil(() => currentEnemies.Count == 0);

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
            Transform spawnPoint = groundSpawnPoints[Random.Range(0, groundSpawnPoints.Length)];
            GameObject prefab = groundEnemyPrefabs[Random.Range(0, groundEnemyPrefabs.Length)];

            GameObject enemy = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            currentEnemies.Add(enemy);
            RegisterDeathCallback(enemy);

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        // Spawn inimigos voadores
        for (int i = 0; i < flyCount; i++)
        {
            Transform spawnPoint = airSpawnPoints[Random.Range(0, airSpawnPoints.Length)];
            GameObject prefab = flyEnemyPrefabs[Random.Range(0, flyEnemyPrefabs.Length)];

            GameObject enemy = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            currentEnemies.Add(enemy);
            RegisterDeathCallback(enemy);

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        isSpawning = false;
    }

    private void RegisterDeathCallback(GameObject enemy)
    {
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
    }

    private IEnumerator WaitForDeath(EnemyHealth eh, GameObject enemy)
    {
        yield return new WaitUntil(() => eh.IsDead);
        currentEnemies.Remove(enemy);
    }

    private IEnumerator WaitForDeath(FlyEnemyHealth fh, GameObject enemy)
    {
        yield return new WaitUntil(() => fh.IsDead);
        currentEnemies.Remove(enemy);
    }
}
