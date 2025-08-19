using LNE.Inputs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LNE.Characters
{
    public class PlayerCharacterMovementController : MonoBehaviour
    {
        [SerializeField, Required] private PlayerInputPresenter _playerInputPresenter;
        [SerializeField] private CharacterMovementController _characterMovementController;
        [SerializeField] private Transform _cameraTransform;

        private bool _isJumpInputPressed;

        private void Start()
        {
            _playerInputPresenter.JumpInputChanged += OnJumpInputChanged;
        }

        private void FixedUpdate()
        {
            _characterMovementController.SetMovementDirection(GetMovementDirection());
        }

        private void OnJumpInputChanged(bool isPressed)
        {
            if (isPressed && !_isJumpInputPressed)
            {
                _characterMovementController.ShouldStartJump = true;
            }
            else if (!isPressed && _isJumpInputPressed)
            {
                _characterMovementController.ShouldStopJump = true;
                _characterMovementController.IsJumpLocked = false;
            }

            _isJumpInputPressed = isPressed;
        }

        private Vector3 GetMovementDirection()
        {
            Vector3 direction = !_cameraTransform
                ? transform.right * _playerInputPresenter.MovementInput.x +
                  transform.forward * _playerInputPresenter.MovementInput.y
                : Vector3.ProjectOnPlane(_cameraTransform.right, transform.up).normalized *
                  _playerInputPresenter.MovementInput.x +
                  Vector3.ProjectOnPlane(_cameraTransform.forward, transform.up).normalized *
                  _playerInputPresenter.MovementInput.y;

            return direction.magnitude > 1f ? direction.normalized : direction;
        }
    }
}