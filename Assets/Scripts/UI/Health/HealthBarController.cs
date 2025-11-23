using FischlWorks_FogWar;
using Photon.Pun;
using UnityEngine;

namespace UI.Health
{
    public class HealthBarController : MonoBehaviourPun
    {
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private Core.Components.Health health;
        
        [Header("Optional - for owner checks")]
        [SerializeField] private bool checkOwnerDamage;
        [SerializeField] private int ownerActorNumber = -1;
        [SerializeField] private csFogVisibilityAgent csFogVisibilityAgent;

        private void Start()
        {
            InitializeHealthBar();
        }

        private void InitializeHealthBar()
        {
            if (healthBar == null)
            {
                Debug.LogError("HealthBar reference is missing!", this);
                return;
            }

            if (health == null)
            {
                health = GetComponent<Core.Components.Health>();
                if (health == null)
                {
                    Debug.LogError("Health component not found!", this);
                    return;
                }
            }
            
            if (checkOwnerDamage && ownerActorNumber == -1)
            {
                PhotonView pv = GetComponent<PhotonView>();
                if (pv != null) ownerActorNumber = pv.OwnerActorNr;
            }
            
            // ✅ ВКЛЮЧАЕМ HealthBar при инициализации
            healthBar.gameObject.SetActive(true);
            healthBar.SetMaxHealth(health.GetMaxHealth());
            healthBar.SetHealth(health.GetHealth());
            
            Debug.Log($"HealthBar Initialized: {health.GetHealth()}/{health.GetMaxHealth()} HP");
            
            // Отписываемся и подписываемся заново (на случай респавна)
            health.OnDamaged -= OnDamaged;
            health.OnDeath -= OnDeath;
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }
        
        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                healthBar.transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void FixedUpdate()
        {
            if (csFogVisibilityAgent == null) return;

            bool visible = csFogVisibilityAgent.GetVisibility();
            healthBar.gameObject.SetActive(visible);
        }

        private void OnDamaged(Transform attacker)
        {
            if (checkOwnerDamage && attacker != null)
            {
                PhotonView attackerView = attacker.GetComponent<PhotonView>();
                if (attackerView != null && attackerView.OwnerActorNr == ownerActorNumber)
                {
                    return;
                }
            }
            
            healthBar.SetHealth(health.GetHealth());
        }

        private void OnDeath(Transform dead)
        {
            // ✅ При смерти СКРЫВАЕМ HealthBar, но не уничтожаем
            healthBar.gameObject.SetActive(false);
            healthBar.SetHealth(0);
        }

        // ✅ ДОБАВЬТЕ метод для обработки респавна
        public void OnRespawn()
        {
            Debug.Log("HealthBarController: OnRespawn called");
            
            // ✅ ВКЛЮЧАЕМ HealthBar и обновляем значения
            healthBar.gameObject.SetActive(true);
            healthBar.SetMaxHealth(health.GetMaxHealth());
            healthBar.SetHealth(health.GetHealth());
            
            Debug.Log($"HealthBar after respawn: {health.GetHealth()}/{health.GetMaxHealth()} HP");
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDamaged -= OnDamaged;
                health.OnDeath -= OnDeath;
            }
        }
    }
}