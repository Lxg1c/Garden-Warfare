using Core.Components;
using Photon.Pun;
using UnityEngine;
using Core.Settings;
using UI;

namespace Gameplay
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PhotonView))]
    public class LifeFruit : MonoBehaviourPun
    {
        [Header("Owner info")]
        [SerializeField] private int ownerActorNumber = -1;

        private Health _health;
        private bool _initialized; 

        [SerializeField] private HealthBar healthBar;

        private void Awake()
        {
            _health = GetComponent<Health>();

            if (ownerActorNumber == -1)
                ownerActorNumber = photonView.OwnerActorNr;

            // Подписки на события
            _health.OnDamaged += OnDamaged;
            _health.OnDeath += OnDeath;

            _initialized = true;

            RespawnManager.SetRespawnEnabled(ownerActorNumber, true);
        }

        private void Start()
        {
            healthBar.SetHealth(_health.GetHealth(), _health.MaxHealth);
        }

        private void OnDamaged(Transform attacker)
        {
            // Если наш владелец попал по своему фрукту → полный иммунитет
            if (attacker != null)
            {
                PhotonView attackerView = attacker.GetComponent<PhotonView>();

                if (attackerView != null && attackerView.OwnerActorNr == ownerActorNumber)
                {
                    Debug.Log($"LifeFruit({ownerActorNumber}) ignored self damage");

                    // Восстанавливаем здоровье до максимума — т.к. Health уже снял 1 тик
                    _health.SetHealth(_health.MaxHealth);

                    healthBar.SetHealth(_health.MaxHealth, _health.MaxHealth);
                    return;
                }
            }

            // Урон от врага — просто обновляем HealthBar
            healthBar.SetHealth(_health.GetHealth(), _health.MaxHealth);
        }

        private void OnDeath(Transform dead)
        {
            if (!_initialized) return;

            RespawnManager.SetRespawnEnabled(ownerActorNumber, false);
        }

        private void OnDestroy()
        {
            _health.OnDamaged -= OnDamaged;
            _health.OnDeath -= OnDeath;
        }
    }
}
