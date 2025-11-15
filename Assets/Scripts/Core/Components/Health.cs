using Core.Interfaces;
using Core.Settings;
using Photon.Pun;
using UnityEngine;

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
        private float _currentHealth;
        public float MaxHealth => maxHealth;

        // Events
        public delegate void DamageEvent(Transform attacker);
        public event DamageEvent OnDamaged;

        public delegate void DeathEvent(Transform deadTransform);
        public event DeathEvent OnDeath;

        private void Start()
        {
            _currentHealth = maxHealth;
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

            _currentHealth -= damage;
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
        }

        // Setter && Getter
        public float GetHealth() => _currentHealth;
        
        public void SetHealth(float value)
        {
            _currentHealth = Mathf.Clamp(value, 0, maxHealth);
        }
    }
}
