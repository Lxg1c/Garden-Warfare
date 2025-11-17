using UnityEngine;
using Photon.Pun;
using Player.Components;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(Animator))]
    public class PlayerMovement : MonoBehaviourPun, IPunObservable
    {
        private PlayerInputActions _playerInputActions;
        private CharacterController _characterController;
        private Animator _animator;

        private float _verticalVelocity;
        private Vector2 _moveInput;
        [SerializeField] private Vector3 moveDirection;

        [SerializeField] private float gravityScale = 9.81f;
        [SerializeField] private float jumpForce = 5f;

        public float normalSpeed = 5f;
        public float carrySpeed = 3f;

        private CarryPlantAgent _carry;

        // Плавное смешивание анимаций 
        private float _animMoveX;
        private float _animMoveY;
        private readonly float _animationSmooth = 10f;

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
            _animator = GetComponent<Animator>();
            _carry = GetComponent<CarryPlantAgent>();
        }

        private void Update()
        {
            if (!photonView.IsMine) return;

            // горизонтальное движение + вертикальная скорость в одном векторе
            moveDirection = new Vector3(_moveInput.x, _verticalVelocity, _moveInput.y);

            // движение только по горизонтали
            Vector3 horizontal = new Vector3(moveDirection.x, 0, moveDirection.z);

            if (horizontal.magnitude > 0)
            {
                float currentSpeed = _carry != null && _carry.IsCarrying ? carrySpeed : normalSpeed;
                _characterController.Move(currentSpeed * Time.deltaTime * horizontal);
            }

            ApplyGravity();

            // применяем вертикальное движение
            _characterController.Move(new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);

            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            Vector3 worldMove = new Vector3(_moveInput.x, 0, _moveInput.y);
            Vector3 localMove = transform.InverseTransformDirection(worldMove);

            localMove.Normalize();

            _animMoveX = Mathf.Lerp(_animMoveX, localMove.x, Time.deltaTime * _animationSmooth);
            _animMoveY = Mathf.Lerp(_animMoveY, localMove.z, Time.deltaTime * _animationSmooth);

            _animator.SetFloat("moveX", _animMoveX);
            _animator.SetFloat("moveY", _animMoveY);
            _animator.SetFloat("speed", worldMove.magnitude);
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(_animMoveX);
                stream.SendNext(_animMoveY);
                stream.SendNext(_animator.GetFloat("speed"));
            }
            else
            {
                _animMoveX = (float)stream.ReceiveNext();
                _animMoveY = (float)stream.ReceiveNext();
                float speed = (float)stream.ReceiveNext();

                _animator.SetFloat("moveX", _animMoveX);
                _animator.SetFloat("moveY", _animMoveY);
                _animator.SetFloat("speed", speed);
            }
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
            if (!photonView.IsMine) return;
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
    }
}
