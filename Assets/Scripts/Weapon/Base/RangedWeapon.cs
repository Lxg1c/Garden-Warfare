using UnityEngine;
using Weapon.Interfaces;

namespace Weapon.Base
{
    /// <summary>
    /// Базовый класс для дальнобойного оружия (стрельба + перезарядка).
    /// </summary>
    public abstract class RangedWeapon : Weapon, IReloadable
    {
        public abstract void Reload();
    }
}