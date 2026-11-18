using UnityEngine;

public class ProximityGlow : MonoBehaviour
{
    [Tooltip("Leave empty to use all renderers on this object and its children that have the emission property.")]
    [SerializeField]
    private Renderer[] renderers;

    [Header("Shader properties")] [SerializeField]
    private string colorProperty = "_Emission";

    [Tooltip("Leave empty if the shader has no separate intensity property (URP Lit uses only _EmissionColor).")]
    [SerializeField]
    private string intensityProperty = "_EmissionIntensity";

    [Header("Glow")] [ColorUsage(false, true)] [SerializeField]
    private Color glowColor = new Color(1f, 0.85f, 0.55f);

    [Tooltip("Emission added at full glow, on top of the material's own emission")] [SerializeField, Min(0f)]
    private float maxIntensity = 1.5f;

    [Tooltip("How fast SetTarget fades (full range per second)")] [SerializeField, Min(0.1f)]
    private float fadeSpeed = 6f;

    private int _colorId;
    private int _intensityId;
    private bool _hasIntensity;
    private Color[] _baseEmission;
    private MaterialPropertyBlock _mpb;

    private float _current;
    private float _target;
    private float _applied = -1f;

    public float Current => _current;

    private void Awake()
    {
        _colorId = Shader.PropertyToID(colorProperty);
        _hasIntensity = !string.IsNullOrEmpty(intensityProperty);
        if (_hasIntensity) _intensityId = Shader.PropertyToID(intensityProperty);

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        renderers = System.Array.FindAll(renderers, r =>
            r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty(_colorId));

        if (renderers.Length == 0)
            Debug.LogWarning(
                $"[ProximityGlow] No renderer on '{name}' has the property '{colorProperty}'. Glow does nothing.",
                this);

        _baseEmission = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material mat = renderers[i].sharedMaterial;
            Color c = mat.GetColor(_colorId);
            float intensity = (_hasIntensity && mat.HasProperty(_intensityId)) ? mat.GetFloat(_intensityId) : 1f;
            _baseEmission[i] = new Color(c.r * intensity, c.g * intensity, c.b * intensity, 1f);
        }

        _mpb = new MaterialPropertyBlock();
    }

    public void Configure(Color color, float intensity)
    {
        glowColor = color;
        maxIntensity = intensity;
        _applied = -1f;
    }

    public void SetTarget(float value) => _target = Mathf.Clamp01(value);

    public void SetValue(float value)
    {
        _current = _target = Mathf.Clamp01(value);
        ApplyIfChanged();
    }

    public Vector3 Center =>
        renderers != null && renderers.Length > 0 ? renderers[0].bounds.center : transform.position;

    private void Update()
    {
        if (Mathf.Approximately(_current, _target)) return;
        _current = Mathf.MoveTowards(_current, _target, fadeSpeed * Time.deltaTime);
        ApplyIfChanged();
    }

    private void OnDisable()
    {
        _current = _target = 0f;
        ApplyIfChanged();
    }

    private void ApplyIfChanged()
    {
        if (_mpb == null || Mathf.Abs(_current - _applied) < 0.001f) return;
        _applied = _current;

        Color add = glowColor * (maxIntensity * _current);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorId, _baseEmission[i] + add);
            if (_hasIntensity) _mpb.SetFloat(_intensityId, 1f);
            r.SetPropertyBlock(_mpb);
        }
    }

    public static float ColliderProximity(Camera cam, Collider col, Vector2 screenPoint,
        float nearRadius, float farRadius, Vector3 shift = default)
    {
        if (cam == null || col == null || !col.enabled || !col.gameObject.activeInHierarchy) return 0f;

        Ray ray = cam.ScreenPointToRay(screenPoint);
        ray.origin += shift;

        if (col.Raycast(ray, out _, cam.farClipPlane)) return 1f;

        Vector3 center = col.bounds.center;
        float along = Mathf.Max(0f, Vector3.Dot(center - ray.origin, ray.direction));
        Vector3 onRay = ray.origin + ray.direction * along;
        Vector3 closest = ClosestPointOn(col, onRay) - shift;

        return ScreenProximity(cam, closest, screenPoint, nearRadius, farRadius);
    }

    private static Vector3 ClosestPointOn(Collider col, Vector3 point)
    {
        bool supported = col is BoxCollider || col is SphereCollider || col is CapsuleCollider
                         || (col is MeshCollider mesh && mesh.convex);
        return supported ? col.ClosestPoint(point) : col.bounds.ClosestPoint(point);
    }

    public static float ScreenProximity(Camera cam, Vector3 worldPos, Vector2 screenPoint, float nearRadius,
        float farRadius)
    {
        if (cam == null) return 0f;

        Vector3 sp = cam.WorldToScreenPoint(worldPos);
        if (sp.z <= 0f) return 0f;

        float dist = Vector2.Distance(new Vector2(sp.x, sp.y), screenPoint) / Mathf.Max(1, Screen.height);
        float t = Mathf.InverseLerp(nearRadius, Mathf.Max(nearRadius + 0.001f, farRadius), dist);
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }
}