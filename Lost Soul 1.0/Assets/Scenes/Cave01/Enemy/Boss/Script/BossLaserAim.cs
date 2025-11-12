using UnityEngine;

public class BossLaserAim : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Ponto de origem do laser (objeto LaserPoint).")]
    public Transform laserPoint;

    [Tooltip("Transform do Player. Se deixar vazio, ele procura pela Tag.")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Sprite do Laser")]
    [Tooltip("Arrasta aqui o OBJETO que tem o sprite do laser (normalmente o objeto 'Laser'). NÃO é o LaserHitbox.")]
    public GameObject laserSpriteObject;

    [Header("Hitbox do Laser")]
    [Tooltip("Objeto que tem o Collider2D de dano do laser (ex: LaserHitbox).")]
    public GameObject laserHitboxObject;

    [Header("Target do Laser")]
    public GameObject targetSpriteObject;

    [Header("Luzes do Laser")]
    [Tooltip("Luz dos olhos do boss (EyeLight).")]
    public GameObject eyeLightObject;

    [Tooltip("Luz principal do laser (BossLaserLight).")]
    public GameObject bossLaserLightObject;

    private void Awake()
    {
        // Acha o player pela Tag se não tiver sido arrastado
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
                player = p.transform;
        }

        // Começa com o laser escondido
        if (laserSpriteObject != null)
        {
            Debug.Log("[BossLaserAim] Awake -> Desativando laserSpriteObject.");
            laserSpriteObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[BossLaserAim] Awake -> laserSpriteObject NÃO atribuído no Inspector!");
        }

        // Começa com a hitbox desligada também
        if (laserHitboxObject != null)
        {
            Debug.Log("[BossLaserAim] Awake -> Desativando laserHitboxObject.");
            laserHitboxObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[BossLaserAim] Awake -> laserHitboxObject NÃO atribuído no Inspector!");
        }

        // Target começa desligado
        if (targetSpriteObject != null)
        {
            targetSpriteObject.SetActive(false);
        }

        // Luz dos olhos começa desligada
        if (eyeLightObject != null)
        {
            Debug.Log("[BossLaserAim] Awake -> Desativando eyeLightObject.");
            eyeLightObject.SetActive(false);
        }

        // Luz do laser começa desligada
        if (bossLaserLightObject != null)
        {
            Debug.Log("[BossLaserAim] Awake -> Desativando bossLaserLightObject.");
            bossLaserLightObject.SetActive(false);
        }
    }

    // ================================
    //  FUNÇÕES PARA ANIMATION EVENT
    // ================================

    // Chama no começo da animação de laser (pra mirar no player só 1 vez)
    public void AimLaserAtPlayer()
    {
        if (laserPoint == null || player == null)
        {
            Debug.LogWarning("[BossLaserAim] AimLaserAtPlayer -> laserPoint ou player nulo.");
            return;
        }

        Vector3 dir = player.position - laserPoint.position;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        // LaserPoint em Z = 0 aponta pra direita => usamos ângulo a partir do eixo X
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        laserPoint.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Debug.Log("[BossLaserAim] AimLaserAtPlayer -> Rotacionou laserPoint para ângulo " + angle);
    }

    // Chamar num Animation Event quando o LASER VISUAL deve APARECER
    public void ShowLaser()
    {
        if (laserSpriteObject != null)
        {
            laserSpriteObject.SetActive(true);
            Debug.Log("[BossLaserAim] ShowLaser -> Ativou laserSpriteObject.");
        }
        else
        {
            Debug.LogWarning("[BossLaserAim] ShowLaser -> laserSpriteObject NÃO atribuído.");
        }
    }

    // Chamar num Animation Event quando o LASER VISUAL deve SUMIR
    public void HideLaser()
    {
        if (laserSpriteObject != null)
        {
            laserSpriteObject.SetActive(false);
            Debug.Log("[BossLaserAim] HideLaser -> Desativou laserSpriteObject.");
        }
        else
        {
            Debug.LogWarning("[BossLaserAim] HideLaser -> laserSpriteObject NÃO atribuído.");
        }
    }

    // ================================
    //  HITBOX DO LASER
    // ================================

    // Chamar num Animation Event quando o LASER COMEÇA a causar dano
    public void ShowLaserHitbox()
    {
        if (laserHitboxObject != null)
        {
            laserHitboxObject.SetActive(true);
            Debug.Log("[BossLaserAim] ShowLaserHitbox -> Ativou laserHitboxObject.");
        }
        else
        {
            Debug.LogWarning("[BossLaserAim] ShowLaserHitbox -> laserHitboxObject NÃO atribuído.");
        }
    }

    // Chamar num Animation Event quando o LASER PARA de causar dano
    public void HideLaserHitbox()
    {
        if (laserHitboxObject != null)
        {
            laserHitboxObject.SetActive(false);
            Debug.Log("[BossLaserAim] HideLaserHitbox -> Desativou laserHitboxObject.");
        }
        else
        {
            Debug.LogWarning("[BossLaserAim] HideLaserHitbox -> laserHitboxObject NÃO atribuído.");
        }
    }

    // ================================
    //  TARGET LINE
    // ================================

    public void ShowTargetLine()
    {
        if (targetSpriteObject != null)
        {
            targetSpriteObject.SetActive(true);
        }
    }

    public void HideTargetLine()
    {
        if (targetSpriteObject != null)
        {
            targetSpriteObject.SetActive(false);
        }
    }

    // ================================
    //  LUZES
    // ================================

    public void ShowEyeLight()
    {
        if (eyeLightObject != null)
        {
            eyeLightObject.SetActive(true);
            Debug.Log("[BossLaserAim] ShowEyeLight -> Ativou eyeLightObject.");
        }
    }

    public void HideEyeLight()
    {
        if (eyeLightObject != null)
        {
            eyeLightObject.SetActive(false);
            Debug.Log("[BossLaserAim] HideEyeLight -> Desativou eyeLightObject.");
        }
    }

    public void ShowBossLaserLight()
    {
        if (bossLaserLightObject != null)
        {
            bossLaserLightObject.SetActive(true);
            Debug.Log("[BossLaserAim] ShowBossLaserLight -> Ativou bossLaserLightObject.");
        }
    }

    public void HideBossLaserLight()
    {
        if (bossLaserLightObject != null)
        {
            bossLaserLightObject.SetActive(false);
            Debug.Log("[BossLaserAim] HideBossLaserLight -> Desativou bossLaserLightObject.");
        }
    }
}
