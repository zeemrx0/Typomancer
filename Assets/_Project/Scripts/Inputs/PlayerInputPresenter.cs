using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LNE.Inputs
{
    public class PlayerInputPresenter : MonoBehaviour
    {
        public event Action<bool> JumpInputChanged;

        private PlayerInputAction _playerInputAction;
        private PlayerInputAction.CharacterActions _characterActions;
        private PlayerInputAction.SpellActions _spellActions;

        // Events for text input system
        public event Action ToggleSpell;
        public event Action SubmitWord;
        public event Action SubmitWordAndEndSpell;

        private void Awake()
        {
            _playerInputAction = new PlayerInputAction();
            _characterActions = _playerInputAction.Character;
            _spellActions = _playerInputAction.Spell;

            SetupInputCallbacks();
            _playerInputAction.Enable();
        }

        private void SetupInputCallbacks()
        {
            // Character actions
            _characterActions.ToggleSpell.performed += OnToggleSpellPerformed;
            _characterActions.Jump.performed += OnJumpPerformed;
            _characterActions.Jump.canceled += OnJumpCanceled;

            // Spell actions
            _spellActions.SubmitWord.started += OnSubmitWordPerformed;
            _spellActions.SubmitWordAndEndSpell.performed += OnSubmitWordAndEndSpellPerformed;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            JumpInputChanged?.Invoke(true);
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            JumpInputChanged?.Invoke(false);
        }

        private void OnToggleSpellPerformed(InputAction.CallbackContext context)
        {
            ToggleSpell?.Invoke();
        }

        private void OnSubmitWordPerformed(InputAction.CallbackContext context)
        {
            SubmitWord?.Invoke();
        }

        private void OnSubmitWordAndEndSpellPerformed(InputAction.CallbackContext context)
        {
            SubmitWordAndEndSpell?.Invoke();
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

        // Public methods to enable/disable specific action maps
        public void EnableCharacterActions() => _characterActions.Enable();
        public void DisableCharacterActions() => _characterActions.Disable();
        public void EnableSpellActions() => _spellActions.Enable();
        public void DisableSpellActions() => _spellActions.Disable();

        public void SetMoveActionActive(bool isActive)
        {
            if (isActive)
            {
                _characterActions.Move.Enable();
            }
            else
            {
                _characterActions.Move.Disable();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            _characterActions.ToggleSpell.performed -= OnToggleSpellPerformed;
            _spellActions.SubmitWord.performed -= OnSubmitWordPerformed;
            _spellActions.SubmitWordAndEndSpell.performed -= OnSubmitWordAndEndSpellPerformed;

            _playerInputAction.Dispose();
        }
    }
}