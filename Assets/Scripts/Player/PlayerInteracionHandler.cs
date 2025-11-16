using AI.Plant;
using Gameplay;
using UnityEngine;
using Photon.Pun;
using Player.Components;
using Weapon;

namespace Player
{
    public class PlayerInteractionHandler : MonoBehaviourPun
    {
        public CarryPlantAgent carry;
        public LayerMask plantLayer;
        public float interactRange = 3f;
        public WeaponController weaponController;
    
        private PlayerInputActions _input;
    
        private void Awake()
        {
            _input = new PlayerInputActions();
    
            _input.Player.Interact.performed += _ => TryPickup();
            _input.Player.Drop.performed += _ => TryDrop();
            _input.Player.Place.performed += _ => TryPlace();
        }
    
        private void OnEnable()
        {
            if (photonView.IsMine)
                _input.Enable();
        }
    
        private void OnDisable()
        {
            if (photonView.IsMine)
                _input.Disable();
        }
    
        private void TryPickup()
        {
            if (!photonView.IsMine) return;
            if (carry.IsCarrying) return;
    
            // ищем растение перед игроком
            if (Physics.Raycast(transform.position + Vector3.up,
                    transform.forward,
                    out RaycastHit hit,
                    interactRange,
                    plantLayer))
            {
                Plant plant = hit.collider.GetComponentInParent<Plant>();
                if (plant != null)
                {
                    carry.PickupPlant(plant);
                    weaponController.enabled = false;
                }
            }
        }
    
        private void TryDrop()
        {
            if (!photonView.IsMine) return;
            carry.DropPlant();
            weaponController.enabled = false;
        }
    
        private void TryPlace()
        {
            if (!photonView.IsMine) return;
            if (!carry.IsCarrying) return;

            if (!IsInsideBase())
            {
                Debug.Log("\u274c Ты должен быть внутри базы, чтобы посадить растение");
            }

            // Позиция перед игроком с привязкой к сетке
            Vector3 placePosition = transform.position + transform.forward * 2f;
            placePosition = carry.SnapToGrid(placePosition);
            Quaternion placeRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
    
            carry.PlacePlant(placePosition, placeRotation);
            weaponController.enabled = true;
        }
        
        private bool IsInsideBase()
        {
            BaseArea[] bases = FindObjectsByType<BaseArea>(FindObjectsSortMode.None);

            foreach (var b in bases)
            {
                if (b.Owner == photonView.OwnerActorNr)
                {
                    float dist = Vector3.Distance(transform.position, b.transform.position);
                    return dist <= b.Radius;
                }
            }

            return false;
        }
    }
}