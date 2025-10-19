using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Vector3 offset = new Vector3(0f, 0f, -10f);
    private float smoothTime = 0.25f;
    private Vector3 velocity = Vector3.zero;

    [SerializeField] private Transform target;
    private float lookDownOffset = -2f; // quanto a câmera desce
    private bool isLookingDown = false;

    private void Update()
    {
        Vector3 targetPosition = target.position + offset;

        if (isLookingDown)
            targetPosition.y += lookDownOffset;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    public void LookDown(bool state)
    {
        isLookingDown = state;
    }
}
