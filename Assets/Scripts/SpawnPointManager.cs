using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class SpawnPointManager : MonoBehaviourPunCallbacks
{
    public static SpawnPointManager Instance;

    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    private Dictionary<int, Transform> playerSpawnPoints = new Dictionary<int, Transform>();
    private int nextSpawnIndex = 0;

    private void Awake()
    {
        // Убедимся что только один экземпляр существует
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSpawnPoints();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSpawnPoints()
    {
        Debug.Log($"SpawnPointManager initialized with {spawnPoints.Count} spawn points");
        
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points assigned in SpawnPointManager!");
        }
    }

    public Transform GetSpawnPointForPlayer(int actorNumber)
    {
        // Если у игрока уже есть назначенная точка спавна
        if (playerSpawnPoints.ContainsKey(actorNumber))
        {
            return playerSpawnPoints[actorNumber];
        }

        // Назначаем новую точку спавна
        if (spawnPoints.Count > 0)
        {
            Transform spawnPoint = spawnPoints[nextSpawnIndex % spawnPoints.Count];
            playerSpawnPoints[actorNumber] = spawnPoint;
            nextSpawnIndex++;
            
            Debug.Log($"Player {actorNumber} assigned to spawn point: {spawnPoint.name}");
            return spawnPoint;
        }

        Debug.LogError("No spawn points assigned in SpawnPointManager!");
        return transform; // Fallback на текущую позицию менеджера
    }

    public Transform GetRespawnPointForPlayer(int actorNumber)
    {
        return GetSpawnPointForPlayer(actorNumber);
    }

    // Для отладки
    private void OnDrawGizmos()
    {
        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(spawnPoint.position, Vector3.one * 2f);
                Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 3f);
            }
        }
    }
}