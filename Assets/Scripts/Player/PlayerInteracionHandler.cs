using AI.Plant;
using Gameplay;
using UnityEngine;
using Photon.Pun;
using Weapon;
using Gameplay;

namespace Player
{
    public class PlayerInteractionHandler : MonoBehaviourPun
    {
        [Header("References")]
        public WeaponController weaponController;
        
        [Header("Carry System")]
        public Transform carryPoint; 
        public LayerMask canPickUpLayer;
        
        [Header("Interaction Settings")]
        public float interactRange = 3f;
        
        [Header("Visual Feedback")]
        public GameObject interactionHint;
        
        private PlayerInputActions _input;
        private GameObject _potentialObject;
        private GameObject _carriedObject;
        private bool _isCarrying;
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
            CheckForNearbyInteractables();
            
            // Обновляем позицию переносимого объекта
            if (_isCarrying && _carriedObject != null)
            {
                _carriedObject.transform.position = carryPoint.position;
                _carriedObject.transform.rotation = carryPoint.rotation;
            }
        }
    
        private void TryPickup()
        {
            if (!CanInteract()) return;
            if (_isCarrying) return;

            if (_potentialObject != null)
            {
                PickupObject(_potentialObject);
                weaponController.enabled = false;
                _potentialObject = null;
            }
            
            UpdateInteractionHint();
        }
    
        private void TryDrop()
        {
            if (!CanInteract()) return;
            if (!_isCarrying) return;
            
            DropCarriedObject();
            weaponController.enabled = true;
            UpdateInteractionHint();
        }
    
        private void TryPlace()
        {
            if (!CanInteract()) return;
            if (!_isCarrying) return;

            // Проверяем можно ли посадить растение
            Plant plant = _carriedObject.GetComponent<Plant>();
            if (plant != null)
            {
                if (!IsInsideBase())
                {
                    Debug.Log("❌ Ты должен быть внутри базы, чтобы посадить растение");
                    return;
                }
                
                PlacePlant();
            }
            else
            {
                // Для обычных предметов - просто бросаем
                DropCarriedObject();
            }
            
            weaponController.enabled = true;
        }
        
        private void CheckForNearbyInteractables()
        {
            if (_isCarrying)
            {
                _potentialObject = null;
                return;
            }
            
            // ✅ Ищем все объекты на слое CanPickUp
            Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactRange, canPickUpLayer);
            _potentialObject = null;
            
