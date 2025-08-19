using System;
using ImprovedTimers;
using LNE.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityHFSM;

namespace LNE.Characters
{
    public static class CharacterMovementState
    {
        public const string Grounded = "Grounded";
        public const string Falling = "Falling";
        public const string Sliding = "Sliding";
        public const string Rising = "Rising";
        public const string Jumping = "Jumping";
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class CharacterMovementController : MonoBehaviour
    {
        #region Events

        public event Action<Vector3> OnJump;
        public event Action<Vector3> OnLand;

        #endregion

        #region Serialized Fields

        [SerializeField] private bool _isDebugging;

        [TitleGroup("Character Movement Settings")] [SerializeField, Required]
        private CharacterMovementConfig _characterMovementConfig;

        private const string ColliderSettings = "Collider Settings";

        [TitleGroup(ColliderSettings)] [Range(0f, 1f)] [SerializeField]
        private float _stepHeightRatio = 0.1f;

        [TitleGroup(ColliderSettings)] [SerializeField]
        private float _colliderHeight = 2f;

        [TitleGroup(ColliderSettings)] [SerializeField]
        private float _colliderThickness = 1f;

        [TitleGroup(ColliderSettings)] [SerializeField]
        private Vector3 _colliderOffset = Vector3.zero;

        #endregion

        #region Component References

        private Transform _transform;
        private Rigidbody _rigidbody;
        private CapsuleCollider _capsuleCollider;
        private RaycastSensor _raycastSensor;

        #endregion

        #region Input Fields

        // Indicates if the jump key was pressed since the last reset, used to detect jump initiation
        public bool ShouldStartJump { get; set; }

        // Indicates if the jump key was released since it was last pressed, used to detect when to stop jumping
        public bool ShouldStopJump { get; set; }

        // Prevents jump initiation when true, used to ensure only one jump action per press
        public bool IsJumpLocked { get; set; }

        #endregion

        #region State Machine & Movement Fields

        private StateMachine _fsm;
        private CountdownTimer _jumpTimer;

        private Vector3 _movementDirection;
        private Vector3 _momentum;
        private Vector3 _savedVelocity;
        private Vector3 _savedMovementVelocity;

        #endregion

        #region Ground Detection Fields

        private bool _isGrounded;
        private float _baseSensorRange;
        private Vector3 _currentGroundAdjustmentVelocity;
        private int _currentLayer;
        private bool _isUsingExtendedSensorRange = true;

        #endregion

        #region Public API

        public bool IsGrounded() =>
            _fsm.ActiveStateName is CharacterMovementState.Grounded or CharacterMovementState.Sliding;

        public bool IsGroundedPhysics() => _isGrounded;
        public Vector3 GetGroundNormal() => _raycastSensor.GetNormal();
        public Vector3 GetVelocity() => _savedVelocity;
        public Vector3 GetMovementVelocity() => _savedMovementVelocity;

        public Vector3 GetMomentum() =>
            _characterMovementConfig.UseLocalMomentum ? _transform.localToWorldMatrix * _momentum : _momentum;

        public void SetMovementDirection(Vector3 movementDirection)
        {
            _movementDirection = movementDirection;
        }

        public void SetVelocity(Vector3 velocity) =>
            _rigidbody.linearVelocity = velocity + _currentGroundAdjustmentVelocity;

        public void SetExtendSensorRange(bool isExtended) => _isUsingExtendedSensorRange = isExtended;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _transform = transform;
            Setup();
            RecalculateColliderDimensions();

            _jumpTimer = new CountdownTimer(_characterMovementConfig.JumpDuration);
            SetupStateMachine();
        }

        private void OnValidate()
        {
            if (gameObject.activeInHierarchy)
            {
                RecalculateColliderDimensions();
            }
        }

        private void LateUpdate()
        {
            if (_isDebugging)
            {
                _raycastSensor.DrawDebug();
            }
        }

        private void FixedUpdate()
        {
            _fsm.OnLogic();
            CheckForGround();
            HandleMomentum();
            Vector3 velocity = _fsm.ActiveStateName == CharacterMovementState.Grounded
                ? CalculateMovementVelocity()
                : Vector3.zero;
            velocity += _characterMovementConfig.UseLocalMomentum
                ? _transform.localToWorldMatrix * _momentum
                : _momentum;

            SetExtendSensorRange(IsGrounded());
            SetVelocity(velocity);

            _savedVelocity = velocity;
            _savedMovementVelocity = CalculateMovementVelocity();

            ResetJumpKeys();
        }

