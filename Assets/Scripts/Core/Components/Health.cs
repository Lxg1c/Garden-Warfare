using Core.Interfaces;
using Core.Settings;
using Photon.Pun;
using UnityEngine;

namespace Core.Components
{
    public class Health : MonoBehaviourPun, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;

        // Events
        public delegate void DamageEvent(Transform attacker);
        public event DamageEvent OnDamaged;

        public delegate void DeathEvent(Transform deadTransform);
        public event DeathEvent OnDeath;
        
        private void Start()
        {
            currentHealth = maxHealth;
            Debug.Log($"[Health Start] {name}: Current={currentHealth}, Max={maxHealth}");
        }

        // -----------------------
        // Интерфейс (локальный вызов)
        // -----------------------
        public void TakeDamage(float amount, Transform attacker = null)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                var attackerView = attacker ? attacker.GetComponent<PhotonView>() : null;
                int attackerId = attackerView != null ? attackerView.ViewID : -1;

                int dmgToSend = Mathf.CeilToInt(amount);
                photonView.RPC(nameof(TakeDamageRPC), RpcTarget.All, dmgToSend, attackerId);
            }
        }

        [PunRPC]
        public void TakeDamageRPC(int damage, int attackerViewId = -1, PhotonMessageInfo info = default)
        {
            Transform attacker = null;
            if (attackerViewId != -1)
            {
                var av = PhotonView.Find(attackerViewId);
                if (av != null) attacker = av.transform;
            }

            ApplyDamage(damage, attacker);
        }

        private void ApplyDamage(int damage, Transform attacker)
        {
            if (currentHealth <= 0f)
            {
                Debug.LogWarning($"[{name}] ApplyDamage ignored — already dead.");
                return;
            }

            SetHealth(currentHealth - damage);
            Debug.Log($"[{name}] took {damage} dmg. HP: {currentHealth}");

            OnDamaged?.Invoke(attacker);

            if (currentHealth <= 0f)
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

        public void SetHealth(float newHealth)
        {
            currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
            Debug.Log($"[Health SetHealth] {name}: New={currentHealth}, Max={maxHealth}");
        }

        public void Heal(float amount)
        {
            SetHealth(currentHealth + amount);
        }
        
        public float GetHealth() => currentHealth;
        public float GetMaxHealth() => maxHealth;
    }
}