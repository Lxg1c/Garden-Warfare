using UnityEngine;
using Photon.Pun;
using Weapon.Base;
using Weapon.Utils;

namespace Weapon.Types
{
    public class Rifle : RangedWeapon
    {
        [Header("Rifle Settings")]
        public GameObject bulletPrefab;
        public Transform shootPoint;
        public float bulletForce = 10f;
        public float bulletLifetime = 3f;

        private PhotonView _photonView;

        private void Awake()
        {
            _photonView = GetComponentInParent<PhotonView>();
        }

        public override void Use()
        {
            if (!CanUse() || _photonView == null || !_photonView.IsMine)
                return;
            
            object[] instantiationData = { _photonView.ViewID };
            
            GameObject bullet = PhotonNetwork.Instantiate(bulletPrefab.name, shootPoint.position, shootPoint.rotation, 0, instantiationData);

            if (bullet.TryGetComponent(out Rigidbody rb))
                rb.AddForce(shootPoint.forward * bulletForce, ForceMode.Impulse);

            if (bullet.TryGetComponent(out Bullet bulletComponent))
            {
                bulletComponent.damage = damage;
                bulletComponent.owner = _photonView.transform;
            }

            Destroy(bullet, bulletLifetime);

            MarkUse();
        }

        public override void Reload()
        {
            Debug.Log($"Reloading {weaponName}");
        }
    }
}