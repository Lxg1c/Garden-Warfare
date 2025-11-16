using AI.Plant;
using UnityEngine;
using Photon.Pun;


namespace Player.Components
{
    public class CarryPlantAgent : MonoBehaviourPun
    {
        [Header("Links")] public Transform carryPoint;
        public MonoBehaviour weaponScript; // WeaponController

        private Plant _carriedPlant;

        public bool IsCarrying => _carriedPlant != null;

        private void Update()
        {
            if (!photonView.IsMine) return;

            // Растение следует за точкой переноски
            if (_carriedPlant != null)
            {
                _carriedPlant.transform.position = carryPoint.position;
                _carriedPlant.transform.rotation = carryPoint.rotation;
            }
        }

        // -------------------------------------------------
        // PICK UP
        // -------------------------------------------------
        public void PickupPlant(Plant plant)
        {
            if (!photonView.IsMine) return;
            if (IsCarrying) return;

            _carriedPlant = plant;

            plant.photonView.RPC("SetCarried", RpcTarget.All, photonView.ViewID);

            // отключаем оружие
            if (weaponScript != null)
                weaponScript.enabled = false;
        }

        // -------------------------------------------------
        // DROP
        // -------------------------------------------------
        public void DropPlant()
        {
            if (!photonView.IsMine) return;
            if (!IsCarrying) return;

            _carriedPlant.photonView.RPC("Drop", RpcTarget.All);

            _carriedPlant = null;

            // включаем оружие обратно
            if (weaponScript != null)
                weaponScript.enabled = true;
        }

        // -------------------------------------------------
        // PLACE ON BASE
        // -------------------------------------------------
        public void PlacePlant(Vector3 snappedPos, Quaternion rot)
        {
            if (!photonView.IsMine) return;
            if (!IsCarrying) return;

            _carriedPlant.photonView.RPC(
                "Place",
                RpcTarget.All,
                snappedPos,
                rot.eulerAngles.y
            );

            _carriedPlant = null;

            if (weaponScript != null)
                weaponScript.enabled = true;
        }

        public void OnPlayerDamaged()
        {
            if (IsCarrying)
                DropPlant();
        }

        // -------------------------------------------------
        // GRID SNAPPING
        // -------------------------------------------------
        public Vector3 SnapToGrid(Vector3 pos, float grid = 1f)
        {
            pos.x = Mathf.Round(pos.x / grid) * grid;
            pos.z = Mathf.Round(pos.z / grid) * grid;
            return pos;
        }
    }
}