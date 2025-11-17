using Photon.Pun;
using Player.Components;
using UnityEngine;
using Weapon.Interfaces;

namespace Weapon
{
    public class WeaponController : MonoBehaviour
    {
        [Tooltip("Текущее оружие")]
        public Weapon.Base.Weapon currentWeapon;

        [Tooltip("Если true — стрельба удержанием кнопки, иначе — по нажатию")]
        public bool holdToFire = true;

        public CarryPlantAgent carry;

        private PlayerInputActions _inputActions;
        private PhotonView _pv;

        private void Awake()
        {
            _pv = GetComponent<PhotonView>();
            _inputActions = new PlayerInputActions();
        }

        private void OnEnable() => _inputActions.Enable();
        private void OnDisable() => _inputActions.Disable();

        private void Update()
        {
            if (!_pv.IsMine) return;
            
            if (carry != null && carry.IsCarrying) return;
            
            if (currentWeapon == null) return;

            bool trigger = holdToFire
                ? _inputActions.Player.Fire.IsPressed()
                : _inputActions.Player.Fire.WasPressedThisFrame();

            if (trigger && currentWeapon.CanUse())
            {
                currentWeapon.Use();
            }

            if (_inputActions.Player.Reload.WasPressedThisFrame() && currentWeapon is IReloadable reloadable)
            {
                reloadable.Reload();
            }
        }
    }
}