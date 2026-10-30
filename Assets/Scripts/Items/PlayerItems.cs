using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerItems : NetworkBehaviour
{
    public const int MaxItems = 4;

    [Tooltip("4 empty transforms on the player prefab where this player's items appear, in order")] [SerializeField]
    private Transform[] slots = new Transform[MaxItems];

    private NetworkList<int> _items = new NetworkList<int>();

    private readonly GameObject[] _visuals = new GameObject[MaxItems];
    private readonly int[] _visualIds = new int[MaxItems] { -1, -1, -1, -1 };

    private bool _useInProgress; // server-side guard against double-use

    private void Awake()
    {
        if (slots == null || slots.Length != MaxItems) slots = new Transform[MaxItems];

        var children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < MaxItems; i++)
        {
            if (slots[i] != null) continue;

            string slotName = $"PlayerItem_{i + 1}";
            foreach (var child in children)
            {
                if (child.name == slotName)
                {
                    slots[i] = child;
                    break;
                }
            }

            if (slots[i] == null) Debug.LogWarning($"[PlayerItems] '{slotName}' not found under {name}.");
        }
    }

    public override void OnNetworkSpawn()
    {
        _items.OnListChanged += OnItemsChanged;
        RefreshSlots();
    }

    public override void OnNetworkDespawn()
    {
        _items.OnListChanged -= OnItemsChanged;
        for (int i = 0; i < MaxItems; i++)
            if (_visuals[i] != null)
                Destroy(_visuals[i]);
    }

    private void OnItemsChanged(NetworkListEvent<int> _) => RefreshSlots();

    // Finds a player's PlayerItems on any machine (used for effects on the target)
    public static PlayerItems Find(ulong clientId)
    {
        foreach (var p in FindObjectsByType<PlayerItems>(FindObjectsSortMode.None))
            if (p.OwnerClientId == clientId)
                return p;
        return null;
    }

    public bool TryAddItem(int itemIndex)
    {
        if (!IsServer) return false;
        if (_items.Count >= MaxItems) return false;

        _items.Add(itemIndex);
        return true;
    }

    public void RequestUse(int slot, ulong targetClientId) => UseItemServerRpc(slot, targetClientId);

    [ServerRpc] // RequireOwnership = true: only the owning client can call this
    private void UseItemServerRpc(int slot, ulong targetClientId)
    {
        if (_useInProgress) return;
        if (slot < 0 || slot >= _items.Count) return;

        var round = RoundManager.Instance;
        if (round == null || !round.CanUseItems(OwnerClientId)) return;

        int itemIndex = _items[slot];
        ItemDefinition def = ItemSystem.Instance.Get(itemIndex);
        if (def == null) return;

        ulong target = OwnerClientId;
        if (def.RequiresTarget)
        {
            if (targetClientId == OwnerClientId) return; // type 2 items can't target yourself
            if (!round.IsPlayer(targetClientId)) return; // target must be a player in the game
            target = targetClientId;
        }

        if (!def.CanUse(OwnerClientId, target)) return;

        _useInProgress = true;
        def.Apply(OwnerClientId, target);
        PlayUseClientRpc(slot, itemIndex, target, def.RollPitch());
        StartCoroutine(RemoveAfterAnim(slot, def.useAnimDuration));
    }

    private IEnumerator RemoveAfterAnim(int slot, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (slot < _items.Count) _items.RemoveAt(slot);
        _useInProgress = false;
    }

    [ClientRpc]
    private void PlayUseClientRpc(int slot, int itemIndex, ulong targetClientId, float pitch)
    {
        ItemDefinition def = ItemSystem.Instance != null ? ItemSystem.Instance.Get(itemIndex) : null;
        if (def == null) return;

        Vector3 position = transform.position;
        if (slot >= 0 && slot < MaxItems && _visuals[slot] != null)
        {
            position = _visuals[slot].transform.position;
            var vis = _visuals[slot].GetComponent<ItemVisual>();
            if (vis != null) vis.PlayUse();
        }

        Transform target = null;
        if (def.RequiresTarget)
        {
            var targetPlayer = Find(targetClientId);
            if (targetPlayer != null) target = targetPlayer.transform;
        }

        def.PlayUseEffects(position, target, pitch);
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < MaxItems; i++)
        {
            int desired = i < _items.Count ? _items[i] : -1;
            if (desired == _visualIds[i]) continue;

            if (_visuals[i] != null) Destroy(_visuals[i]);
            _visuals[i] = null;
            _visualIds[i] = desired;

            if (desired < 0) continue;

            ItemDefinition def = ItemSystem.Instance != null ? ItemSystem.Instance.Get(desired) : null;
            if (def == null)
            {
                Debug.LogWarning(
                    $"[PlayerItems] Item index {desired} not found (no ItemSystem in this scene, or the list is missing/empty at that index).");
                continue;
            }

            if (def.prefab == null)
            {
                Debug.LogWarning($"[PlayerItems] Item '{def.itemName}' has no Prefab assigned.");
                continue;
            }

            if (i >= slots.Length || slots[i] == null)
            {
                Debug.LogWarning($"[PlayerItems] Slot {i} not assigned on {name}.");
                continue;
            }

            _visuals[i] = Instantiate(def.prefab, slots[i].position, slots[i].rotation, slots[i]);

            var vis = _visuals[i].GetComponent<ItemVisual>();
            if (vis == null) vis = _visuals[i].AddComponent<ItemVisual>();
            vis.Init(this, i, def);
        }
    }
}