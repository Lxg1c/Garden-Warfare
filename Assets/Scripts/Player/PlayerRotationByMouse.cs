using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Player
{
    [RequireComponent(typeof(PhotonView))]
    public class PlayerRotationByMouse : MonoBehaviourPun
    {
        [Header("Camera & Layers")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundLayer;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 15f;

        [Header("Aim Debug")]
        [SerializeField] private bool showAimDirection = true;
        [SerializeField] private float aimLineLength = 3f;
        [SerializeField] private Color aimLineColor = Color.green;

        private Vector3 _aimDirection = Vector3.forward;

        public Vector3 AimDirection => _aimDirection.normalized; // доступно для оружия

        private void Start()
        {
            if (photonView.IsMine)
            {
                if (mainCamera == null)
                    mainCamera = Camera.main;
            }
            else
            {
                // Убираем чужую камеру
                if (mainCamera != null)
                    mainCamera.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!photonView.IsMine) return;
            HandleRotation();
            if (showAimDirection)
                DrawAimDirection();
        }

        private void HandleRotation()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                // Цель в XZ-плоскости
                Vector3 targetPoint = hit.point;
                Vector3 flatDirection = targetPoint - transform.position;
                flatDirection.y = 0; // оставляем только горизонтальную составляющую

                if (flatDirection.sqrMagnitude > 0.001f)
                {
                    _aimDirection = flatDirection.normalized;

                    Quaternion targetRotation = Quaternion.LookRotation(_aimDirection, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }

        private void DrawAimDirection()
        {
            Vector3 start = transform.position + Vector3.up * 0.1f;
            Vector3 end = start + _aimDirection * aimLineLength;
            Debug.DrawLine(start, end, aimLineColor);
        }
    }
}
