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
        [Header("References")]
        public CarryPlantAgent carry;
        public WeaponController weaponController;
        
        [Header("Interaction Settings")]
        public LayerMask plantLayer;
        public float interactRange = 3f;
        
        [Header("Visual Feedback")]
        public GameObject interactionHint;
        
        private PlayerInputActions _input;
        private Plant _potentialPlant;
        private bool _canInteract = true;

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
        
        private void Update()
        {
            if (!photonView.IsMine || !_canInteract) return;
            
            UpdateInteractionHint();
            CheckForNearbyPlants();
        }
    
        private void TryPickup()
        {
            if (!CanInteract()) return;
            if (carry.IsCarrying) return;
    
            if (_potentialPlant != null)
            {
                carry.PickupPlant(_potentialPlant);
                weaponController.enabled = false;
                _potentialPlant = null;
                UpdateInteractionHint();
            }
        }
    
        private void TryDrop()
        {
            if (!CanInteract()) return;
            
            carry.DropPlant();
            weaponController.enabled = true;
            UpdateInteractionHint();
        }
    
        private void TryPlace()
        {
            if (!CanInteract()) return;
            if (!carry.IsCarrying) return;

            if (!IsInsideBase())
            {
                Debug.Log("❌ Ты должен быть внутри базы, чтобы посадить растение");
                return;
            }

            Vector3 placePosition = GetPlacePosition();
            Quaternion placeRotation = GetPlaceRotation();
    
            if (carry.PlacePlant(placePosition, placeRotation))
            {
                weaponController.enabled = true;
            }
        }
        
        private void CheckForNearbyPlants()
        {
            if (carry.IsCarrying)
            {
                _potentialPlant = null;
                return;
            }
            
            Collider[] nearbyPlants = Physics.OverlapSphere(transform.position, interactRange, plantLayer);
            _potentialPlant = null;
            
            foreach (Collider col in nearbyPlants)
            {
                Plant plant = col.GetComponentInParent<Plant>();
                if (plant != null && IsPlantInFront(plant.transform))
                {
                    _potentialPlant = plant;
                    break;
                }
            }
        }
        
        private bool IsPlantInFront(Transform plantTransform)
        {
            Vector3 directionToPlant = (plantTransform.position - transform.position).normalized;
            float dotProduct = Vector3.Dot(transform.forward, directionToPlant);
            return dotProduct > 0.7f;
        }
        
        private void UpdateInteractionHint()
        {
            if (interactionHint == null) return;
            
            if (carry.IsCarrying)
            {
                interactionHint.SetActive(true);
                // Можно менять текст: "Press F to Place / Q to Drop"
            }
            else if (_potentialPlant != null)
            {
                interactionHint.SetActive(true);
                // "Press E to Pick Up"
            }
            else
            {
                interactionHint.SetActive(false);
            }
        }
        
        private Vector3 GetPlacePosition()
        {
            Vector3 basePosition = transform.position + transform.forward * 2f;
            
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 2f))
            {
                basePosition = hit.point - transform.forward * 0.5f;
            }
            
            return carry.SnapToGrid(basePosition);
        }
        
        private Quaternion GetPlaceRotation()
        {
            return Quaternion.Euler(0, transform.eulerAngles.y + 180f, 0);
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
        
        private bool CanInteract()
        {
            return photonView.IsMine && _canInteract;
        }
        
        public void SetInteractionEnabled(bool enabled)
        {
            _canInteract = enabled;
            if (interactionHint != null)
                interactionHint.SetActive(false);
        }
    }
}