using UnityEngine;
using Core.Interfaces;
using Core.Components;

namespace Weapon.Utils
{
    public class Bullet : MonoBehaviour
    {
        public int damage = 10;
        public Transform owner;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent(out IDamageable target))
            {
                target.TakeDamage(damage, owner);
            }

            if (collision.gameObject.TryGetComponent(out Health health))
            {
                Debug.Log($"Bullet hit {collision.gameObject.name}, Health: {health.GetHealth()}");
            }
            else
            {
                Debug.Log($"Bullet hit {collision.gameObject.name}, но у него нет компонента Health.");
            }
            Destroy(gameObject);
        }
    }
}