        #endregion

        #region State Machine Setup

        private void SetupStateMachine()
        {
            _fsm = new StateMachine();

            // Add states
            _fsm.AddState(CharacterMovementState.Grounded, onEnter: _ => OnGroundContactRegained());
            _fsm.AddState(CharacterMovementState.Falling, onEnter: _ => OnFallStart());
            _fsm.AddState(CharacterMovementState.Sliding, onEnter: _ => OnGroundContactLost());
            _fsm.AddState(CharacterMovementState.Rising, onEnter: _ => OnGroundContactLost());
            _fsm.AddState(
                CharacterMovementState.Jumping,
                onEnter: _ =>
                {
                    OnGroundContactLost();
                    OnJumpStart();
                }
            );

            // Grounded state transitions
            _fsm.AddTransition(
                CharacterMovementState.Grounded,
                CharacterMovementState.Rising,
                _ => IsRising()
            );
            _fsm.AddTransition(
                CharacterMovementState.Grounded,
                CharacterMovementState.Sliding,
                _ => _isGrounded && IsGroundTooSteep()
            );
            _fsm.AddTransition(
                CharacterMovementState.Grounded,
                CharacterMovementState.Falling,
                _ => !_isGrounded
            );
            _fsm.AddTransition(
                CharacterMovementState.Grounded,
                CharacterMovementState.Jumping,
                _ => ShouldStartJump && !IsJumpLocked
            );

            // Falling state transitions
            _fsm.AddTransition(
                CharacterMovementState.Falling,
                CharacterMovementState.Rising,
                _ => IsRising()
            );
            _fsm.AddTransition(
                CharacterMovementState.Falling,
                CharacterMovementState.Grounded,
                _ => _isGrounded && !IsGroundTooSteep()
            );
            _fsm.AddTransition(
                CharacterMovementState.Falling,
                CharacterMovementState.Sliding,
                _ => _isGrounded && IsGroundTooSteep()
            );

            // Sliding state transitions
            _fsm.AddTransition(
                CharacterMovementState.Sliding,
                CharacterMovementState.Rising,
                _ => IsRising()
            );
            _fsm.AddTransition(
                CharacterMovementState.Sliding,
                CharacterMovementState.Falling,
                _ => !_isGrounded
            );
            _fsm.AddTransition(
                CharacterMovementState.Sliding,
                CharacterMovementState.Grounded,
                _ => _isGrounded && !IsGroundTooSteep()
            );

            // Rising state transitions
            _fsm.AddTransition(
                CharacterMovementState.Rising,
                CharacterMovementState.Grounded,
                _ => _isGrounded && !IsGroundTooSteep()
            );
            _fsm.AddTransition(
                CharacterMovementState.Rising,
                CharacterMovementState.Sliding,
                _ => _isGrounded && IsGroundTooSteep()
            );
            _fsm.AddTransition(
                CharacterMovementState.Rising,
                CharacterMovementState.Falling,
                _ => IsFalling()
            );

            // Jumping state transitions
            _fsm.AddTransition(
                CharacterMovementState.Jumping,
                CharacterMovementState.Rising,
                _ => _jumpTimer.IsFinished || ShouldStopJump
            );

            _fsm.SetStartState(CharacterMovementState.Falling);
            _fsm.Init();
        }

        #endregion

        #region State Condition Helpers

        private bool IsRising() => VectorMath.GetDotProduct(GetMomentum(), _transform.up) > 0f;
        private bool IsFalling() => VectorMath.GetDotProduct(GetMomentum(), _transform.up) < 0f;

        private bool IsGroundTooSteep() => !_isGrounded ||
                                           Vector3.Angle(_raycastSensor.GetNormal(), _transform.up) >
                                           _characterMovementConfig.SlopeLimit;

        #endregion

        #region Movement Calculation

        private Vector3 CalculateMovementVelocity() => _movementDirection * _characterMovementConfig.MovementSpeed;

