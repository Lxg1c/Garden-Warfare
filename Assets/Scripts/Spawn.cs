using Photon.Pun;
using UnityEngine;
using Unity.Cinemachine;
using FischlWorks_FogWar;

namespace DefaultNamespace
{
    public class Spawn : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CinemachineCamera cameraPrefab;
        [SerializeField] private csFogWar fogWar;
        [SerializeField] private Transform fallbackSpawnPoint;
        void Start()
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("Photon not connected yet, delaying spawn...");
                return;
            }

            SpawnPlayer();
        }

        public override void OnJoinedRoom()
        {
            if (PhotonNetwork.IsConnectedAndReady)
            {
                SpawnPlayer();
            }
        }

        private void SpawnPlayer()
        {
            Transform playerSpawnPoint = GetPlayerSpawnPoint();

            if (playerSpawnPoint == null)
            {
                Debug.LogError("Failed to get spawn point for player! Using fallback.");
                return;
            }

            GameObject newPlayer = PhotonNetwork.Instantiate(
                playerPrefab.name,
                playerSpawnPoint.position,
                playerSpawnPoint.rotation
            );

            if (newPlayer.GetComponent<PhotonView>().IsMine)
            {
                CreatePersonalCameraForPlayer(newPlayer);
                SetupFogWar(newPlayer);

                // Сохраняем ID точки спавна в игроке для респавна
                var playerInfo = newPlayer.GetComponent<PlayerInfo>();
                if (playerInfo != null)
                {
                    playerInfo.SetSpawnPoint(playerSpawnPoint);
                }

                Debug.Log($"Player spawned at: {playerSpawnPoint.name}");
            }
        }

        private Transform GetPlayerSpawnPoint()
        {
            if (SpawnPointManager.Instance == null)
            {
                Debug.LogError("SpawnPointManager instance is null!");

                SpawnPointManager manager = FindObjectOfType<SpawnPointManager>();
                if (manager != null)
                {
                    Debug.Log("Found SpawnPointManager in scene");
                }
                else
                {
                    Debug.LogError("No SpawnPointManager found in scene!");
                    return fallbackSpawnPoint != null ? fallbackSpawnPoint : transform;
                }
            }

            if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
            {
                return SpawnPointManager.Instance.GetSpawnPointForPlayer(PhotonNetwork.LocalPlayer.ActorNumber);
            }

            Debug.LogWarning("Photon not connected, using fallback spawn");
            return fallbackSpawnPoint != null ? fallbackSpawnPoint : transform;
        }

        private void CreatePersonalCameraForPlayer(GameObject player)
        {
            if (cameraPrefab != null)
            {
                CinemachineCamera playerCamera = Instantiate(cameraPrefab);
                playerCamera.Follow = player.transform;
                playerCamera.LookAt = player.transform;
                playerCamera.name = $"PlayerCamera_{player.GetPhotonView().ViewID}";
                Debug.Log($"Personal camera created for player {player.GetPhotonView().ViewID}");
            }
            else
            {
                Debug.LogError("Camera prefab is not assigned!");
            }
        }

        private void SetupFogWar(GameObject player)
        {
            if (fogWar != null)
            {
                csFogWar.FogRevealer revealer = new csFogWar.FogRevealer(
                    player.transform,
                    10,
                    true
                );

                fogWar._FogRevealers.Add(revealer);
            }
            else
            {
                Debug.LogError("Fog War component is not assigned!");
            }
        }
    }
}