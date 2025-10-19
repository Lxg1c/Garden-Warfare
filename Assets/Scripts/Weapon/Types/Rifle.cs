using UnityEngine;
using Weapon.Base;
using Weapon.Utils;

namespace Weapon.Types
{
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
            if (!CanUse()) return;

            if (bulletPrefab == null || shootPoint == null)
            {
                Debug.LogError($"[{weaponName}] Missing bulletPrefab or shootPoint!");
                return;
            }

            // --- создаем пулю ---
            GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

            // --- настраиваем пулю ---
            if (bullet.TryGetComponent(out Bullet bulletComponent))
            {
                bulletComponent.damage = damage;

                // ✅ передаём владельца (игрока или объект, у кого это оружие)
                bulletComponent.owner = transform.root; // <-- либо transform, если оружие не дочерний объект
                Debug.Log($"[{weaponName}] Bullet owner set to {bulletComponent.owner.name}");
            }

            if (bullet.TryGetComponent(out Rigidbody rb))
                rb.AddForce(shootPoint.forward * bulletForce, ForceMode.Impulse);

            Destroy(bullet, bulletLifetime);
            MarkUse();
        }

        public override void Reload()
        {
            Debug.Log($"Reloading {weaponName}");
            // Анимация / звук / таймер при необходимости
        }
    }
}