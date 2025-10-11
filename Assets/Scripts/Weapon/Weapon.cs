using UnityEngine;

/// <summary>
/// Базовый абстрактный класс для любого оружия.
/// </summary>
public abstract class Weapon : MonoBehaviour
{
    [Header("Base Weapon Settings")]
    public string weaponName = "Weapon";
    public int damage = 10;
    public float useRate = 0.5f;

    protected float nextUseTime;

    public virtual bool CanUse() => Time.time >= nextUseTime;
    protected void MarkUse() => nextUseTime = Time.time + useRate;

    public abstract void Use(); // <-- добавлено
}