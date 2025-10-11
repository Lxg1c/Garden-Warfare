using UnityEngine;

/// <summary>
/// Базовый класс для стрелкового оружия.
/// </summary>
public abstract class RangedWeapon : Weapon, IReloadable
{
    [Header("Ranged Weapon Settings")]
    public float fireRate = 0.5f;
    protected float nextFireTime;

    public bool CanFire() => Time.time >= nextFireTime;
    protected void MarkFired() => nextFireTime = Time.time + fireRate;

    public abstract void Reload();
}