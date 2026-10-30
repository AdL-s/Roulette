using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemSystem : MonoBehaviour
{
    public static ItemSystem Instance { get; private set; }

    [Header("Database (index in this array = item id, do not reorder during a game)")] [SerializeField]
    private ItemDefinition[] items;

    [Header("Hover/click raycast")] [SerializeField]
    private LayerMask itemLayerMask = ~0;

    [SerializeField] private float rayDistance = 100f;

    [Header("Target selection (type 2 items)")]
    [Tooltip("Layers that can be clicked as targets. Must include the layer of the players' colliders.")]
    [SerializeField]
    private LayerMask targetLayerMask = ~0;

    private ItemVisual _hovered;

    private ItemVisual _tooltipFor;
    private bool _tooltipVisible;

    private PlayerItems _targetingOwner;
    private int _targetingSlot;

    private int _consumedFrame = -1;

    public static Vector2 PointerPosition =>
        (Cursor.lockState == CursorLockMode.Locked || Mouse.current == null)
            ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            : Mouse.current.position.ReadValue();

    public static bool BlockShooting => Instance != null && Instance.IsBlockingShooting();

    public ItemDefinition Get(int index) =>
        (index >= 0 && items != null && index < items.Length) ? items[index] : null;

    private void Awake()
    {
        string first = (items != null && items.Length > 0 && items[0] != null) ? items[0].name : "EMPTY";
        Debug.Log(
            $"[ItemSystem] Awake on '{name}' in scene '{gameObject.scene.name}': {(items == null ? 0 : items.Length)} entries, first = {first}.");

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                $"[ItemSystem] Duplicate ItemSystem '{name}' in scene '{gameObject.scene.name}' destroyed. The first one stays active.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool GrantRandomItem(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[ItemSystem] GrantRandomItem called on a client - ignored.");
            return false;
        }

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) ||
            client.PlayerObject == null)
        {
            Debug.LogWarning($"[ItemSystem] Client {clientId} has no player object.");
            return false;
        }

        var inventory = client.PlayerObject.GetComponentInChildren<PlayerItems>();
        if (inventory == null)
        {
            Debug.LogWarning($"[ItemSystem] Player prefab of client {clientId} has no PlayerItems component.");
            return false;
        }

        int index = RollItemIndex();
        if (index < 0)
        {
            Debug.LogWarning($"[ItemSystem] No item can be granted. {DescribePool()}");
            return false;
        }

        if (!inventory.TryAddItem(index))
        {
            Debug.Log($"[ItemSystem] Client {clientId} inventory is full ({PlayerItems.MaxItems}), no item granted.");
            return false;
        }

        Debug.Log($"[ItemSystem] Granted '{items[index].itemName}' (index {index}) to client {clientId}.");
        return true;
    }

    private string DescribePool()
    {
        if (items == null || items.Length == 0) return "The Items list on ItemSystem is empty.";

        int empty = 0, zeroWeight = 0;
        foreach (var item in items)
        {
            if (item == null) empty++;
            else if (item.dropWeight <= 0f) zeroWeight++;
        }

        return $"Items list has {items.Length} entries: {empty} empty/missing-script, {zeroWeight} with drop weight 0.";
    }

    private int RollItemIndex()
    {
        if (items == null) return -1;

        float total = 0f;
        foreach (var item in items)
            if (item != null)
                total += item.dropWeight;
        if (total <= 0f) return -1;

        float roll = Random.Range(0f, total);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null || items[i].dropWeight <= 0f) continue;
            roll -= items[i].dropWeight;
            if (roll <= 0f) return i;
        }

        return -1;
    }

    public void BeginTargeting(PlayerItems owner, int slot)
    {
        _targetingOwner = owner;
        _targetingSlot = slot;
        _hovered = null;
        _tooltipVisible = false;
        _tooltipFor = null;
        ItemTooltip.Show("Select a target", "Click a player. Right click to cancel.");
    }

    private void EndTargeting()
    {
        _targetingOwner = null;
        _hovered = null;
        ItemTooltip.Hide();
    }

    private bool CanTargetNow()
    {
        var round = RoundManager.Instance;
        return round != null
               && _targetingOwner != null
               && round.CurrentPhase.Value == GamePhase.Action
               && NetworkManager.Singleton.LocalClientId == round.RoundOwnerClientId.Value;
    }

    private bool IsBlockingShooting()
    {
        if (_targetingOwner != null) return true;
        if (_consumedFrame == Time.frameCount) return true;
        return _hovered != null && _hovered.CanInteract();
    }

    private void Update()
    {
        Camera cam = Camera.main;
        if (cam == null || Mouse.current == null) return;

        if (_targetingOwner != null)
        {
            if (CanTargetNow())
            {
                UpdateTargeting(cam);
                return;
            }

            EndTargeting(); // phase changed or turn ended
        }

        UpdateHover(cam);
    }

    private void UpdateTargeting(Camera cam)
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            _consumedFrame = Time.frameCount; // stops the self-shoot on right click
            EndTargeting();
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        _consumedFrame = Time.frameCount; // stops the shot on this click

        Ray ray = cam.ScreenPointToRay(PointerPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, targetLayerMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        PlayerItems target = null;
        foreach (var hit in hits)
        {
            var player = hit.collider.GetComponentInParent<PlayerItems>(); // PlayerItems is on the player prefab
            if (player != null && player.IsOwner) continue;
            target = player;
            break;
        }

        if (target == null) return;

        PlayerItems owner = _targetingOwner;
        int slot = _targetingSlot;
        EndTargeting();
        owner.RequestUse(slot, target.OwnerClientId);
    }

    private void UpdateHover(Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(PointerPosition);

        ItemVisual hit = null;
        if (Physics.Raycast(ray, out RaycastHit info, rayDistance, itemLayerMask))
            hit = info.collider.GetComponentInParent<ItemVisual>();

        _hovered = hit;
        UpdateTooltip();

        if (_hovered != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_hovered.Click()) _consumedFrame = Time.frameCount;
        }
    }

    private void UpdateTooltip()
    {
        bool show = _hovered != null && _hovered.CanInteract();

        if (show)
        {
            if (!_tooltipVisible || _tooltipFor != _hovered)
            {
                _hovered.ShowTooltip();
                _tooltipFor = _hovered;
                _tooltipVisible = true;
            }
        }
        else if (_tooltipVisible)
        {
            ItemTooltip.Hide();
            _tooltipVisible = false;
            _tooltipFor = null;
        }
    }
}