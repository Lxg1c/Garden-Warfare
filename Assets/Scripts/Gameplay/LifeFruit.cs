using Core.Components;
using Photon.Pun;
using UnityEngine;
using Core.Settings;

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

        private void OnDamaged(Transform attacker)
        {
            // Если наш владелец попал по своему предмету, то отменяем урон
            if (attacker != null)
            {
                PhotonView attackerView = attacker.GetComponent<PhotonView>();
                if (attackerView != null && attackerView.OwnerActorNr == ownerActorNumber)
                {
                    Debug.Log($"LifeFruit({ownerActorNumber}) ignored self damage");
                    _health.SetHealth(_health.GetMaxHealth());
                }
            }
        }

        private void OnDeath(Transform dead)
        {
            if (!_initialized) return;
            RespawnManager.SetRespawnEnabled(ownerActorNumber, false);
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDamaged -= OnDamaged;
                _health.OnDeath -= OnDeath;
            }
        }
    }
}