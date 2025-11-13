using System.Collections;
using UnityEngine;
using Photon.Pun;
using AI;

public class RespawnManager : MonoBehaviourPunCallbacks
{
    private static RespawnManager _instance;

    [Header("Настройки респавна")]
    public float respawnDelay = 3f;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartRespawn(GameObject deadObject)
    {
        if (IsPlayer(deadObject))
        {
            StartCoroutine(RespawnCoroutine(deadObject));
        }
        else
        {
            Debug.Log($"Объект {deadObject.name} не является игроком, респавн отменен");
        }
    }

    private bool IsPlayer(GameObject obj)
    {
        if (obj.CompareTag("Player")) return true;
        if (obj.GetComponent<CharacterController>() != null) return true;
        if (obj.GetComponent<UnityEngine.InputSystem.PlayerInput>() != null) return true;
        if (obj.GetComponent<NeutralAI>() != null) return false;
        return false;
    }

    private IEnumerator RespawnCoroutine(GameObject deadPlayer)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (deadPlayer != null)
        {
            Transform respawnPoint = GetRespawnPointForPlayer(deadPlayer);
            
            if (respawnPoint != null)
            {
                // Отключаем контроллер для телепортации
                var controller = deadPlayer.GetComponent<CharacterController>();
                if (controller != null)
                    controller.enabled = false;

                // Телепортируем на точку респавна
                deadPlayer.transform.position = respawnPoint.position;
                deadPlayer.transform.rotation = respawnPoint.rotation;

                // Включаем контроллер обратно
                if (controller != null)
                    controller.enabled = true;

                // Активируем игрока
                deadPlayer.SetActive(true);

                // Восстанавливаем здоровье
                var health = deadPlayer.GetComponent<Core.Components.Health>();
                if (health != null)
                {
                    var field = typeof(Core.Components.Health)
                        .GetField("_currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    field?.SetValue(health, 100f);
                }

                Debug.Log($"Игрок возрождён на базе: {respawnPoint.name}");
            }
        }
    }

    private Transform GetRespawnPointForPlayer(GameObject player)
    {
        // Пытаемся получить из PlayerInfo
        var playerInfo = player.GetComponent<PlayerInfo>();
        if (playerInfo != null && playerInfo.SpawnPoint != null)
        {
            return playerInfo.SpawnPoint;
        }

        // Используем NetworkSpawnManager
        var photonView = player.GetComponent<PhotonView>();
        if (photonView != null && NetworkSpawnManager.Instance != null)
        {
            return NetworkSpawnManager.Instance.GetRespawnPointForPlayer(photonView.OwnerActorNr);
        }

        Debug.LogError("Cannot find respawn point for player!");
        return null;
    }
}