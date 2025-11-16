using Core.Interfaces;
using Photon.Pun;
using UnityEngine;
using System.Collections; // Добавлено для корутин, если будут использоваться, хотя здесь не обязательно

namespace Core.Components
{
    /// <summary>
    /// Универсальный Health для сетевого мира:
    /// - Урон рассчитывается только у MasterClient (авторитет)
    /// - Состояние синхронизируется со всеми через RPC
    /// </summary>
    public class Health : MonoBehaviourPun, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        public float MaxHealth => maxHealth; // Добавлено свойство для доступа к MaxHealth
        private float _currentHealth;

        // Events
        public delegate void DamageEvent(Transform attacker);
        public event DamageEvent OnDamaged;

        public delegate void DeathEvent(Transform deadTransform);
        public event DeathEvent OnDeath;

        // --- Добавленные поля для Health Bar ---
        [SerializeField] private GameObject healthBarPrefab; // Префаб полоски здоровья
        private HealthBarController _healthBarController;
        // --- Конец добавленных полей ---

        private void Start()
        {
            _currentHealth = maxHealth;
            InitializeHealthBar(); // Вызываем новый метод для инициализации Health Bar
        }

        /// <summary>
        /// Инициализирует и создает Health Bar для этого существа.
        /// Этот метод можно вызвать извне, если Health Bar нужно создать динамически или позже.
        /// </summary>
        public void InitializeHealthBar()
        {
            // Если Health Bar уже существует, возможно, нужно сначала его уничтожить, чтобы избежать дублирования
            if (_healthBarController != null)
            {
                Destroy(_healthBarController.gameObject);
                _healthBarController = null;
            }

            if (healthBarPrefab != null)
            {
                // Создаем Health Bar. Предполагаем, что HealthBarController управляет своим позиционированием.
                GameObject healthBarGO = Instantiate(healthBarPrefab);
                _healthBarController = healthBarGO.GetComponent<HealthBarController>();
                if (_healthBarController != null)
                {
                    _healthBarController.Initialize(this); // Инициализируем HealthBarController
                    // Обновляем полоску сразу после инициализации, чтобы она показала текущее здоровье
                    _healthBarController.UpdateHealthBar(_currentHealth, maxHealth);
                }
                else
                {
                    Debug.LogError("HealthBarPrefab does not have a HealthBarController component!");
                }
            }
            else
            {
                Debug.LogWarning($"HealthBarPrefab is not assigned for {name}. Health bar will not be displayed.");
            }
        }

        // -----------------------
        // Интерфейс (локальный вызов)
        // -----------------------
        public void TakeDamage(float amount, Transform attacker = null)
        {
            // Только MasterClient инициирует сетевой процесс урона
            if (PhotonNetwork.IsMasterClient)
            {
                var attackerView = attacker ? attacker.GetComponent<PhotonView>() : null;
                int attackerId = attackerView != null ? attackerView.ViewID : -1;

                // Приводим amount к int при отправке (RPC ожидает Int32)
                int dmgToSend = Mathf.CeilToInt(amount);
                photonView.RPC(nameof(TakeDamageRPC), RpcTarget.All, dmgToSend, attackerId);
            }
        }

        // -----------------------
        // RPC: выполняется на всех
        // IMPORTANT: сигнатура должна соответствовать типам, которые мы посылаем!
        // В нашем проекте мы посылаем (int damage, int attackerViewId)
        // -----------------------
        [PunRPC]
        public void TakeDamageRPC(int damage, int attackerViewId = -1, PhotonMessageInfo info = default)
        {
            // Восстанавливаем трансформ атакующего по viewId (если есть)
            Transform attacker = null;
            if (attackerViewId != -1)
            {
                var av = PhotonView.Find(attackerViewId);
                if (av != null) attacker = av.transform;
            }

            // Приводим damage (int) к float для внутренней логики
            ApplyDamage(damage, attacker);
        }

        // -----------------------
        // Внутренняя логика урона
        // -----------------------
        private void ApplyDamage(int damage, Transform attacker)
        {
            if (_currentHealth <= 0f)
            {
                Debug.LogWarning($"[{name}] ApplyDamage ignored — already dead.");
                return;
            }

            SetHealth(_currentHealth - damage); // Используем SetHealth для обновления и Health Bar

            Debug.Log($"[{name}] took {damage} dmg. HP: {_currentHealth}");

            OnDamaged?.Invoke(attacker);

            if (_currentHealth <= 0f)
                Die();
        }

        private void Die()
        {
            Debug.Log($"[{name}] died!");
            OnDeath?.Invoke(transform);

            var respawn = FindFirstObjectByType<RespawnManager>();
            if (respawn != null)
                respawn.StartRespawn(gameObject);
            else
                Debug.LogWarning("RespawnManager not found.");

            gameObject.SetActive(false);

            // Уничтожаем Health Bar при смерти объекта
            if (_healthBarController != null)
            {
                Destroy(_healthBarController.gameObject);
                _healthBarController = null; // Обнуляем ссылку
            }
        }

        public float GetHealth() => _currentHealth;

        // Добавлено для удобства доступа к максимальному здоровью из HealthBarController
        public float GetMaxHealth() => maxHealth;

        /// <summary>
        /// Устанавливает текущее здоровье на указанное значение, с учетом максимального здоровья.
        /// </summary>
        public void SetHealth(float newHealth)
        {
            _currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
            // Обновляем Health Bar после изменения здоровья
            if (_healthBarController != null)
            {
                _healthBarController.UpdateHealthBar(_currentHealth, maxHealth);
            }
        }

        /// <summary>
        /// Лечит существо на указанное количество здоровья.
        /// </summary>
        public void Heal(float amount)
        {
            SetHealth(_currentHealth + amount);
        }
    }
}