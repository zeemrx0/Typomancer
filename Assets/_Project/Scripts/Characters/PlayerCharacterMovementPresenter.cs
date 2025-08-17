using ECM2;
using LNE.Inputs;
using PurrNet;
using UnityEngine;

namespace LNE.Characters
{
    public class PlayerCharacterMovementPresenter : NetworkBehaviour
    {
        private struct ReplicateData
        {
            public Vector2 MovementInput;
        }

        [SerializeField] private CharacterConfig _config;

        private Character _character;
        private PlayerInputPresenter _playerInputPresenter;
        private PlayerCharacterMovementView _view;
        private Camera _camera;

        private void Awake()
        {
            _character = GetComponent<Character>();
            _playerInputPresenter = GetComponent<PlayerInputPresenter>();
            _view = GetComponent<PlayerCharacterMovementView>();
            _camera = Camera.main;
        }

        protected override void OnSpawned()
        {
            if (!isOwner)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            ReplicateData replicateData = new()
            {
                MovementInput = _playerInputPresenter.MovementInput
            };

            if (isOwner)
            {
                HandleMovementServerRpc(replicateData);
            }
        }

        [ServerRpc]
        private void HandleMovementServerRpc(ReplicateData replicateData)
        {
            HandleMovement(replicateData);
        }

        private void HandleMovement(ReplicateData replicateData)
        {
            Vector2 movementInput = replicateData.MovementInput;
            Vector3 movementDirection = GetDirectionRelativeToCamera(movementInput, _camera);
            _character.SetMovementDirection(movementDirection);
            _view.SetWalkingSpeed(_character.velocity.magnitude);
        }

        private static Vector3 GetDirectionRelativeToCamera(Vector2 movementInput, Camera camera)
        {
            Vector3 movementDirection = movementInput.x * Vector3.right + movementInput.y * Vector3.forward;

            if (camera)
            {
                movementDirection = movementDirection.relativeTo(camera.transform);
            }

            return movementDirection;
        }
    }
}