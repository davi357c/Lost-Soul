using UnityEngine;
using UnityEngine.SceneManagement;

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
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded; // <- reconecta toda vez que trocar de cena
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        FindPlayer();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Espera 1 frame pra garantir que o player foi instanciado
        StartCoroutine(FindPlayerNextFrame());
    }

    private System.Collections.IEnumerator FindPlayerNextFrame()
    {
        yield return null;
        FindPlayer();
    }

    private void Update()
    {
        if (target == null)
            return;

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
            target = playerObj.transform;
    }
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }


}
