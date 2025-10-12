using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Настройки возрождения")]
    public GameObject playerPrefab; // Префаб игрока
    public float respawnDelay = 2f; // Задержка перед возрождением

    private Vector3 currentCheckpoint; // Текущая точка возрождения
    private GameObject currentPlayer; // Текущий игрок

    void Awake()
    {
        // Паттерн Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Устанавливаем начальную точку возрождения
        SetSpawnpoint(FindObjectOfType<PlayerController>().transform.position);
    }

    // Вызывается при смерти игрока
    public void PlayerDied()
    {
        Debug.Log("Игрок умер. Возрождение через " + respawnDelay + " секунд");
        Invoke(nameof(RespawnPlayer), respawnDelay);
    }

    // Возрождение игрока
    void RespawnPlayer()
    {
        if (currentPlayer != null)
            Destroy(currentPlayer);

        // Создаем нового игрока в точке возрождения
        currentPlayer = Instantiate(playerPrefab, currentCheckpoint, Quaternion.identity);

        // Можно добавить эффекты при возрождении
        Debug.Log("Игрок возрожден!");
    }

    // Для отладки - установка точки возрождения вручную
    public void SetCheckpointManually(Vector3 position)
    {
        SetCheckpoint(position);
    }
}