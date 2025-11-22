using AI.Plant;
using UnityEngine;
using Photon.Pun;

namespace Player.Components
{
    public class CarryPlantAgent : MonoBehaviourPun
    {
        [Header("Carry Settings")]
        public float snapGridSize = 1f;
        
        public bool IsCarrying { get; private set; }
        public Plant CarriedPlant { get; private set; }
        
        public void PickupPlant(Plant plant)
        {
            if (IsCarrying) return;
            
            // Сетевое взаимодействие
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_PickupPlant", RpcTarget.All, plant.GetComponent<PhotonView>().ViewID);
            }
        }
        
        [PunRPC]
        private void RPC_PickupPlant(int plantViewID)
        {
            PhotonView plantView = PhotonView.Find(plantViewID);
            if (plantView == null) return;
            
            Plant plant = plantView.GetComponent<Plant>();
            if (plant == null) return;
            
            CarriedPlant = plant;
            IsCarrying = true;
            
            // Отключаем физику и AI растения
            Rigidbody rb = plant.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            
            // ПЕРЕМЕЩАЕМ К СЕБЕ (carryPoint)
            plant.transform.SetParent(this.transform);
            plant.transform.localPosition = Vector3.zero;
            plant.transform.localRotation = Quaternion.identity;
            
            // Отключаем коллайдеры
            Collider[] colliders = plant.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
            
            // Отключаем AI поведение
            Plant plantAI = plant.GetComponent<Plant>();
            if (plantAI != null)
            {
                plantAI.enabled = false;
            }
        }
        
        public void DropPlant()
        {
            if (!IsCarrying) return;
            
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_DropPlant", RpcTarget.All);
            }
        }
        
        [PunRPC]
        private void RPC_DropPlant()
        {
            if (CarriedPlant == null) return;
            
            // Восстанавливаем физику
            Rigidbody rb = CarriedPlant.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            
            // Включаем коллайдеры
            Collider[] colliders = CarriedPlant.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = true;
            }
            
            // Включаем AI
            Plant plantAI = CarriedPlant.GetComponent<Plant>();
            if (plantAI != null)
            {
                plantAI.enabled = true;
            }
            
            // Отсоединяем
            CarriedPlant.transform.SetParent(null);
            CarriedPlant = null;
            IsCarrying = false;
        }
        
        public bool PlacePlant(Vector3 position, Quaternion rotation)
        {
            if (!IsCarrying) return false;
            
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_PlacePlant", RpcTarget.All, position, rotation);
                return true;
            }
            
            return false;
        }
        
        [PunRPC]
        private void RPC_PlacePlant(Vector3 position, Quaternion rotation)
        {
            if (CarriedPlant == null) return;
            
            // Устанавливаем растение на землю
            CarriedPlant.transform.SetParent(null);
            CarriedPlant.transform.position = position;
            CarriedPlant.transform.rotation = rotation;
            
            // Включаем коллайдеры
            Collider[] colliders = CarriedPlant.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = true;
            }
            
            // Включаем AI
            Plant plantAI = CarriedPlant.GetComponent<Plant>();
            if (plantAI != null)
            {
                plantAI.enabled = true;
            }
            
            CarriedPlant = null;
            IsCarrying = false;
        }
        
        public Vector3 SnapToGrid(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x / snapGridSize) * snapGridSize,
                position.y,
                Mathf.Round(position.z / snapGridSize) * snapGridSize
            );
        }
    }
}