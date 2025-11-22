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
            
            // ✅ ИСПРАВЛЕНИЕ: Используем MaxHealth из Health компонента
            healthBar.SetMaxHealth(health.GetMaxHealth());
            healthBar.SetHealth(health.GetHealth());
            
            // ✅ ДЛЯ ОТЛАДКИ:
            Debug.Log($"HealthBar Initialized: {health.GetHealth()}/{health.GetMaxHealth()} HP");
            
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
            // ✅ РАСКОММЕНТИРУЙТЕ ЭТО:
            healthBar.SetHealth(health.GetHealth());
        }

        private void OnDeath(Transform dead)
        {
            // ✅ РАСКОММЕНТИРУЙТЕ ЭТО:
            healthBar.SetHealth(0);
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