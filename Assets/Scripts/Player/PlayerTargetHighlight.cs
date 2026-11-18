using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ProximityGlow))]
public class PlayerTargetHighlight : NetworkBehaviour
{
    [Tooltip("The player's hitbox. Empty = CapsuleCollider on this object, otherwise the first collider on it.")]
    [SerializeField]
    private Collider hitbox;

    [Tooltip("Full glow inside this distance from the crosshair (fraction of screen height)")]
    [SerializeField, Range(0f, 0.5f)]
    private float nearRadius = 0.02f;

    [Tooltip("No glow beyond this distance from the crosshair (fraction of screen height)")]
    [SerializeField, Range(0.01f, 1f)]
    private float farRadius = 0.2f;

    private ProximityGlow _glow;

    private static readonly List<PlayerTargetHighlight> Active = new List<PlayerTargetHighlight>();
    private static int _focusFrame = -1;
    private static PlayerTargetHighlight _focused;
    private static float _focusedValue;

    private void Awake()
    {
        _glow = GetComponent<ProximityGlow>();
        if (hitbox == null) hitbox = GetComponent<CapsuleCollider>();
        if (hitbox == null) hitbox = GetComponent<Collider>();
    }

    private void OnEnable() => Active.Add(this);

    private void OnDisable()
    {
        Active.Remove(this);
        if (_focused == this) _focused = null;
    }

    private void Update()
    {
        UpdateFocus();
        _glow.SetTarget(_focused == this ? _focusedValue : 0f);
    }

    private static void UpdateFocus()
    {
        if (_focusFrame == Time.frameCount) return;
        _focusFrame = Time.frameCount;

        _focused = null;
        _focusedValue = 0f;
        if (!LocalPlayerCanPickTarget()) return;

        Camera cam = Camera.main;
        foreach (var p in Active)
        {
            if (!p.IsSpawned || p.IsOwner) continue;

            float v = ProximityGlow.ColliderProximity(cam, p.hitbox, ItemSystem.PointerPosition, p.nearRadius,
                p.farRadius);
            if (v > _focusedValue)
            {
                _focused = p;
                _focusedValue = v;
            }
        }
    }

    private static bool LocalPlayerCanPickTarget()
    {
        var round = RoundManager.Instance;
        var nm = NetworkManager.Singleton;
        if (round == null || nm == null) return false;

        return round.CurrentPhase.Value == GamePhase.Action
               && nm.LocalClientId == round.RoundOwnerClientId.Value;
    }
}