using UnityEngine;

public class ChestScriptOpen : MonoBehaviour
{
    public float interactDistance = 2f;  // distância máxima para interagir
    public KeyCode interactKey = KeyCode.E;  // tecla para interagir

    private Animator animator;
    private Transform player;
    private bool isOpen = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;  // Assumindo que seu jogador tem a tag "Player"
    }

    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= interactDistance)
        {
            if (Input.GetKeyDown(interactKey) && !isOpen)
            {
                animator.SetTrigger("Open");  // ativa o trigger para abrir
                isOpen = true;
            }
        }
    }
}
