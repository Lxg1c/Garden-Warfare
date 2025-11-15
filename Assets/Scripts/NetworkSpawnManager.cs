using Photon.Pun;
using UnityEngine;
using Unity.Cinemachine;
using FischlWorks_FogWar;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;

public class NetworkSpawnManager : MonoBehaviourPunCallbacks
{
    public static NetworkSpawnManager instance;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject lifeFruitPrefab;
    [SerializeField] private CinemachineCamera cameraPrefab;
    [SerializeField] private csFogWar fogWar;

    [Header("Spawn Points")]
    [SerializeField] private List<Transform> playerSpawnPoints;
    [SerializeField] private List<Transform> lifeFruitSpawnPoints;

    private Dictionary<int, GameObject> _players;
    private Dictionary<int, GameObject> _lifeFruits;

    private bool _localSpawned;
    private Keyboard _keyboard;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        _players = new Dictionary<int, GameObject>();
        _lifeFruits = new Dictionary<int, GameObject>();

        _keyboard = Keyboard.current;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.3f);

        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            SpawnLocalPlayer();
    }

    public override void OnJoinedRoom()
    {
        StartCoroutine(SpawnDelayed());
    }

    private IEnumerator SpawnDelayed()
    {
        yield return new WaitForSeconds(0.3f);
        SpawnLocalPlayer();
    }

    private void SpawnLocalPlayer()
    {
        if (_localSpawned) return;

        int actor = PhotonNetwork.LocalPlayer.ActorNumber;

        // === PLAYER SPAWN POINT ===
        Transform pSpawn = GetPlayerSpawnPoint(actor);
        // === FRUIT SPAWN POINT ===
        Transform fSpawn = GetFruitSpawnPoint(actor);

        // === SPAWN PLAYER ===
        GameObject player = PhotonNetwork.Instantiate(
            playerPrefab.name,
            pSpawn.position,
            pSpawn.rotation
        );

        _players[actor] = player;
        _localSpawned = true;

        SetupLocalPlayer(player, pSpawn);

        // === SPAWN LIFE FRUIT FOR THIS PLAYER ===
        GameObject fruit = PhotonNetwork.Instantiate(
            lifeFruitPrefab.name,
            fSpawn.position,
            fSpawn.rotation
        );

        _lifeFruits[actor] = fruit;

        Debug.Log($"Spawned PLAYER({actor}) and FRUIT({actor})");
    }


    // ---------------------------
    // SPAWN POINT RESOLUTION
    // ---------------------------
    public Transform GetPlayerSpawnPoint(int actor)
    {
        int idx = (actor - 1) % playerSpawnPoints.Count;
        return playerSpawnPoints[idx];
    }

    private Transform GetFruitSpawnPoint(int actor)
    {
        int idx = (actor - 1) % lifeFruitSpawnPoints.Count;
        return lifeFruitSpawnPoints[idx];
    }


    // ---------------------------
    // LOCAL PLAYER SETUP
    // ---------------------------
    private void SetupLocalPlayer(GameObject player, Transform spawnPoint)
    {
        // Camera
        if (cameraPrefab != null)
        {
            var cam = Instantiate(cameraPrefab);
            cam.Follow = player.transform;
            cam.LookAt = player.transform;
        }

        // Fog of War
        if (fogWar != null)
        {
            fogWar._FogRevealers.Add(
                new csFogWar.FogRevealer(player.transform, 10, true)
            );
        }

        // Save spawn point
        var info = player.GetComponent<PlayerInfo>();
        if (info != null)
        {
            info.SetSpawnPoint(spawnPoint);
            info.SetActorNumber(PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player other)
    {
        int actor = other.ActorNumber;

        if (_players.ContainsKey(actor))
        {
            if (_players[actor] != null)
                PhotonNetwork.Destroy(_players[actor]);
            _players.Remove(actor);
        }

        if (_lifeFruits.ContainsKey(actor))
        {
            if (_lifeFruits[actor] != null)
                PhotonNetwork.Destroy(_lifeFruits[actor]);
            _lifeFruits.Remove(actor);
        }
    }


    private void Update()
    {
        // Debug info
        if (_keyboard != null && _keyboard.f1Key.wasPressedThisFrame)
        {
            Debug.Log($"Players: {_players.Count} | Fruits: {_lifeFruits.Count}");
        }
    }
}
