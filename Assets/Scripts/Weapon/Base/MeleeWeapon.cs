using UnityEngine;
using Core.Interfaces;

namespace Weapon.Base
{
    /// <summary>
    /// Класс для ближнего боя — без перезарядки.
    /// </summary>
    public class MeleeWeapon : Weapon
    {
        [Header("Melee Weapon Settings")]
        public float attackRange = 2f;
        public LayerMask hitMask;

        public override void Use()
        {
            if (!CanUse()) return;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, attackRange, hitMask))
            {
                if (hit.collider.TryGetComponent(out IDamageable target))
                {
                    target.TakeDamage(damage);
                    Debug.Log($"Hit {hit.collider.name} with {weaponName}");
                }
            }
            else
            {
                Debug.Log($"{weaponName} swing missed");
            }

            MarkUse();
        }
    }
}