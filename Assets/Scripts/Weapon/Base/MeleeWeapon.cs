using UnityEngine;
using Core.Components;
using Photon.Pun;

namespace Weapon.Base
{
    public class MeleeWeapon : Weapon
    {
        [Header("Melee Weapon Settings")]
        public float attackRange = 2f;
        public LayerMask hitMask;

        private PhotonView _pv;

        private void Awake()
        {
            _pv = GetComponentInParent<PhotonView>();
        }

        public override void Use()
        {
            if (!CanUse() || !_pv.IsMine) return;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, attackRange, hitMask))
            {
                if (hit.collider.TryGetComponent(out Health targetHealth))
                {
                    var targetView = targetHealth.GetComponent<PhotonView>();
                    if (targetView != null)
                        targetView.RPC("TakeDamageRPC", RpcTarget.All, damage, _pv.ViewID);
                    else
                        targetHealth.TakeDamage(damage, _pv.transform);
                }
            }
            MarkUse();
        }
    }
}