using UnityEngine;
using Photon.Pun;
using Player.Components;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PhotonView))]
    public class PlayerMovement : MonoBehaviourPun
    {
        private PlayerInputActions _playerInputActions;
        private CharacterController _characterController;
        private float _verticalVelocity;
        private Vector2 _moveInput;

        [SerializeField] private Vector3 moveDirection;
        [SerializeField] private float gravityScale = 9.81f;
        [SerializeField] private float jumpForce = 5f;
        
        public float normalSpeed = 5f;
        public float carrySpeed = 3f;

        private CarryPlantAgent _carry;

        private void Awake()
        {
            _playerInputActions = new PlayerInputActions();

            _playerInputActions.Player.Jump.performed += _ => JumpHandler();

            _playerInputActions.Player.Movement.performed += context => _moveInput = context.ReadValue<Vector2>();
            _playerInputActions.Player.Movement.canceled += _ => _moveInput = Vector2.zero;
        }

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
            _carry = GetComponent <CarryPlantAgent>();
        }

        private void Update()
        {
            if (!photonView.IsMine) return;

            moveDirection = new Vector3(_moveInput.x, _verticalVelocity, _moveInput.y);

            if (moveDirection.magnitude > 0)
            {
                float currentSpeed = _carry.IsCarrying ? carrySpeed : normalSpeed;
                _characterController.Move(currentSpeed * Time.deltaTime * moveDirection );
            }

            ApplyGravity();
        }

        private void ApplyGravity()
        {
            if (_characterController.isGrounded)
            {
                _verticalVelocity = -0.5f;
            }
            else
            {
                _verticalVelocity -= gravityScale * Time.deltaTime;
            }
        }

        private void JumpHandler()
        {
            if (!photonView.IsMine) return; // только локальный игрок может прыгать
            if (_characterController.isGrounded)
                _verticalVelocity = jumpForce;
        }

        private void OnEnable()
        {
            if (photonView.IsMine)
                _playerInputActions.Enable();
        }

        private void OnDisable()
        {
            if (photonView.IsMine)
                _playerInputActions.Disable();
        }
        
        // Getter && Setter 
    }
}
