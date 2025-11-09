using Photon.Pun;
using UnityEngine;
using Unity.Cinemachine;
using FischlWorks_FogWar;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem; // Добавляем using для новой Input System

public class NetworkSpawnManager : MonoBehaviourPunCallbacks
{
    public static NetworkSpawnManager Instance;

    [Header("Настройки спавна")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private CinemachineCamera cameraPrefab;
    [SerializeField] private csFogWar fogWar;
    
    [Header("Точки спавна")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    
    private Dictionary<int, Transform> playerSpawnAssignments = new Dictionary<int, Transform>();
    private Dictionary<int, GameObject> playerGameObjects = new Dictionary<int, GameObject>();
    private bool hasSpawnedLocalPlayer = false;

    // Для новой Input System
    private Keyboard keyboard;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Инициализируем новую Input System
        keyboard = Keyboard.current;
    }

    private void Start()
    {
        Debug.Log("NetworkSpawnManager started");
        
        if (PhotonNetwork.IsConnectedAndReady)
        {
            if (PhotonNetwork.InRoom)
            {
                StartCoroutine(DelayedSpawn());
            }
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room as player {PhotonNetwork.LocalPlayer.ActorNumber}. Players in room: {PhotonNetwork.CurrentRoom?.PlayerCount}");
        
        // Спавним локального игрока
        if (photonView.IsMine && !hasSpawnedLocalPlayer)
        {
            StartCoroutine(DelayedSpawn());
        }
        
        // Запрашиваем синхронизацию для всех игроков
        if (photonView.IsMine)
        {
            photonView.RPC("RequestSpawnSync", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"Player {newPlayer.ActorNumber} entered room. Total players: {PhotonNetwork.CurrentRoom.PlayerCount}");
        
        // Если мы мастер-клиент, говорим новому игроку заспавниться
        if (PhotonNetwork.IsMasterClient && photonView.IsMine)
        {
            photonView.RPC("SpawnPlayerForClient", RpcTarget.Others, newPlayer.ActorNumber);
        }
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"Player {otherPlayer.ActorNumber} left room");
        
        // Удаляем объект игрока который вышел
        if (playerGameObjects.ContainsKey(otherPlayer.ActorNumber))
        {
            if (playerGameObjects[otherPlayer.ActorNumber] != null)
            {
                PhotonNetwork.Destroy(playerGameObjects[otherPlayer.ActorNumber]);
            }
            playerGameObjects.Remove(otherPlayer.ActorNumber);
        }
    }

    [PunRPC]
    private void RequestSpawnSync(int requestingPlayerActorNumber)
    {
        Debug.Log($"Player {requestingPlayerActorNumber} requested spawn sync");
        
        // Мастер-клиент говорит всем игрокам заспавнить этого игрока
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SpawnPlayerForClient", RpcTarget.All, requestingPlayerActorNumber);
        }
    }

    [PunRPC]
    private void SpawnPlayerForClient(int actorNumber)
    {
        Debug.Log($"Spawning player {actorNumber} for all clients");
        
        // Если это наш собственный актор номер, спавним локально
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber && !hasSpawnedLocalPlayer)
        {
            SpawnLocalPlayer();
        }
        // Если это другой игрок, и мы еще не спавнили его
        else if (!playerGameObjects.ContainsKey(actorNumber))
        {
            SpawnRemotePlayer(actorNumber);
        }
    }

    private IEnumerator DelayedSpawn()
    {
        // Небольшая задержка для стабильности
        yield return new WaitForSeconds(0.5f);
        
        if (!hasSpawnedLocalPlayer && photonView.IsMine)
        {
            SpawnLocalPlayer();
        }
    }

    private void SpawnLocalPlayer()
    {
        if (hasSpawnedLocalPlayer) return;

        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        Transform spawnPoint = GetSpawnPointForPlayer(actorNumber);
        
        if (spawnPoint == null)
        {
            Debug.LogError("No available spawn points for local player!");
            return;
        }

        Debug.Log($"Spawning LOCAL player {actorNumber} at {spawnPoint.name}");
        
        // Создаем игрока через PhotonNetwork
        GameObject newPlayer = PhotonNetwork.Instantiate(
            playerPrefab.name, 
            spawnPoint.position, 
            spawnPoint.rotation
        );
        
        playerGameObjects[actorNumber] = newPlayer;
        hasSpawnedLocalPlayer = true;

        SetupLocalPlayer(newPlayer, spawnPoint);
        
        // Уведомляем других клиентов о нашем спавне
        if (photonView.IsMine)
        {
            photonView.RPC("NotifyPlayerSpawned", RpcTarget.Others, actorNumber, newPlayer.GetPhotonView().ViewID);
        }
    }

    private void SpawnRemotePlayer(int actorNumber)
    {
        // Для удаленных игроков мы не создаем новые объекты - Photon сделает это автоматически
        // Но мы можем настроить их точки спавна когда они появятся
        
        Transform spawnPoint = GetSpawnPointForPlayer(actorNumber);
        Debug.Log($"Prepared spawn point for REMOTE player {actorNumber} at {spawnPoint?.name}");
        
        // Находим объект игрока когда он появится
        StartCoroutine(FindAndSetupRemotePlayer(actorNumber, spawnPoint));
    }