        private void HandleMomentum()
        {
            if (_characterMovementConfig.UseLocalMomentum)
            {
                _momentum = _transform.localToWorldMatrix * _momentum;
            }

            Vector3 verticalMomentum = VectorMath.ExtractDotVector(_momentum, _transform.up);
            Vector3 horizontalMomentum = _momentum - verticalMomentum;

            // Apply gravity to vertical momentum
            verticalMomentum -= _transform.up * (_characterMovementConfig.Gravity * Time.deltaTime);
            if (_fsm.ActiveStateName == CharacterMovementState.Grounded &&
                VectorMath.GetDotProduct(verticalMomentum, _transform.up) < 0f)
            {
                verticalMomentum = Vector3.zero;
            }

            // Handle air control
            if (!IsGrounded())
            {
                AdjustHorizontalMomentum(ref horizontalMomentum, CalculateMovementVelocity());
            }

            // Handle sliding movement
            if (_fsm.ActiveStateName == CharacterMovementState.Sliding)
            {
                HandleSliding(ref horizontalMomentum);
            }

            // Apply friction
            float friction = _fsm.ActiveStateName == CharacterMovementState.Grounded
                ? _characterMovementConfig.GroundFriction
                : _characterMovementConfig.AirFriction;
            horizontalMomentum = Vector3.MoveTowards(horizontalMomentum, Vector3.zero, friction * Time.deltaTime);

            _momentum = horizontalMomentum + verticalMomentum;

            // Handle jumping
            if (_fsm.ActiveStateName == CharacterMovementState.Jumping)
            {
                HandleJumping();
            }

            // Handle sliding physics
            if (_fsm.ActiveStateName == CharacterMovementState.Sliding)
            {
                _momentum = Vector3.ProjectOnPlane(_momentum, _raycastSensor.GetNormal());
                if (VectorMath.GetDotProduct(_momentum, _transform.up) > 0f)
                {
                    _momentum = VectorMath.RemoveDotVector(_momentum, _transform.up);
                }

                Vector3 slideDirection =
                    Vector3.ProjectOnPlane(-_transform.up, _raycastSensor.GetNormal()).normalized;
                _momentum += slideDirection * (_characterMovementConfig.SlideGravity * Time.deltaTime);
            }

            if (_characterMovementConfig.UseLocalMomentum)
            {
                _momentum = _transform.worldToLocalMatrix * _momentum;
            }
        }

        private void AdjustHorizontalMomentum(ref Vector3 horizontalMomentum, Vector3 movementVelocity)
        {
            if (horizontalMomentum.magnitude > _characterMovementConfig.MovementSpeed)
            {
                if (VectorMath.GetDotProduct(movementVelocity, horizontalMomentum.normalized) > 0f)
                {
                    movementVelocity = VectorMath.RemoveDotVector(movementVelocity, horizontalMomentum.normalized);
                }

                horizontalMomentum +=
                    movementVelocity * (Time.deltaTime * _characterMovementConfig.AirControlRate * 0.25f);
            }
            else
            {
                horizontalMomentum += movementVelocity * (Time.deltaTime * _characterMovementConfig.AirControlRate);
                horizontalMomentum = Vector3.ClampMagnitude(horizontalMomentum, _characterMovementConfig.MovementSpeed);
            }
        }

        private void HandleSliding(ref Vector3 horizontalMomentum)
        {
            Vector3 pointDownVector =
                Vector3.ProjectOnPlane(_raycastSensor.GetNormal(), _transform.up).normalized;
            Vector3 movementVelocity = CalculateMovementVelocity();
            movementVelocity = VectorMath.RemoveDotVector(movementVelocity, pointDownVector);
            horizontalMomentum += movementVelocity * Time.fixedDeltaTime;
        }

        #endregion

        #region Jump Handling

        private void HandleJumping()
        {
            _momentum = VectorMath.RemoveDotVector(_momentum, _transform.up);
            _momentum += _transform.up * _characterMovementConfig.JumpSpeed;
        }

        private void ResetJumpKeys()
        {
            ShouldStopJump = false;
            ShouldStartJump = false;
        }

        private void OnJumpStart()
        {
            if (_characterMovementConfig.UseLocalMomentum)
            {
                _momentum = _transform.localToWorldMatrix * _momentum;
            }

            _momentum += _transform.up * _characterMovementConfig.JumpSpeed;
            _jumpTimer.Start();
            IsJumpLocked = true;
            OnJump?.Invoke(_momentum);

            if (_characterMovementConfig.UseLocalMomentum)
            {
                _momentum = _transform.worldToLocalMatrix * _momentum;
            }
        }

        #endregion

        #region State Event Handlers

