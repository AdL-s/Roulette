using Steamworks;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            SteamClient.Init(480); // replace 480 with your real AppID when you have one
        }
        catch (System.Exception e)
        {
            Debug.LogError("Steam init failed: " + e.Message);
        }
    }

    private void OnApplicationQuit()
    {
        SteamClient.Shutdown();
    }

    private void Update()
    {
        SteamClient.RunCallbacks();
    }
}