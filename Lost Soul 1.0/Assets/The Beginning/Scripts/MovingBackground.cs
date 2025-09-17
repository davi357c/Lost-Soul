using UnityEngine;

[AddComponentMenu("Parallax/Parallax Simple 2D")]
public class ParallaxSimple : MonoBehaviour
{
    [System.Serializable]
    public struct Layer
    {
        public Transform t;             // arraste Background1..4 aqui
        [Range(0f, 1f)] public float factorX; // 0 = acompanha a câmera (perto) | 1 = quase parado (longe)
        [Range(0f, 1f)] public float factorY; // normalmente 1 em 2D sidescroller
        [HideInInspector] public Vector3 startPos;
    }

    public Camera cam;                  // arraste sua Main Camera
    public Layer[] layers;              // 4 entradas: BG1..BG4 (BG4 = mais longe)

    Vector3 _camStart;
    bool _inited;

    void OnEnable()
    {
        if (cam == null) cam = Camera.main;
        Init();
    }

    void OnValidate()
    {
        if (cam == null) cam = Camera.main;
        if (!Application.isPlaying && cam != null)
        {
            Init();
            Apply();
        }
    }

    void LateUpdate()
    {
        if (!_inited) Init();
        Apply(); // roda após a câmera já ter se movido
    }

    void Init()
    {
        if (cam == null || layers == null) return;
        _camStart = cam.transform.position;
        for (int i = 0; i < layers.Length; i++)
            if (layers[i].t != null) layers[i].startPos = layers[i].t.position;
        _inited = true;
    }

    void Apply()
    {
        Vector3 delta = cam.transform.position - _camStart;
        for (int i = 0; i < layers.Length; i++)
        {
            var l = layers[i];
            if (l.t == null) continue;
            float moveX = delta.x * (1f - l.factorX);
            float moveY = delta.y * (1f - l.factorY);
            l.t.position = new Vector3(l.startPos.x + moveX, l.startPos.y + moveY, l.startPos.z);
        }
    }
}
