using UnityEngine;
using UnityEngine.InputSystem;

namespace LNE.Inputs
{
    public class PlayerInputPresenter : MonoBehaviour
    {
        private PlayerInputAction _playerInputAction;
        private PlayerInputAction.CharacterActions _characterActions;

        private void Awake()
        {
            _playerInputAction = new PlayerInputAction();
            _characterActions = _playerInputAction.Character;
            _playerInputAction.Enable();
        }

        public Vector2 MovementInput => _characterActions.Move.ReadValue<Vector2>();

        public bool IsGamepadInput(InputAction inputAction)
        {
            // Check if the last used device for the Look action was a gamepad
            if (inputAction is { activeControl: not null })
            {
                return inputAction.activeControl.device is Gamepad;
            }

            // Fallback: check if any gamepad is currently connected and being used
            return Gamepad.current != null && Gamepad.current.rightStick.IsActuated();
        }
    }
}