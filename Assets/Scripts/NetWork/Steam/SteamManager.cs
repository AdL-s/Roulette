using Steamworks;
using Unity.Netcode;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }

    public const uint AppId = 480; // replace with your real AppID when you have one

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitSteamClient();
    }

    private void InitSteamClient()
    {
        try
        {
            // asyncCallbacks: false -> WE decide when Steam callbacks get pumped (see Update()
            // below), instead of Facepunch.Steamworks running its own background thread. Our
            // lobby/network code touches UnityEngine/NetworkManager APIs from inside Steam
            // callbacks, so they must run on Unity's main thread.
            SteamClient.Init(AppId, false);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Steam init failed: " + e.Message);
        }
    }

    // FacepunchTransport calls SteamClient.Shutdown() whenever NetworkManager.Shutdown() runs
    // (e.g. a hosting player leaving the lobby). That kills the whole Steam client, even though
    // SteamManager is meant to own its lifecycle for the life of the app. Call this to bring it
    // back - it's also called automatically from Update() below as a safety net.
    public void EnsureInitialized()
    {
        if (SteamClient.IsValid) return;

        Debug.LogWarning("SteamClient was shut down externally (likely by the network transport) - reinitializing.");
        InitSteamClient();
    }

    private void OnApplicationQuit()
    {
        SteamClient.Shutdown();
    }

    private void Update()
    {
        if (!SteamClient.IsValid)
        {
            // Something shut Steam down out from under us (see EnsureInitialized above) -
            // bring it back on the very next frame rather than waiting for someone to notice.
            EnsureInitialized();
            return;
        }

        // IMPORTANT: FacepunchTransport pumps Steam callbacks itself (in its own OnEarlyUpdate)
        // for as long as Netcode's NetworkManager is hosting/connecting/connected
        // (NetworkManager.IsListening). If we ALSO call SteamClient.RunCallbacks() here at the
        // same time, the same callback/call-result queue gets processed from two places in the
        // same frame, which corrupts pending async Steam calls (e.g. SteamMatchmaking.LobbyList.
        // RequestAsync() or CreateLobbyAsync() silently hanging forever afterwards). So while a
        // network session is active, let the transport be the only thing pumping.
        bool transportIsPumping = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!transportIsPumping)
        {
            SteamClient.RunCallbacks();
        }
    }
}