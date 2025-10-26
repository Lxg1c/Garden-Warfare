using Photon.Pun;
using UnityEngine;
using Unity.Cinemachine;
using FischlWorks_FogWar;

namespace DefaultNamespace
{
    public class Spawn : MonoBehaviour
    {
        [SerializeField] private Transform spawn;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CinemachineCamera cameraPrefab;
        [SerializeField] private csFogWar fogWar;

        void Start()
        {
            GameObject newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, spawn.position, Quaternion.identity);
            
            if (newPlayer.GetComponent<PhotonView>().IsMine)
            {
                CreatePersonalCameraForPlayer(newPlayer);
                SetupFogWar(newPlayer);
            }
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
                // Создаем FogRevealer и добавляем в список
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