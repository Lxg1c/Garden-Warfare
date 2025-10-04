namespace Player
{
    using UnityEngine;
    
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private PlayerInputActions _playerInputActions;
        private CharacterController _characterController;
        private float _verticalVelocity;
        private Vector2 _moveInput;
    
        [SerializeField] private Vector3 moveDirection;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float gravityScale = 9.81f;
        [SerializeField] private float jumpForce = 5f;
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
        }
    
        private void Update()
        {
            moveDirection = new Vector3(_moveInput.x, _verticalVelocity, _moveInput.y);
    
            if (moveDirection.magnitude > 0)
            {
                _characterController.Move(speed * Time.deltaTime * moveDirection);
            }
            
            Gravity();
        }
    
        private void Gravity()
        {
            if (_characterController.isGrounded)
            {
                _verticalVelocity = -0.5f;
            }
            else
            {
                _verticalVelocity = _verticalVelocity - gravityScale * Time.deltaTime; 
            }
        }
    
        private void JumpHandler()
        {
            _verticalVelocity += jumpForce;
        }
    
        private void OnEnable()
        {
            _playerInputActions.Enable();
        }
    
        private void OnDisable()
        {
            _playerInputActions.Disable();
        }
    }
}