        private void OnGroundContactLost()
        {
            if (_characterMovementConfig.UseLocalMomentum)
            {
                _momentum = _transform.localToWorldMatrix * _momentum;
            }

            Vector3 velocity = GetMovementVelocity();
            if (velocity.sqrMagnitude >= 0f && _momentum.sqrMagnitude > 0f)
            {
                Vector3 projectedMomentum = Vector3.Project(_momentum, velocity.normalized);
                float dot = VectorMath.GetDotProduct(projectedMomentum.normalized, velocity.normalized);

                if (projectedMomentum.sqrMagnitude >= velocity.sqrMagnitude && dot > 0f)
                {
                    velocity = Vector3.zero;
                }
                else if (dot > 0f)
                {
                    velocity -= projectedMomentum;
                }
            }

            _momentum += velocity;

            if (_characterMovementConfig.UseLocalMomentum)
            {
                _momentum = _transform.worldToLocalMatrix * _momentum;
            }
        }

        private void OnGroundContactRegained()
        {
            Vector3 collisionVelocity = _characterMovementConfig.UseLocalMomentum
                ? _transform.localToWorldMatrix * _momentum
                : _momentum;
            OnLand?.Invoke(collisionVelocity);
        }

        private void OnFallStart()
        {
            Vector3 currentUpMomentum = VectorMath.ExtractDotVector(_momentum, _transform.up);
            _momentum = VectorMath.RemoveDotVector(_momentum, _transform.up);
            _momentum -= _transform.up * currentUpMomentum.magnitude;
        }

        #endregion

        #region Ground Detection

        private void CheckForGround()
        {
            if (_currentLayer != gameObject.layer)
            {
                RecalculateSensorLayerMask();
            }

            _currentGroundAdjustmentVelocity = Vector3.zero;
            _raycastSensor.CastLength = _isUsingExtendedSensorRange
                ? _baseSensorRange + _colliderHeight * _transform.localScale.x * _stepHeightRatio
                : _baseSensorRange;
            _raycastSensor.Cast();

            _isGrounded = _raycastSensor.HasDetectedHit();
            if (!_isGrounded)
            {
                return;
            }

            float distance = _raycastSensor.GetDistance();
            float upperLimit = _colliderHeight * _transform.localScale.x * (1f - _stepHeightRatio) * 0.5f;
            float middle = upperLimit + _colliderHeight * _transform.localScale.x * _stepHeightRatio;
            float distanceToGo = middle - distance;

            _currentGroundAdjustmentVelocity = _transform.up * (distanceToGo / Time.fixedDeltaTime);
        }

        #endregion

        #region Component Setup & Initialization

        private void Setup()
        {
            _transform = transform;
            _rigidbody = GetComponent<Rigidbody>();
            _capsuleCollider = GetComponent<CapsuleCollider>();

            _rigidbody.freezeRotation = true;
            _rigidbody.useGravity = false;
        }

        private void RecalculateColliderDimensions()
        {
            if (!_capsuleCollider)
            {
                Setup();
            }

            _capsuleCollider.height = _colliderHeight * (1f - _stepHeightRatio);
            _capsuleCollider.radius = _colliderThickness / 2f;
            _capsuleCollider.center = _colliderOffset * _colliderHeight + new Vector3(
                0f,
                _stepHeightRatio * _capsuleCollider.height / 2f,
                0f
            );

            if (_capsuleCollider.height / 2f < _capsuleCollider.radius)
            {
                _capsuleCollider.radius = _capsuleCollider.height / 2f;
            }

            RecalibrateSensor();
        }

        private void RecalibrateSensor()
        {
            _raycastSensor ??= new RaycastSensor(_transform);

            _raycastSensor.SetCastOrigin(_capsuleCollider.bounds.center);
            _raycastSensor.SetCastDirection(RaycastDirection.Down);
            RecalculateSensorLayerMask();

            // Small factor added to prevent clipping issues when the sensor range is calculated
            const float safetyDistanceFactor = 0.001f;

            float length = _colliderHeight * (1f - _stepHeightRatio) * 0.5f + _colliderHeight * _stepHeightRatio;
            _baseSensorRange = length * (1f + safetyDistanceFactor) * _transform.localScale.x;
            _raycastSensor.CastLength = length * _transform.localScale.x;
        }

        private void RecalculateSensorLayerMask()
        {
            int objectLayer = gameObject.layer;
            int layerMask = Physics.AllLayers;

            for (int i = 0; i < 32; i++)
            {
                if (Physics.GetIgnoreLayerCollision(objectLayer, i))
                {
                    layerMask &= ~(1 << i);
                }
            }

            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            layerMask &= ~(1 << ignoreRaycastLayer);

            _raycastSensor.LayerMask = layerMask;
            _currentLayer = objectLayer;
        }

        #endregion
    }
}