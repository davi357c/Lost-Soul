using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ScrollManager : MonoBehaviour
{
    public static ScrollManager Instance { get; private set; }

    [Header("Configuração")]
    [Tooltip("Se true, o manager procura automaticamente todos os FloatingItem ativos na cena no Start.")]
    public bool autoCountScrolls = true;

    [Header("NPC (opções - escolha 1)")]
    [Tooltip("Arraste aqui o NPC já presente na cena (deixe desativado na Hierarchy).")]
    public GameObject npcToEnable;       // opção: habilitar NPC já existente
    [Tooltip("Ou arraste um prefab de NPC para instanciar.")]
    public GameObject npcPrefab;         // opção: instanciar prefab
    public Transform npcSpawnPoint;      // ponto para instanciar o prefab (opcional)

    [Header("Atrasos/efeitos")]
    public float delayBeforeSpawn = 0.5f;

    [Header("Eventos")]
    public UnityEvent OnAllCollected;    // pode ser usado para abrir UI, tocar som, etc.

    private int totalScrolls = 0;
    private int collected = 0;
    private bool finished = false;

    private const string SAVE_KEY = "CollectedScrolls";

    void Awake()
    {
        // Garante que só exista um ScrollManager no jogo
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantém entre cenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (autoCountScrolls)
        {
            // Conta automaticamente todos os FloatingItem ativos
            FloatingItem[] scrolls = FindObjectsOfType<FloatingItem>();
            totalScrolls = scrolls.Length;
        }

        // Carrega progresso salvo
        collected = PlayerPrefs.GetInt(SAVE_KEY, 0);

        Debug.Log($"ScrollManager iniciado: {collected}/{totalScrolls} coletados.");
    }

    public void NotifyScrollCollected()
    {
        if (finished) return;

        collected++;
        Debug.Log($"ScrollManager: coletado {collected}/{totalScrolls}");

        // Salva progresso
        PlayerPrefs.SetInt(SAVE_KEY, collected);
        PlayerPrefs.Save();

        // Verifica se terminou
        if (collected >= totalScrolls && totalScrolls > 0)
        {
            finished = true;
            StartCoroutine(HandleAllCollected());
        }
    }

    public void RegisterNewScroll()
    {
        totalScrolls++;
        Debug.Log($"ScrollManager: novo pergaminho registrado. Total agora = {totalScrolls}");
    }

    private IEnumerator HandleAllCollected()
    {
        Debug.Log("🎉 Todos os pergaminhos coletados!");

        // Evento configurável no inspector
        OnAllCollected?.Invoke();

        yield return new WaitForSeconds(delayBeforeSpawn);

        // Opção A: habilitar NPC já existente
        if (npcToEnable != null)
        {
            npcToEnable.SetActive(true);
            yield break;
        }

        // Opção B: instanciar prefab
        if (npcPrefab != null)
        {
            Vector3 pos = npcSpawnPoint != null ? npcSpawnPoint.position : Vector3.zero;
            Instantiate(npcPrefab, pos, Quaternion.identity);
        }
    }

    // Método opcional para resetar progresso manualmente (pode chamar por botão)
    public void ResetProgress()
    {
        collected = 0;
        PlayerPrefs.SetInt(SAVE_KEY, 0);
        PlayerPrefs.Save();
        finished = false;
        Debug.Log("Progresso de pergaminhos resetado!");
    }
}
