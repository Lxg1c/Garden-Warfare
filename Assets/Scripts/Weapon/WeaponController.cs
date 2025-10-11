using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Tooltip("Assign the current weapon (must have a class derived from Weapon)")]
    public Weapon currentWeapon;

    [Tooltip("Если true — стрельба удержанием кнопки, иначе — по нажатию")]
    public bool holdToFire = true;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        if (currentWeapon == null) return;

        bool trigger = holdToFire
            ? inputActions.Player.Fire.IsPressed()
            : inputActions.Player.Fire.WasPressedThisFrame();

        if (trigger && currentWeapon.CanUse())
        {
            currentWeapon.Use();
        }

        if (inputActions.Player.Reload.WasPressedThisFrame() && currentWeapon is IReloadable reloadable)
        {
            reloadable.Reload();
        }
    }
}