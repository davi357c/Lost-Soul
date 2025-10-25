using UnityEngine;
using System.Collections;
using UnityEngine.UI;                    // UI
using UnityEngine.Rendering.Universal;   // Light2D (URP)

public class RandomDisappearHierarchy : MonoBehaviour
{
    [Header("Intervalo (s)")]
    [SerializeField] float minInterval = 2f;
    [SerializeField] float maxInterval = 8f;

    Renderer[] renderers;
    Collider[] colliders3D;
    Collider2D[] colliders2D;
    LineRenderer[] lineRenderers;
    TrailRenderer[] trailRenderers;
    ParticleSystem[] particleSystems;
    Light[] lights3D;
    Light2D[] lights2D;
    CanvasGroup[] canvasGroups;
    Graphic[] graphics;

    // guardamos intensidades originais para voltar exatamente como estava
    float[] lights3DIntensity;
    float[] lights2DIntensity;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders3D = GetComponentsInChildren<Collider>(true);
        colliders2D = GetComponentsInChildren<Collider2D>(true);
        lineRenderers = GetComponentsInChildren<LineRenderer>(true);
        trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        lights3D = GetComponentsInChildren<Light>(true);
        lights2D = GetComponentsInChildren<Light2D>(true);
        canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
        graphics = GetComponentsInChildren<Graphic>(true);

        // cache das intensidades originais
        if (lights3D != null)
        {
            lights3DIntensity = new float[lights3D.Length];
            for (int i = 0; i < lights3D.Length; i++)
                lights3DIntensity[i] = lights3D[i] ? lights3D[i].intensity : 1f;
        }
        if (lights2D != null)
        {
            lights2DIntensity = new float[lights2D.Length];
            for (int i = 0; i < lights2D.Length; i++)
                lights2DIntensity[i] = lights2D[i] ? lights2D[i].intensity : 1f;
        }

        // se houver UI mas nenhum CanvasGroup, adiciona um no objeto raiz para facilitar
        if ((graphics?.Length ?? 0) > 0 && (canvasGroups == null || canvasGroups.Length == 0))
        {
            var cg = gameObject.GetComponent<CanvasGroup>();
            if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
            canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
        }
    }

    void OnEnable() => StartCoroutine(Loop());
    void OnDisable() => StopAllCoroutines();

    IEnumerator Loop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            bool visible = IsVisible();
            SetVisibility(!visible);
        }
    }

    bool IsVisible()
    {
        if (renderers != null && renderers.Length > 0 && renderers[0] != null)
            return renderers[0].enabled;
        if (lights2D != null && lights2D.Length > 0 && lights2D[0] != null)
            return lights2D[0].enabled && lights2D[0].intensity > 0.001f;
        if (lights3D != null && lights3D.Length > 0 && lights3D[0] != null)
            return lights3D[0].enabled && lights3D[0].intensity > 0.001f;
        if (canvasGroups != null && canvasGroups.Length > 0 && canvasGroups[0] != null)
            return canvasGroups[0].alpha > 0.5f;
        return true;
    }

    void SetVisibility(bool visible)
    {
        // Renderers comuns
        if (renderers != null)
            foreach (var r in renderers) if (r) r.enabled = visible;

        // Colliders
        if (colliders3D != null)
            foreach (var c in colliders3D) if (c) c.enabled = visible;
        if (colliders2D != null)
            foreach (var c in colliders2D) if (c) c.enabled = visible;

        // Line/Trail
        if (lineRenderers != null)
            foreach (var lr in lineRenderers) if (lr) lr.enabled = visible;
        if (trailRenderers != null)
            foreach (var tr in trailRenderers) if (tr) tr.emitting = visible;

        // Partículas
        if (particleSystems != null)
        {
            foreach (var ps in particleSystems)
            {
                if (!ps) continue;
                var emission = ps.emission;
                emission.enabled = visible;
                if (visible) ps.Play(true);
                else ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // LUZES 3D
        if (lights3D != null)
        {
            for (int i = 0; i < lights3D.Length; i++)
            {
                var l = lights3D[i];
                if (!l) continue;
                l.enabled = visible;
                if (lights3DIntensity != null && i < lights3DIntensity.Length)
                    l.intensity = visible ? lights3DIntensity[i] : 0f; // redundante, garante “apagado”
            }
        }

        // LUZES 2D (URP)
        if (lights2D != null)
        {
            for (int i = 0; i < lights2D.Length; i++)
            {
                var l2d = lights2D[i];
                if (!l2d) continue;
                l2d.enabled = visible;
                if (lights2DIntensity != null && i < lights2DIntensity.Length)
                    l2d.intensity = visible ? lights2DIntensity[i] : 0f;
            }
        }

        // UI
        if (canvasGroups != null && canvasGroups.Length > 0)
        {
            foreach (var cg in canvasGroups)
            {
                if (!cg) continue;
                cg.alpha = visible ? 1f : 0f;
                cg.interactable = visible;
                cg.blocksRaycasts = visible;
            }
        }
        else if (graphics != null)
        {
            foreach (var g in graphics) if (g) g.enabled = visible;
        }
    }
}
