using UnityEngine;

/// <summary>
/// Конкретная реализация дальнобойного оружия — винтовка.
/// </summary>
public class Rifle : RangedWeapon
{
    [Header("Rifle Settings")]
    public GameObject bulletPrefab;
    public Transform shootPoint;
    public float bulletForce = 10f;
    public float bulletLifetime = 3f;

    public override void Use()
    {
        if (!CanFire()) return;

        if (bulletPrefab == null || shootPoint == null)
        {
            Debug.LogError($"[{weaponName}] Missing bulletPrefab or shootPoint!");
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

        if (bullet.TryGetComponent(out Bullet bulletComponent))
            bulletComponent.damage = damage;

        if (bullet.TryGetComponent(out Rigidbody rb))
            rb.AddForce(shootPoint.forward * bulletForce, ForceMode.Impulse);

        Destroy(bullet, bulletLifetime);

        MarkFired();
        Debug.Log($"{weaponName} fired!");
    }

    public override void Reload()
    {
        Debug.Log($"Reloading {weaponName}");
        // добавить анимацию/звук/таймер
    }
}