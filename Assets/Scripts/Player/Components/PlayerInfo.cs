using UnityEngine;
using Photon.Pun;

namespace Player.Components
{
    public class PlayerInfo : MonoBehaviourPunCallbacks
    {
        public Transform SpawnPoint { get; private set; }
        public int ActorNumber { get; private set; }

        public void SetSpawnPoint(Transform spawnPoint)
        {
            SpawnPoint = spawnPoint;
            Debug.Log($"Spawn point set: {spawnPoint.name}");
        }

        public void SetActorNumber(int actorNumber)
        {
            ActorNumber = actorNumber;
        }

        private void Start()
        {
            if (photonView.IsMine && ActorNumber == 0)
            {
                ActorNumber = photonView.OwnerActorNr;
            }
        }
    }
}
