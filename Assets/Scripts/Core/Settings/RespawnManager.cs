using AI;
using Core.Components;
using Photon.Pun;
using System.Collections;
using UnityEngine;

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
        // Убедитесь, что AI у вас в пространстве имен AI, если это не так, удалите AI. из следующей строки
        if (obj.GetComponent<NeutralAI>() != null) return false; // Проверяем AI первыми, так как они могут быть "объектами"

        if (obj.CompareTag("Player")) return true;
        if (obj.GetComponent<CharacterController>() != null) return true;
        if (obj.GetComponent<UnityEngine.InputSystem.PlayerInput>() != null) return true;

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

                // Восстанавливаем здоровье и переинициализируем Health Bar
                var health = deadPlayer.GetComponent<Core.Components.Health>();
                if (health != null)
                {
                    health.SetHealth(health.MaxHealth); // Используем публичный метод для установки полного здоровья
                    health.InitializeHealthBar(); // Переинициализируем Health Bar
                }

                Debug.Log($"Игрок возрождён на базе: {respawnPoint.name}");
            }
            else
            {
                Debug.LogError($"RespawnManager: Не удалось найти точку респавна для игрока {deadPlayer.name}.");
                // Если нет точки респавна, возможно, стоит снова деактивировать игрока или обработать ошибку иначе.
                // deadPlayer.SetActive(false); 
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