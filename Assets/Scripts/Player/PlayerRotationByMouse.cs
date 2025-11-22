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
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float aimSmoothTime = 0.08f;
        [SerializeField] private float rotationSmoothTime = 0.05f;

        private Vector3 _aimDirection = Vector3.forward;
        private Vector3 _smoothAimVelocity;

        private float _currentRotationVelocity;

        private void Start()
        {
            if (photonView.IsMine)
            {
                if (mainCamera == null)
                    mainCamera = Camera.main;
            }
            else
            {
                if (mainCamera != null)
                    mainCamera.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!photonView.IsMine) return;

            HandleRotation();

#if UNITY_EDITOR
            Debug.DrawLine(transform.position + Vector3.up * 0.1f,
                transform.position + Vector3.up * 0.1f + _aimDirection * 3f,
                Color.green);
#endif
        }

        private void HandleRotation()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                Vector3 targetDir = hit.point - transform.position;
                targetDir.y = 0;

                if (targetDir.sqrMagnitude < 0.01f)
                    return;

                // Направление без сглаживания (чтобы реагировало мгновенно)
                Vector3 aimDir = targetDir.normalized;

                // Вычисляем целевой угол
                float targetAngle = Mathf.Atan2(aimDir.x, aimDir.z) * Mathf.Rad2Deg;

                // Текущий угол
                float currentAngle = transform.eulerAngles.y;

                // Плавный поворот ИМЕННО угла
                float smoothedAngle = Mathf.LerpAngle(
                    currentAngle,
                    targetAngle,
                    rotationSpeed * Time.deltaTime
                );

                transform.rotation = Quaternion.Euler(0, smoothedAngle, 0);
                _aimDirection = aimDir;
            }
        }
    }
}
