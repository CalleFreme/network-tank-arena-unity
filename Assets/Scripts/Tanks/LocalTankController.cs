using NetworkTankArena.Combat;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace NetworkTankArena.Tanks
{
    [DisallowMultipleComponent]
    public sealed class LocalTankController : MonoBehaviour
    {
        [SerializeField] private TankSimulation tank;
        [SerializeField] private TankWeapon weapon;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private InputActionReference throttle;
        [SerializeField] private InputActionReference turn;
        [SerializeField] private InputActionReference aimPosition;
        [SerializeField] private InputActionReference fire;

        private InputAction throttleAction;
        private InputAction turnAction;
        private InputAction aimPositionAction;
        private InputAction fireAction;
        private bool enabledThrottle;
        private bool enabledTurn;
        private bool enabledAimPosition;
        private bool enabledFire;
        private readonly FireRequestLatch fireLatch = new FireRequestLatch();

        private void OnEnable()
        {
            fireLatch.RequireRelease();
            throttleAction = throttle != null ? throttle.action : null;
            turnAction = turn != null ? turn.action : null;
            aimPositionAction = aimPosition != null ? aimPosition.action : null;
            fireAction = fire != null ? fire.action : null;

            if (tank == null || weapon == null || weapon.Tank != tank || aimCamera == null ||
                throttleAction == null || turnAction == null || aimPositionAction == null || fireAction == null ||
                throttleAction.expectedControlType != "Axis" || turnAction.expectedControlType != "Axis" ||
                aimPositionAction.expectedControlType != "Vector2" || fireAction.type != InputActionType.Button ||
                fireAction.expectedControlType != "Button")
            {
                Debug.LogError("LocalTankController needs a tank, its weapon, an aim camera, and " +
                    "Tank/Throttle (Axis), Tank/Turn (Axis), Tank/AimPosition (Vector2), and " +
                    "Tank/Fire (Button) action references.", this);
                enabled = false;
                return;
            }

            // Project-wide actions may already be enabled. Preserve their existing lifecycle.
            // These actions are intended for one active local controller.
            enabledThrottle = EnableIfNeeded(throttleAction);
            enabledTurn = EnableIfNeeded(turnAction);
            enabledAimPosition = EnableIfNeeded(aimPositionAction);
            enabledFire = EnableIfNeeded(fireAction);
        }

        private void OnDisable()
        {
            fireLatch.RequireRelease();
            if (enabledThrottle)
                throttleAction.Disable();
            if (enabledTurn)
                turnAction.Disable();
            if (enabledAimPosition)
                aimPositionAction.Disable();
            if (enabledFire)
                fireAction.Disable();

            enabledThrottle = enabledTurn = enabledAimPosition = enabledFire = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                fireLatch.RequireRelease();
        }

        private void Update()
        {
            if (tank == null)
                return;

            TankControlIntent intent = default;
            if (Application.isFocused)
            {
                bool hasAimPoint = TryGetAimPoint(out Vector3 aimPoint);
                bool canRequestFire = hasAimPoint && Time.timeScale > 0f && Time.deltaTime > 0f &&
                    throttleAction.enabled && turnAction.enabled;
                bool fireRequested = TryReadFireHeld(out bool fireHeld) &&
                    fireLatch.TryRequest(fireHeld, fireAction.WasPressedThisFrame(), canRequestFire);
                intent = new TankControlIntent(throttleAction.ReadValue<float>(), turnAction.ReadValue<float>(),
                    aimPoint, hasAimPoint, fireRequested);
            }
            else
            {
                fireLatch.RequireRelease();
            }

            tank.Simulate(intent, Time.deltaTime);
            // Canonical aim is applied before consuming this request. Rejected clicks are never retained.
            if (intent.FireRequested && weapon != null && weapon.Tank == tank)
                weapon.TryFire();
        }

        private bool TryReadFireHeld(out bool held)
        {
            held = false;
            if (!fireAction.enabled)
                return false;

            // With initial-state checking off, a newly enabled action can report unpressed
            // while its physical button is still held. Observe the bound controls directly.
            bool hasEnabledButton = false;
            foreach (InputControl control in fireAction.controls)
            {
                if (control is ButtonControl button && button.device.enabled)
                {
                    hasEnabledButton = true;
                    held |= button.isPressed;
                }
            }
            return hasEnabledButton;
        }

        private bool TryGetAimPoint(out Vector3 aimPoint)
        {
            aimPoint = default;
            if (aimCamera == null || !aimCamera.isActiveAndEnabled || !aimPositionAction.enabled ||
                aimPositionAction.controls.Count == 0)
                return false;

            Vector2 screenPoint = aimPositionAction.ReadValue<Vector2>();
            if (!aimCamera.pixelRect.Contains(screenPoint))
                return false;

            Ray ray = aimCamera.ScreenPointToRay(screenPoint);
            Plane plane = new Plane(Vector3.up, tank.TurretOrigin);
            if (!plane.Raycast(ray, out float distance))
                return false;

            aimPoint = ray.GetPoint(distance);
            return true;
        }

        private static bool EnableIfNeeded(InputAction action)
        {
            bool wasDisabled = !action.enabled;
            if (wasDisabled)
                action.Enable();
            return wasDisabled;
        }
    }
}
