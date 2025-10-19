using UnityEngine;
using Weapon.Interfaces;

namespace Weapon.Base
{
    /// <summary>
    /// Базовый класс для дальнобойного оружия (стрельба + перезарядка).
    /// </summary>
    public abstract class RangedWeapon : Weapon, IReloadable
    {
        [Header("Ranged Weapon Settings")]
        public float fireRate = 0.5f;
        private float _nextFireTime;

        public override bool CanUse() => Time.time >= _nextFireTime;
        protected void MarkUse() => _nextFireTime = Time.time + fireRate;

        public abstract void Reload();
    }
}