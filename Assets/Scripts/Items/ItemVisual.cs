using Unity.Netcode;
using UnityEngine;


public class ItemVisual : MonoBehaviour
{
    private PlayerItems _owner;
    private int _slot;
    private ItemDefinition _def;
    private Animator _anim;

    private static readonly int SpawnTrigger = Animator.StringToHash("Spawn");
    private static readonly int UseTrigger = Animator.StringToHash("Use");

    public void Init(PlayerItems owner, int slot, ItemDefinition def)
    {
        _owner = owner;
        _slot = slot;
        _def = def;

        _anim = GetComponentInChildren<Animator>();
        if (_anim != null) _anim.SetTrigger(SpawnTrigger);
    }

    public void PlayUse()
    {
        if (_anim != null) _anim.SetTrigger(UseTrigger);
    }

    public bool CanInteract()
    {
        if (_owner == null || _def == null || RoundManager.Instance == null) return false;
        if (!_owner.IsOwner) return false; // not my item
        if (RoundManager.Instance.CurrentPhase.Value != GamePhase.Action) return false;

        return NetworkManager.Singleton.LocalClientId == RoundManager.Instance.RoundOwnerClientId.Value;
    }

    // Called by ItemSystem while this item is hovered and interactable
    public void ShowTooltip()
    {
        if (_def == null) return;

        string hint = _def.RequiresTarget ? "Click, then click a player" : "Click to use on yourself";
        ItemTooltip.Show(_def.itemName, _def.description + "\n" + hint);
    }

    // Returns true if the click was used
    public bool Click()
    {
        if (!CanInteract()) return false;
        ItemTooltip.Hide();

        if (_def.RequiresTarget) ItemSystem.Instance.BeginTargeting(_owner, _slot);
        else _owner.RequestUse(_slot, 0);

        return true;
    }
}