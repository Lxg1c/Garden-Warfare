using Core.Components;
using Photon.Pun;
using UnityEngine;
using UI.HealthBar;

namespace UI.HealthBar
{
    public class HealthBarController : MonoBehaviourPun
    {
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private Health health;
        
        [Header("Optional - for owner checks")]
        [SerializeField] private bool checkOwnerDamage = false;
        [SerializeField] private int ownerActorNumber = -1;

        private void Start()
        {
            // Проверки на null
            if (healthBar == null)
            {
                Debug.LogError("HealthBar reference is missing!", this);
                return;
            }

            if (health == null)
            {
                health = GetComponent<Health>();
                if (health == null)
                {
                    Debug.LogError("Health component not found!", this);
                    return;
                }
            }

            // Получаем ownerActorNumber если нужно проверять владельца
            if (checkOwnerDamage && ownerActorNumber == -1)
            {
                PhotonView pv = GetComponent<PhotonView>();
                if (pv != null) ownerActorNumber = pv.OwnerActorNr;
            }

            // Инициализируем HealthBar
            healthBar.SetHealth(health.GetHealth(), health.MaxHealth);
            
            // Подписываемся на события
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }

        private void OnDamaged(Transform attacker)
        {
            // Проверка иммунитета к урону от владельца
            if (checkOwnerDamage && attacker != null)
            {
                PhotonView attackerView = attacker.GetComponent<PhotonView>();
                if (attackerView != null && attackerView.OwnerActorNr == ownerActorNumber)
                {
                    // Восстанавливаем здоровье в HealthBar (логика восстановления в Health уже в LifeFruit)
                    healthBar.SetHealth(health.MaxHealth, health.MaxHealth);
                    return;
                }
            }

            // Обычное обновление HealthBar
            healthBar.SetHealth(health.GetHealth(), health.MaxHealth);
        }

        private void OnDeath(Transform dead)
        {
            // Обновляем HealthBar при смерти
            healthBar.SetHealth(0, health.MaxHealth);
        }

        private void OnDestroy()
        {
            // Отписываемся от событий
            if (health != null)
            {
                health.OnDamaged -= OnDamaged;
                health.OnDeath -= OnDeath;
            }
        }

        // Метод для ручного обновления HealthBar
        public void UpdateHealthBar()
        {
            if (healthBar != null && health != null)
            {
                healthBar.SetHealth(health.GetHealth(), health.MaxHealth);
            }
        }
    }
}