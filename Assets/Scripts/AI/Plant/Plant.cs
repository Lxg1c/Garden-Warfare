using UnityEngine;
using Photon.Pun;

namespace AI.Plant
{
    public class Plant : MonoBehaviourPun
    {
        public enum State { Neutral, Carried, Placed }

        public State CurrentState { get; private set; } = State.Neutral;

        private Rigidbody rb;
        private Collider col;

        public bool isActive => CurrentState == State.Placed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
        }

        [PunRPC]
        public void SetCarried(int carrierViewID)
        {
            CurrentState = State.Carried;

            rb.isKinematic = true;
            col.enabled = false;
        }

        [PunRPC]
        public void Drop()
        {
            CurrentState = State.Neutral;

            rb.isKinematic = false;
            col.enabled = true;
        }

        [PunRPC]
        public void Place(Vector3 pos, float yRot)
        {
            CurrentState = State.Placed;

            transform.position = pos;
            transform.rotation = Quaternion.Euler(0, yRot, 0);

            rb.isKinematic = true;
            col.enabled = true;

            // активируешь стрельбу:
            GetComponent<PlantTurret>()?.Activate();
        }
    }
}
