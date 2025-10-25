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

    public void StartRespawn(GameObject deadPlayer)
    {
        StartCoroutine(RespawnCoroutine(deadPlayer));
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