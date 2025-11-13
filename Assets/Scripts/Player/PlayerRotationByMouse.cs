using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Player
{
    [RequireComponent(typeof(PhotonView))]
    public class PlayerRotationByMouse : MonoBehaviourPun
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float rotationSpeed = 10f;

        private void Start()
        {
            // Камера должна быть активна только у локального игрока
            if (photonView.IsMine)
            {
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }
            }
            else
            {
                // Удаляем/выключаем камеру у чужих игроков
                if (mainCamera != null)
                    mainCamera.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!photonView.IsMine) return;

            HandleRotation();
        }

        private void HandleRotation()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                Vector3 targetPoint = hit.point;

                Vector3 direction = (targetPoint - transform.position);
                direction.y = 0;

                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                Debug.DrawLine(transform.position, targetPoint, Color.red);
            }
        }
    }
}