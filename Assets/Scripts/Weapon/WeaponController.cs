using UnityEngine;
using Weapon.Base;
using Weapon.Interfaces;

namespace Weapon
{
    public class WeaponController : MonoBehaviour
    {
        [Tooltip("Assign the current weapon (must have a class derived from Weapon)")]
        public Weapon.Base.Weapon currentWeapon;

        [Tooltip("Если true — стрельба удержанием кнопки, иначе — по нажатию")]
        public bool holdToFire = true;

        private PlayerInputActions _inputActions;

        private void Awake()
        {
            _inputActions = new PlayerInputActions();
        }

        private void OnEnable() => _inputActions.Enable();
        private void OnDisable() => _inputActions.Disable();

        private void Update()
        {
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