    private IEnumerator FindAndSetupRemotePlayer(int actorNumber, Transform spawnPoint)
    {
        float timeout = 10f;
        float timer = 0f;

        while (timer < timeout)
        {
            // Ищем все объекты игроков на сцене
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            
            foreach (GameObject player in players)
            {
                PhotonView pv = player.GetComponent<PhotonView>();
                if (pv != null && pv.OwnerActorNr == actorNumber && !playerGameObjects.ContainsKey(actorNumber))
                {
                    playerGameObjects[actorNumber] = player;
                    
                    // Настраиваем точку спавна для респавна
                    var playerInfo = player.GetComponent<PlayerInfo>();
                    if (playerInfo != null && spawnPoint != null)
                    {
                        playerInfo.SetSpawnPoint(spawnPoint);
                        playerInfo.SetActorNumber(actorNumber);
                    }
                    
                    Debug.Log($"Found and setup REMOTE player {actorNumber}");
                    yield break;
                }
            }
            
            timer += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.LogWarning($"Timeout finding remote player {actorNumber}");
    }

    [PunRPC]
    private void NotifyPlayerSpawned(int actorNumber, int viewID)
    {
        Debug.Log($"Player {actorNumber} notified about spawn with viewID {viewID}");
        
        // Можно использовать для дополнительной синхронизации
    }

    private Transform GetSpawnPointForPlayer(int actorNumber)
    {
        // Если у игрока уже есть назначенная точка спавна
        if (playerSpawnAssignments.ContainsKey(actorNumber))
        {
            return playerSpawnAssignments[actorNumber];
        }

        // Назначаем точку спавна по порядку (ActorNumber обычно начинается с 1)
        if (spawnPoints.Count > 0)
        {
            int spawnIndex = (actorNumber - 1) % spawnPoints.Count;
            Transform spawnPoint = spawnPoints[spawnIndex];
            playerSpawnAssignments[actorNumber] = spawnPoint;
            
            Debug.Log($"Assigned spawn point {spawnPoint.name} to player {actorNumber}");
            return spawnPoint;
        }

        Debug.LogError("No spawn points configured!");
        return null;
    }

    private void SetupLocalPlayer(GameObject player, Transform spawnPoint)
    {
        // Настраиваем камеру для локального игрока
        if (cameraPrefab != null)
        {
            CinemachineCamera playerCamera = Instantiate(cameraPrefab);
            playerCamera.Follow = player.transform;
            playerCamera.LookAt = player.transform;
            playerCamera.name = $"PlayerCamera_{player.GetPhotonView().ViewID}";
            Debug.Log($"Personal camera created for local player");
        }

        // Настраиваем туман войны для локального игрока
        if (fogWar != null)
        {
            csFogWar.FogRevealer revealer = new csFogWar.FogRevealer(
                player.transform,
                10,
                true
            );
            fogWar._FogRevealers.Add(revealer);
        }

        // Сохраняем точку спавна для респавна
        var playerInfo = player.GetComponent<PlayerInfo>();
        if (playerInfo != null)
        {
            playerInfo.SetSpawnPoint(spawnPoint);
            playerInfo.SetActorNumber(PhotonNetwork.LocalPlayer.ActorNumber);
        }

        Debug.Log($"Local player setup complete at {spawnPoint.name}");
    }

    public Transform GetRespawnPointForPlayer(int actorNumber)
    {
        return GetSpawnPointForPlayer(actorNumber);
    }

    // Исправленный метод Update с новой Input System
    private void Update()
    {
        // Отладочная информация с использованием новой Input System
        if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
        {
            ShowDebugInfo();
        }
    }

    private void ShowDebugInfo()
    {
        Debug.Log("=== SPAWN MANAGER DEBUG INFO ===");
        Debug.Log($"Local player spawned: {hasSpawnedLocalPlayer}");
        Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom?.PlayerCount}");
        Debug.Log($"Tracked player objects: {playerGameObjects.Count}");
        
        foreach (var kvp in playerGameObjects)
        {
            Debug.Log($"Player {kvp.Key}: {(kvp.Value != null ? "Exists" : "Null")}");
        }
        
        Debug.Log("=== SPAWN POINT ASSIGNMENTS ===");
        foreach (var kvp in playerSpawnAssignments)
        {
            Debug.Log($"Player {kvp.Key} -> {kvp.Value?.name}");
        }
    }

    private void OnGUI()
    {
        if (PhotonNetwork.InRoom)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");
            GUI.Label(new Rect(10, 30, 300, 20), $"My actor number: {PhotonNetwork.LocalPlayer?.ActorNumber}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Local spawned: {hasSpawnedLocalPlayer}");
            GUI.Label(new Rect(10, 70, 300, 20), $"Tracked players: {playerGameObjects.Count}");
        }
    }
}