            foreach (Collider col in nearbyObjects)
            {
                if (IsObjectInFront(col.transform))
                {
                    _potentialObject = col.gameObject;
                    break;
                }
            }
        }
        
        private bool IsObjectInFront(Transform objectTransform)
        {
            Vector3 directionToObject = (objectTransform.position - transform.position).normalized;
            float dotProduct = Vector3.Dot(transform.forward, directionToObject);
            return dotProduct > 0.7f;
        }
        
        private void UpdateInteractionHint()
        {
            if (interactionHint == null) return;
            
            if (_isCarrying)
            {
                interactionHint.SetActive(true);
                // Можно добавить разные тексты для разных объектов
                string objectName = _carriedObject.name;
                // "Несете: [ObjectName] (F - посадить, Q - бросить)"
            }
            else if (_potentialObject != null)
            {
                interactionHint.SetActive(true);
                // "Нажмите E чтобы поднять [ObjectName]"
            }
            else
            {
                interactionHint.SetActive(false);
            }
        }
        
        // ========== СИСТЕМА ПЕРЕНОСА ==========
        
        private void PickupObject(GameObject obj)
        {
            if (photonView.IsMine)
            {
                // ✅ Отключаем оружие при поднятии предмета
                if (weaponController != null)
                {
                    weaponController.SetWeaponEnabled(false);
                }
        
                photonView.RPC("RPC_PickupObject", RpcTarget.All, obj.GetComponent<PhotonView>().ViewID);
            }
        }
        
        [PunRPC] private void RPC_PickupObject(int objectViewID)
        {
            PhotonView objectView = PhotonView.Find(objectViewID);
            if (objectView == null) return;
            
            _carriedObject = objectView.gameObject;
            _isCarrying = true;
            
            // ✅ Получаем PickableObject для управления масштабом
            PickableObject pickable = _carriedObject.GetComponent<PickableObject>();
            
            // Отключаем физику
            Rigidbody rb = _carriedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            
            // Отключаем коллайдер
            Collider collider = _carriedObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            
            // Отключаем AI (если есть)
            Plant plantAI = _carriedObject.GetComponent<Plant>();
            if (plantAI != null)
            {
                plantAI.enabled = false;
            }
            
            // ✅ Сохраняем масштаб ПЕРЕД установкой родителя
            if (pickable != null)
            {
                pickable.SaveOriginalScale();
            }
            
            // Устанавливаем в точку переноса
            _carriedObject.transform.SetParent(carryPoint);
            _carriedObject.transform.localPosition = Vector3.zero;
            _carriedObject.transform.localRotation = Quaternion.identity;
            
            // ✅ Восстанавливаем оригинальный масштаб ПОСЛЕ установки родителя
            if (pickable != null)
            {
                pickable.RestoreOriginalScale();
            }
            else
            {
                // Резервный вариант для объектов без PickableObject
                _carriedObject.transform.localScale = Vector3.one;
            }
            
            Debug.Log($"Поднял объект: {_carriedObject.name}");
        }

        [PunRPC]
        private void RPC_DropObject()
        {
            if (_carriedObject == null) return;
            
            // ✅ Восстанавливаем масштаб ПЕРЕД отсоединением
            PickableObject pickable = _carriedObject.GetComponent<PickableObject>();
            if (pickable != null)
            {
                pickable.RestoreOriginalScale();
            }
            
            // Восстанавливаем физику
            Rigidbody rb = _carriedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            
            // Включаем коллайдер
            Collider collider = _carriedObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = true;
            }
            
            // Включаем AI (если есть)
            Plant plantAI = _carriedObject.GetComponent<Plant>();
            if (plantAI != null)
            {
                plantAI.enabled = true;
            }
            
            // Отсоединяем
            _carriedObject.transform.SetParent(null);
            
            // Бросаем объект перед игроком
            Vector3 dropPosition = transform.position + transform.forward * 2f;
            _carriedObject.transform.position = dropPosition;
            
            Debug.Log($"Бросил объект: {_carriedObject.name}");
            
            _carriedObject = null;
            _isCarrying = false;
        }
        
        private void DropCarriedObject()
        {
            if (photonView.IsMine)
            {
                // ✅ Включаем оружие при бросании предмета
                if (weaponController != null)
                {
                    weaponController.SetWeaponEnabled(true);
                }
        
                photonView.RPC("RPC_DropObject", RpcTarget.All);
            }
        }
        
        private void PlacePlant()
        {
            if (_carriedObject == null) return;
    
            Plant plant = _carriedObject.GetComponent<Plant>();
            if (plant == null) return;

            Vector3 placePosition = GetPlacePosition();
            Quaternion placeRotation = GetPlaceRotation();
    
            if (photonView.IsMine)
            {
                // ✅ Включаем оружие после посадки растения
                if (weaponController != null)
                {
                    weaponController.SetWeaponEnabled(true);
                }
        
                photonView.RPC("RPC_PlacePlant", RpcTarget.All, placePosition, placeRotation);
            }
        }
        
        [PunRPC]
        private void RPC_PlacePlant(Vector3 position, Quaternion rotation)
        {
            if (_carriedObject == null) return;
            
            // Устанавливаем растение на землю
            _carriedObject.transform.SetParent(null);
            _carriedObject.transform.position = position;
            _carriedObject.transform.rotation = rotation;
            
            // Включаем коллайдер
            Collider collider = _carriedObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = true;
            }
            
            // Включаем AI растения
            Plant plant = _carriedObject.GetComponent<Plant>();
            if (plant != null)
            {
                plant.enabled = true;
            }
            
            _carriedObject = null;
            _isCarrying = false;
            
            Debug.Log("Растение посажено!");
        }
        
        private Vector3 GetPlacePosition()
        {
            Vector3 basePosition = transform.position + transform.forward * 2f;
            
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 2f))
            {
                basePosition = hit.point - transform.forward * 0.5f;
            }
            
            return basePosition;
        }
        
        private Quaternion GetPlaceRotation()
        {
            return Quaternion.Euler(0, transform.eulerAngles.y + 180f, 0);
        }
        
        private bool IsInsideBase()
        {
            BaseArea[] bases = FindObjectsByType<BaseArea>(FindObjectsSortMode.None);

            foreach (var baseArea in bases)
            {
                if (baseArea.owner == photonView.OwnerActorNr)
                {
                    bool isInside = baseArea.IsPositionInsideBase(transform.position);
                    
                    if (isInside)
                    {
                        float distance = Vector3.Distance(transform.position, baseArea.transform.position);
                        Debug.Log($"✅ Внутри базы! Расстояние до центра: {distance:F1}, Радиус базы: {baseArea.baseRadius}");
                    }
            
                    return isInside;
                }
            }

            Debug.Log("❌ База не найдена для игрока");
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
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRange);
            
            if (carryPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(carryPoint.position, Vector3.one * 0.3f);
            }
        }
    }
}