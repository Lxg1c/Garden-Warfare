using Core.Interfaces;
using Photon.Pun;
using UnityEngine;

namespace Core.Components
{
    /// <summary>
    /// Универсальный Health:
    /// - реализует IDamageable.TakeDamage(float)
    /// - имеет RPC TakeDamageRPC для вызовов по сети (может передавать attackerViewId)
    /// - вызывает событие OnDamaged(attacker) после получения урона
    /// </summary>
    public class Health : MonoBehaviourPun, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        private float _currentHealth;

        public delegate void DamageEvent(Transform attacker);
        public event DamageEvent OnDamaged;

        public delegate void DeathEvent();
        public event DeathEvent OnDeath;

        private void Start()
        {
            _currentHealth = maxHealth;
        }

        // -----------------------
        // Интерфейс (локальный вызов)
        // -----------------------
        // Это метод, который требует IDamageable.
        // Вызывай именно его в оффлайне / локально (или из игрового кода),
        // он решит — применить урон локально или отправить запрос на авторитет по сети.
        public void TakeDamage(float amount, Transform attacker = null)
        {
            // Локальный вызов без информации об атакующем
            // В сетевом варианте лучше вызывать RPC с attackerViewId, но для упрощения этот метод применяет урон локально,
            // или, если нужно, ты можешь здесь посылать RPC к авторитету (по архитектуре проекта).
            ApplyDamage(amount, attacker);
        }

        // -----------------------
        // RPC: сетевой вход (выполняется на целевом/авторитетном экземпляре)
        // -----------------------
        // Называем метод явно TakeDamageRPC, чтобы не путаться с интерфейсным TakeDamage.
        [PunRPC]
        private void TakeDamageRPC(float damage, int attackerViewId = -1, PhotonMessageInfo info = default)
        {
            Debug.Log($"[{name}] TakeDamageRPC called. dmg={damage}, attackerViewId={attackerViewId}, sender={(info.Sender != null ? info.Sender.NickName : "unknown")}");
            Transform attacker = null;
            if (attackerViewId != -1)
            {
                var av = PhotonView.Find(attackerViewId);
                if (av != null) attacker = av.transform;
            }

            ApplyDamage(damage, attacker);
        }

        // -----------------------
        // Внутренняя логика применения урона (общая)
        // -----------------------
        private void ApplyDamage(float damage, Transform attacker)
        {
            if (_currentHealth <= 0f)
            {
                Debug.LogWarning($"[{name}] ApplyDamage ignored because already dead.");
                return;
            }

            _currentHealth -= damage;
            Debug.Log($"[{name}] took {damage} damage. HP now {_currentHealth}");

            // Уведомляем подписчиков (AI и т.д.)
            if (OnDamaged != null)
            {
                Debug.Log($"[{name}] invoking OnDamaged (attacker={(attacker!=null?attacker.name:"null")})");
                OnDamaged.Invoke(attacker);
            }

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }
        
        private void Die()
        {
            Debug.Log($"[{name}] died!");
            OnDeath?.Invoke();

            var respawn = FindFirstObjectByType<RespawnManager>();
            if (respawn != null)
                respawn.StartRespawn(gameObject);
            else
                Debug.LogWarning("RespawnManager not found.");

            gameObject.SetActive(false);
        }


        public float GetHealth() => _currentHealth;
    }
}
