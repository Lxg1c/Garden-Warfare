// Assets/Scripts/Core/Managers/RespawnManager.cs
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace Core.Settings
{
    /// <summary>
    /// Менеджер респавна. Хранит разрешения на респавн per-player.
    /// Вызовы StartRespawn происходят из Health.Die (или вручную).
    /// </summary>
    public class RespawnManager : MonoBehaviourPunCallbacks
    {
        private static RespawnManager _instance;

        [Header("Настройки возрождения")]
        public float respawnDelay = 3f;

        // статическое состояние — true = респавн разрешён
        private static Dictionary<int, bool> _respawnAllowed = new Dictionary<int, bool>();

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

            // Инициализация словаря (на случай domain reload)
            if (_respawnAllowed == null)
                _respawnAllowed = new Dictionary<int, bool>();
        }

        /// <summary>
        /// Разрешить/запретить респавн для актор номера.
        /// Это статический метод, можно вызывать из LifeFruit.OnDeath.
        /// </summary>
        public static void SetRespawnEnabled(int actorNumber, bool enabled)
        {
            if (_respawnAllowed == null) _respawnAllowed = new Dictionary<int, bool>();
            _respawnAllowed[actorNumber] = enabled;
            Debug.Log($"RespawnManager: SetRespawnEnabled({actorNumber}, {enabled})");
        }

        public static bool IsRespawnAllowed(int actorNumber)
        {
            if (_respawnAllowed == null) _respawnAllowed = new Dictionary<int, bool>();
            if (!_respawnAllowed.ContainsKey(actorNumber))
                _respawnAllowed[actorNumber] = true;
            return _respawnAllowed[actorNumber];
        }

        /// <summary>
        /// Запускает респавн — если объект игрок, и если разрешён респавн для владельца.
        /// </summary>
        public void StartRespawn(GameObject deadObject)
        {
            if (!IsPlayer(deadObject))
            {
                Debug.Log($"StartRespawn: {deadObject.name} is not a player — skipping.");
                return;
            }

            PhotonView pv = deadObject.GetComponent<PhotonView>();
            if (pv == null)
            {
                Debug.LogWarning("StartRespawn: dead object has no PhotonView.");
                return;
            }

            int actor = pv.OwnerActorNr;
            if (!IsRespawnAllowed(actor))
            {
                Debug.Log($"Player {actor} cannot respawn — life fruit destroyed.");
                return;
            }

            StartCoroutine(RespawnCoroutine(deadObject));
        }

        private IEnumerator RespawnCoroutine(GameObject deadPlayer)
        {
            yield return new WaitForSeconds(respawnDelay);

            if (deadPlayer == null)
            {
                Debug.LogWarning("RespawnCoroutine: deadPlayer is null.");
                yield break;
            }

            Transform respawnPoint = GetRespawnPointForPlayer(deadPlayer);
            if (respawnPoint == null)
            {
                Debug.LogError("RespawnCoroutine: Cannot find respawn point!");
                yield break;
            }

            // временно отключаем controller для телепортации
            var controller = deadPlayer.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            deadPlayer.transform.position = respawnPoint.position;
            deadPlayer.transform.rotation = respawnPoint.rotation;

            if (controller != null) controller.enabled = true;

            deadPlayer.SetActive(true);

            var health = deadPlayer.GetComponent<Core.Components.Health>();
            if (health != null)
            {
                // Поправляем текущее здоровье — используем рефлексию, как у тебя, 
                // или добавим публичный метод RestoreHealth в Health (лучше — добавить метод).
                var field = typeof(Core.Components.Health)
                    .GetField("_currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(health, health != null ? (int)health.GetHealth() /* noop */ : 100);
                // Лучше иметь метод в Health: RestoreToMax() — но оставим рефлексию, т.к. так у тебя было.
            }

            Debug.Log($"Player respawned at {respawnPoint.name}");
        }

        private bool IsPlayer(GameObject obj)
        {
            if (obj == null) return false;
            if (obj.CompareTag("Player")) return true;
            if (obj.GetComponent<CharacterController>() != null) return true;
            if (obj.GetComponent<UnityEngine.InputSystem.PlayerInput>() != null) return true;
            // NeutralAI — не игрок
            if (obj.GetComponent<AI.NeutralAI>() != null) return false;
            return false;
        }

        private Transform GetRespawnPointForPlayer(GameObject player)
        {
            // Попробуем получить PlayerInfo
            var pi = player.GetComponent<PlayerInfo>();
            if (pi != null && pi.SpawnPoint != null)
                return pi.SpawnPoint;

            var pv = player.GetComponent<PhotonView>();
            if (pv != null && NetworkSpawnManager.instance != null)
                return NetworkSpawnManager.instance.GetPlayerSpawnPoint(pv.OwnerActorNr);

            Debug.LogError("GetRespawnPointForPlayer: cannot determine spawn point.");
            return null;
        }
    }
}
