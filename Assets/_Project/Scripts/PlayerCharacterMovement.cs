using ECM2;
using LNE.Inputs;
using PurrNet;
using UnityEngine;

namespace LNE
{
    public class PlayerCharacterMovement : NetworkBehaviour
    {
        private struct ReplicateData
        {
            public Vector2 MovementInput;
        }

        [SerializeField] private CharacterConfig _config;

        private CharacterMovement _characterMovement;
        private PlayerInputPresenter _playerInputPresenter;

        private void Awake()
        {
            _characterMovement = GetComponent<CharacterMovement>();
            _playerInputPresenter = GetComponent<PlayerInputPresenter>();
        }

        protected override void OnSpawned()
        {
            enabled = isOwner;
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
            float actualAcceleration = _characterMovement.isGrounded
                ? _config.MaxAcceleration
                : _config.MaxAcceleration * _config.AirControl;
            float actualDeceleration = _characterMovement.isGrounded ? _config.BrakingDeceleration : 0.0f;

            float actualFriction =
                _characterMovement.isGrounded ? _config.GroundFriction : _config.AirFriction;

            Vector2 movementInput = replicateData.MovementInput;

            float deltaTime = Time.deltaTime;

            float maxMovementSpeed = _config.MaxWalkingSpeed;
            Vector3 movementDirection = movementInput.x * transform.right + movementInput.y * transform.forward;
            Vector3 desiredVelocity = movementDirection * maxMovementSpeed;

            _characterMovement.SimpleMove(
                desiredVelocity,
                maxMovementSpeed,
                actualAcceleration,
                actualDeceleration,
                actualFriction,
                actualFriction,
                _config.Gravity,
                true,
                deltaTime
            );
        }
    }
}