using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    private Transform[] spawnPoints;

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint")
            .Select(go => go.transform)
            .ToArray();

        if (spawnPoints.Length == 0)
        {
            Debug.LogError(
                "[SpawnPointManager] No objects with the tag 'SpawnPoint' in the scene. No players spawned.");
            return;
        }

        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        int index = 0;
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform spawnPoint = spawnPoints[index % spawnPoints.Length];
            var instance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            var networkObject = instance.GetComponent<NetworkObject>();
            networkObject.DestroyWithScene = true;
            networkObject.SpawnAsPlayerObject(clientId);

            index++;
        }
    }
}