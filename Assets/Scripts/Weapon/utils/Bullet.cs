using UnityEngine;
using Core.Components;
using Photon.Pun;

namespace Weapon.Utils
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PhotonView))]
    [DisallowMultipleComponent]
    public class Bullet : MonoBehaviourPun
    {
        [Header("Bullet Settings")]
        public int damage = 10;
        public float lifetime = 3f;

        [HideInInspector] public Transform owner;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            if (photonView.InstantiationData != null && photonView.InstantiationData.Length > 0)
            {
                int ownerId = (int)photonView.InstantiationData[0];
                PhotonView ownerView = PhotonView.Find(ownerId);
                if (ownerView != null)
                {
                    owner = ownerView.transform;
                }
            }
            
            if (PhotonNetwork.IsMasterClient)
                Destroy(gameObject, lifetime);
        }

        private void Start()
        {
            if (photonView.IsMine && _rb != null && !_rb.isKinematic)
            {
                _rb.AddForce(transform.forward * 10f, ForceMode.Impulse);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (owner != null && collision.transform == owner)
                return;
            
            if (collision.gameObject.TryGetComponent(out Health targetHealth))
            {
                PhotonView ownerView = owner != null ? owner.GetComponent<PhotonView>() : null;

                if (ownerView != null)
                    targetHealth.photonView.RPC("TakeDamageRPC", RpcTarget.All, damage, ownerView.ViewID);
                else
                    targetHealth.TakeDamage(damage, owner);
            }

            PhotonNetwork.Destroy(gameObject);
        }
    }
}
