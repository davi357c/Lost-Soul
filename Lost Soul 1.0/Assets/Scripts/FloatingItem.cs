using UnityEngine;
using UnityEngine.Rendering.Universal; // Necessário para Light2D

public class FloatingItem : MonoBehaviour
{
    [Header("Flutuação")]
    public float floatAmplitude = 0.2f; // altura da flutuação
    public float floatSpeed = 2f;       // velocidade da flutuação

    [Header("Coleta")]
    public string playerTag = "Player";
    public KeyCode collectKey = KeyCode.E;

    private Vector3 startPos;
    private bool playerNearby = false;

    [Header("Referências (preencha se quiser manualmente)")]
    public ParticleSystem particleSystemChild;
    public Light2D light2DChild;

    void Start()
    {
        startPos = transform.position;

        // Busca automática se não for atribuída no Inspector
        if (particleSystemChild == null)
            particleSystemChild = GetComponentInChildren<ParticleSystem>();

        if (light2DChild == null)
            light2DChild = GetComponentInChildren<Light2D>();
    }

    void Update()
    {
        // Movimento de flutuação
        transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time * floatSpeed) * floatAmplitude, 0f);

        // Coleta
        if (playerNearby && Input.GetKeyDown(collectKey))
        {
            Collect();
        }
    }

    void Collect()
    {
        // Desativa efeitos visuais
        if (particleSystemChild != null)
            particleSystemChild.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (light2DChild != null)
            light2DChild.gameObject.SetActive(false);

        // Aqui você pode adicionar efeitos de som, aumentar inventário etc.
        // Exemplo: InventoryManager.instance.AddItem("Pergaminho");

        // Destruir o item principal (após leve atraso pra não cortar efeitos)
        Destroy(gameObject, 0.1f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            playerNearby = true;
            // Pode exibir mensagem tipo "Pressione E para coletar"
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(playerTag))
        {
            playerNearby = false;
        }
    }
}
