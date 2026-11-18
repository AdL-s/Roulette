using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemVisual : MonoBehaviour
{
    private PlayerItems _owner;
    private int _slot;
    private ItemDefinition _def;
    private Animator _anim;
    private Dissolver _dissolver;
    private ProximityGlow _glow;

    private bool _used;
    private float _hover;
    private float _bobPhase;
    private Vector3 _baseLocalPos;
    private Vector3 _baseLocalScale;
    private bool _transformDirty;
    private float _currentLift;
    private Collider _collider;

    private static readonly List<ItemVisual> Active = new List<ItemVisual>();
    private static int _focusFrame = -1;
    private static ItemVisual _focused;
    private static float _focusedValue;

    private static readonly int SpawnTrigger = Animator.StringToHash("Spawn");
    private static readonly int UseTrigger = Animator.StringToHash("Use");

    public void Init(PlayerItems owner, int slot, ItemDefinition def)
    {
        _owner = owner;
        _slot = slot;
        _def = def;

        _baseLocalPos = transform.localPosition;
        _baseLocalScale = transform.localScale;
        _bobPhase = Random.value * Mathf.PI * 2f;

        _anim = GetComponentInChildren<Animator>();
        if (_anim != null) _anim.SetTrigger(SpawnTrigger);

        _collider = GetComponentInChildren<Collider>();

        _glow = GetComponent<ProximityGlow>();
        if (_glow == null) _glow = gameObject.AddComponent<ProximityGlow>();
        var sys = ItemSystem.Instance;
        if (sys != null) _glow.Configure(sys.HoverGlowColor, sys.HoverGlowIntensity);

        _dissolver = GetComponent<Dissolver>();
        if (_dissolver == null) _dissolver = gameObject.AddComponent<Dissolver>();
        _dissolver.SetAmount(1f);
        _dissolver.DissolveIn(def.appearDuration);
    }

    public void SetSlot(int slot) => _slot = slot;

    public void PlayUse(float duration)
    {
        _used = true;
        if (_anim != null) _anim.SetTrigger(UseTrigger);
        if (_dissolver != null) _dissolver.DissolveOut(duration);
    }

    public bool CanInteract()
    {
        if (_used) return false;
        if (_owner == null || _def == null || RoundManager.Instance == null) return false;
        if (!_owner.IsOwner) return false; // not my item
        if (RoundManager.Instance.CurrentPhase.Value != GamePhase.Action) return false;

        return NetworkManager.Singleton.LocalClientId == RoundManager.Instance.RoundOwnerClientId.Value;
    }

    private void OnEnable() => Active.Add(this);

    private void OnDisable()
    {
        Active.Remove(this);
        if (_focused == this) _focused = null;
    }

    private static void UpdateFocus(ItemSystem sys)
    {
        if (_focusFrame == Time.frameCount) return;
        _focusFrame = Time.frameCount;

        _focused = null;
        _focusedValue = 0f;
        Camera cam = Camera.main;

        foreach (var v in Active)
        {
            if (!v.CanInteract()) continue;

            if (sys.IsSelected(v._owner, v._slot))
            {
                _focused = v;
                _focusedValue = 1f;
                return;
            }

            float p = ProximityGlow.ColliderProximity(cam, v._collider, ItemSystem.PointerPosition,
                sys.HoverNearRadius, sys.HoverFarRadius,
                Vector3.up * v._currentLift);
            if (p > _focusedValue)
            {
                _focused = v;
                _focusedValue = p;
            }
        }
    }

    private void Update()
    {
        var sys = ItemSystem.Instance;
        if (sys == null) return;

        UpdateFocus(sys);
        float target = _focused == this ? _focusedValue : 0f;

        _hover = Mathf.MoveTowards(_hover, target, sys.HoverFadeSpeed * Time.deltaTime);
        _glow.SetValue(_hover);

        if (_hover > 0f || _transformDirty) ApplyHoverTransform(sys);
    }

    private void ApplyHoverTransform(ItemSystem sys)
    {
        float bob = Mathf.Sin(Time.time * sys.HoverBobSpeed + _bobPhase) * sys.HoverBobAmplitude * _hover;
        float lift = sys.HoverLift * _hover + bob;

        Vector3 basePos = transform.parent != null ? transform.parent.TransformPoint(_baseLocalPos) : _baseLocalPos;
        transform.position = basePos + Vector3.up * lift;
        _currentLift = lift;
        transform.localScale = _baseLocalScale * (1f + sys.HoverScale * _hover);

        _transformDirty = _hover > 0f;
    }

    public void ShowTooltip()
    {
        if (_def == null) return;

        string hint = _def.RequiresTarget ? "Click, then click a player" : "Click to use on yourself";
        ItemTooltip.Show(_def.itemName, _def.description + "\n" + hint);
    }

    public bool Click()
    {
        if (!CanInteract()) return false;
        ItemTooltip.Hide();

        if (_def.RequiresTarget) ItemSystem.Instance.BeginTargeting(_owner, _slot);
        else _owner.RequestUse(_slot, 0);

        return true;
    }
}