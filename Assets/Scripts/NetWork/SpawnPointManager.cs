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

        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        int index = 0;
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform spawnPoint = spawnPoints[index % spawnPoints.Length];
            var instance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            index++;
        }
    }
}