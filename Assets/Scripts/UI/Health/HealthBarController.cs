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
            healthBar.SetHealth(health.GetHealth(), health.MaxHealth);
            
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
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
                    healthBar.SetHealth(health.MaxHealth, health.MaxHealth);
                    return;
                }
            }
            
            healthBar.SetHealth(health.GetHealth(), health.MaxHealth);
        }

        private void OnDeath(Transform dead)
        {
            healthBar.SetHealth(0, health.MaxHealth);
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