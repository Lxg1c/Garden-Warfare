using System.Collections;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    private static RespawnManager _instance;

    [Header("Настройки респавна")]
    public float respawnDelay = 3f;
    public Transform respawnPoint; 
    public GameObject playerPrefab; 
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // Добавляем метод для появления игрока при старте
    public void SpawnPlayerAtStart()
    {
        if (playerPrefab != null && respawnPoint != null)
        {
            Instantiate(playerPrefab, respawnPoint.position, respawnPoint.rotation);
            Debug.Log("Игрок создан на точке респавна при старте игры");
        }
    }

    public void StartRespawn(GameObject deadObject)
    {
        // ПРОВЕРЯЕМ ЧТО ЭТО ИГРОК, А НЕ НЕЙТРАЛ
        if (IsPlayer(deadObject))
        {
            StartCoroutine(RespawnCoroutine(deadObject));
        }
        else
        {
            Debug.Log($"Объект {deadObject.name} не является игроком, респавн отменен");
        }
    }

    // Метод для проверки что это игрок
    private bool IsPlayer(GameObject obj)
    {
        // Проверяем по тегу
        if (obj.CompareTag("Player")) return true;
        
        // Или по наличию компонентов, характерных для игрока
        if (obj.GetComponent<CharacterController>() != null) return true;
        if (obj.GetComponent<UnityEngine.InputSystem.PlayerInput>() != null) return true;
        
        // Или по отсутствию компонентов нейтрала
        if (obj.GetComponent<NeutralAI>() != null) return false;
        
        return false;
    }

    private IEnumerator RespawnCoroutine(GameObject deadPlayer)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (deadPlayer != null)
        {
            var controller = deadPlayer.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;

            deadPlayer.transform.position = respawnPoint.position;
            deadPlayer.transform.rotation = respawnPoint.rotation;

            if (controller != null)
                controller.enabled = true;

            deadPlayer.SetActive(true);

            var health = deadPlayer.GetComponent<Core.Components.Health>();
            if (health != null)
            {
                var field = typeof(Core.Components.Health)
                    .GetField("_currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(health, 100f);
            }

            Debug.Log("Игрок возрождён в точке респавна");
        }
        else if (playerPrefab != null)
        {
            Instantiate(playerPrefab, respawnPoint.position, respawnPoint.rotation);
            Debug.Log("Создан новый игрок на точке респавна");
        }
    }
}