using UnityEngine;
using UnityEngine.InputSystem;

namespace NetworkTankArena.Tanks
{
    [DisallowMultipleComponent]
    public sealed class LocalTankController : MonoBehaviour
    {
        [SerializeField] private TankSimulation tank;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private InputActionReference throttle;
        [SerializeField] private InputActionReference turn;
        [SerializeField] private InputActionReference aimPosition;

        private InputAction throttleAction;
        private InputAction turnAction;
        private InputAction aimPositionAction;
        private bool enabledThrottle;
        private bool enabledTurn;
        private bool enabledAimPosition;

        private void OnEnable()
        {
            throttleAction = throttle != null ? throttle.action : null;
            turnAction = turn != null ? turn.action : null;
            aimPositionAction = aimPosition != null ? aimPosition.action : null;

            if (tank == null || aimCamera == null || throttleAction == null || turnAction == null ||
                aimPositionAction == null || throttleAction.expectedControlType != "Axis" ||
                turnAction.expectedControlType != "Axis" || aimPositionAction.expectedControlType != "Vector2")
            {
                Debug.LogError("LocalTankController needs a tank, an aim camera, and Tank/Throttle (Axis), " +
                    "Tank/Turn (Axis), and Tank/AimPosition (Vector2) action references.", this);
                enabled = false;
                return;
            }

            // Project-wide actions may already be enabled. Preserve their existing lifecycle.
            // These three actions are intended for one active local controller.
            enabledThrottle = EnableIfNeeded(throttleAction);
            enabledTurn = EnableIfNeeded(turnAction);
            enabledAimPosition = EnableIfNeeded(aimPositionAction);
        }

        private void OnDisable()
        {
            if (enabledThrottle)
                throttleAction.Disable();
            if (enabledTurn)
                turnAction.Disable();
            if (enabledAimPosition)
                aimPositionAction.Disable();

            enabledThrottle = enabledTurn = enabledAimPosition = false;
        }

        private void Update()
        {
            if (tank == null)
                return;

            TankControlIntent intent = default;
            if (Application.isFocused)
            {
                bool hasAimPoint = TryGetAimPoint(out Vector3 aimPoint);
                intent = new TankControlIntent(throttleAction.ReadValue<float>(), turnAction.ReadValue<float>(),
                    aimPoint, hasAimPoint);
            }

            tank.Simulate(intent, Time.deltaTime);
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
