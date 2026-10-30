using UnityEngine;
using UnityEngine.InputSystem;

namespace BubbleAscent.Gameplay
{
    /// <summary>
    /// The game's only control: "is it held?" (touch, mouse, space or gamepad).
    /// </summary>
    public class HoldInput : MonoBehaviour
    {
        InputAction _hold;

        public bool IsHeld => _hold != null && _hold.IsPressed();
        public bool WasPressedThisFrame => _hold != null && _hold.WasPressedThisFrame();

        void Awake()
        {
            _hold = new InputAction("Hold", InputActionType.Button);
            _hold.AddBinding("<Pointer>/press");
            _hold.AddBinding("<Keyboard>/space");
            _hold.AddBinding("<Gamepad>/buttonSouth");
        }

        void OnEnable() => _hold.Enable();
        void OnDisable() => _hold.Disable();
        void OnDestroy() => _hold.Dispose();
    }
}
