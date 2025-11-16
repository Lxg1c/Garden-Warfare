using UnityEngine;
using Photon.Pun;

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

        [SerializeField] private float speed = 5f;
        [SerializeField] private float gravityScale = 9.81f;
        [SerializeField] private float jumpForce = 5f;

        // Плавное смешивание анимаций 
        private float _animMoveX;
        private float _animMoveY;
        private readonly float _animationSmooth = 10f; // скорость сглаживания

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
        }

        private void Update()
        {
            if (!photonView.IsMine) return;

            moveDirection = new Vector3(_moveInput.x, _verticalVelocity, _moveInput.y);

            //  Движение
            if (new Vector3(_moveInput.x, 0, _moveInput.y).magnitude > 0)
            {
                Vector3 worldMove = new Vector3(_moveInput.x, 0, _moveInput.y);
                _characterController.Move(speed * Time.deltaTime * worldMove);
            }

            ApplyGravity();

            //  Анимация 
            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            // направление движения в мировых координатах
            Vector3 worldMove = new Vector3(_moveInput.x, 0, _moveInput.y);

            // переводим в локальные координаты персонажа
            Vector3 localMove = transform.InverseTransformDirection(worldMove);

            localMove.Normalize();

            //  Плавное приближение к целевым значениям 
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
                // отправляем параметры
                stream.SendNext(_animMoveX);
                stream.SendNext(_animMoveY);
                stream.SendNext(_animator.GetFloat("speed"));
            }
            else
            {
                // получаем параметры
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
    }
}
