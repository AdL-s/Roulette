using UnityEngine;

// PARENT of all items. Never create an asset of this class directly.
// Item types:
//   SelfItemDefinition   - type 1: used on yourself, click the item = used
//   TargetItemDefinition - type 2: used on another player, click the item, then click a player
// To add a new item: inherit from one of the two types, add [CreateAssetMenu], create the asset,
// then add the asset to the ItemSystem item list in the scene.
public abstract class ItemDefinition : ScriptableObject
{
    [Header("Info")] public string itemName;
    [TextArea] public string description;

    [Tooltip("Weight in the random drop. 2 = twice as likely as 1. 0 = never dropped.")] [Min(0f)]
    public float dropWeight = 1f;

    [Header("Presentation")]
    [Tooltip("Model shown in the slot. Needs a Collider. Optional Animator with triggers 'Spawn' and 'Use'.")]
    public GameObject prefab;

    [Tooltip("How long the item dissolves in when it appears")] [Min(0f)]
    public float appearDuration = 0.6f;

    [Tooltip("How long the Use animation and the dissolve out play before the item is removed from the inventory")]
    [Min(0f)]
    public float useAnimDuration = 0.5f;

    public AudioClip useSound;
    [Range(0f, 1f)] public float useSoundVolume = 1f;

    [Tooltip(
        "Pitch is rolled randomly between min and max on every use (on the server, so all players hear the same pitch). Set both to 1 for no variation.")]
    [Range(0.5f, 2f)]
    public float minPitch = 0.9f;

    [Range(0.5f, 2f)] public float maxPitch = 1.1f;

    [Tooltip("Spawned at the item when it is used")]
    public GameObject useVfx;

    [Tooltip("Spawned on the target player (target items only)")]
    public GameObject targetVfx;

    [Min(0.1f)] public float vfxLifetime = 3f;

    public abstract bool RequiresTarget { get; }

    // SERVER ONLY. Return false to reject the use (the item is NOT consumed).
    public virtual bool CanUse(ulong userClientId, ulong targetClientId) => true;

    // SERVER ONLY. For self items targetClientId == userClientId.
    public abstract void Apply(ulong userClientId, ulong targetClientId);

    // SERVER ONLY. Rolled once per use and sent to all clients.
    public float RollPitch() => Random.Range(minPitch, maxPitch);

    public void PlayUseEffects(Vector3 itemPosition, Transform target, float pitch)
    {
        if (useSound != null) PlayUseSound(itemPosition, pitch);

        if (useVfx != null)
            Destroy(Instantiate(useVfx, itemPosition, Quaternion.identity), vfxLifetime);

        if (targetVfx != null && target != null)
            Destroy(Instantiate(targetVfx, target.position, Quaternion.identity, target), vfxLifetime);
    }


    private void PlayUseSound(Vector3 position, float pitch)
    {
        var go = new GameObject("ItemUseSound");
        go.transform.position = position;

        var source = go.AddComponent<AudioSource>();
        source.clip = useSound;
        source.volume = useSoundVolume;
        source.pitch = pitch;
        source.spatialBlend = 1f;

        source.Play();
        Destroy(go, useSound.length / Mathf.Max(0.01f, source.pitch) + 0.1f);
    }
}

// TYPE 1: used on yourself
public abstract class SelfItemDefinition : ItemDefinition
{
    public sealed override bool RequiresTarget => false;
    public sealed override void Apply(ulong userClientId, ulong targetClientId) => ApplyToSelf(userClientId);

    protected abstract void ApplyToSelf(ulong userClientId);
}

// TYPE 2: used on another player
public abstract class TargetItemDefinition : ItemDefinition
{
    public sealed override bool RequiresTarget => true;

    public sealed override void Apply(ulong userClientId, ulong targetClientId) =>
        ApplyToTarget(userClientId, targetClientId);

    protected abstract void ApplyToTarget(ulong userClientId, ulong targetClientId);
}