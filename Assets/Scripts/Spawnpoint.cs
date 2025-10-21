using Photon.Pun.Demo.PunBasics;
using UnityEngine;

using UnityEngine;

public class Spawnpoint : MonoBehaviour
{
    public static RespawnManager Instance;

    [Header("Фиксированная точка возрождения")]
    [SerializeField] private Vector3 respawnPosition = Vector3.zero;
    [SerializeField] private bool useWorldCoordinates = true;

    [Header("Настройки")]
    public float respawnDelay = 3f;
    public GameObject playerPrefab;

    void Awake()
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
    }

    public void RespawnPlayer(GameObject playerToRespawn = null)
    {
        StartCoroutine(RespawnCoroutine(playerToRespawn));
    }

    private System.Collections.IEnumerator RespawnCoroutine(GameObject playerToRespawn)
    {
        Debug.Log($"Возрождение через {respawnDelay} секунд в позиции: {respawnPosition}");

        // Ждем указанное время
        yield return new WaitForSeconds(respawnDelay);

        if (playerToRespawn != null)
        {
            // Если передан конкретный игрок - телепортируем его
            TeleportPlayer(playerToRespawn);
        }
    }

    // Телепортация существующего игрока
    private void TeleportPlayer(GameObject player)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        player.transform.position = GetRespawnPosition();

        if (controller != null)
        {
            controller.enabled = true;
        }

        Debug.Log("Игрок возрождён");
    }

 

    // Получение позиции возрождения с учетом типа координат
    private Vector3 GetRespawnPosition()
    {
        return useWorldCoordinates ? respawnPosition : transform.TransformPoint(respawnPosition);
    }

    // Методы для настройки из других скриптов
    public void SetRespawnPosition(Vector3 newPosition)
    {
        respawnPosition = newPosition;
        Debug.Log($"Точка возрождения установлена: {newPosition}");
    }

    public Vector3 GetCurrentRespawnPosition()
    {
        return GetRespawnPosition();
    }

    // Для визуализации точки возрождения в редакторе
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GetRespawnPosition(), 1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawIcon(GetRespawnPosition() + Vector3.up * 2f, "RespawnIcon", true);
    }
}
