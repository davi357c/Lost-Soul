using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Vector3 offset = new Vector3(0f, 0f, -10f);
    private float smoothTime = 0.25f;
    private Vector3 velocity = Vector3.zero;

    private Transform target;
    private float lookDownOffset = -2f;
    private bool isLookingDown = false;

    private void Awake()
    {
        // Faz a câmera não ser destruída ao trocar de cena
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Tenta achar o player automaticamente ao iniciar a cena
        FindPlayer();
    }

    private void Update()
    {
        if (target == null)
        {
            FindPlayer(); // caso o player ainda não tenha sido instanciado
            return;
        }

        Vector3 targetPosition = target.position + offset;
        if (isLookingDown)
            targetPosition.y += lookDownOffset;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    public void LookDown(bool state)
    {
        isLookingDown = state;
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
        }
    }
}
