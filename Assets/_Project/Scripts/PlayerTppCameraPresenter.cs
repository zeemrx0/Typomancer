using LNE.Inputs;
using PurrNet;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

namespace LNE
{
    public class PlayerTppCameraPresenter : NetworkBehaviour
    {
        #region Serialized Fields

        #region Camera Setup

        [TitleGroup("Camera Setup")] [SerializeField]
        private GameObject _tppCameraPrefab;

        [TitleGroup("Camera Setup")] [SerializeField]
        private Transform _cameraTarget;

        #endregion

        #endregion

        private PlayerInputPresenter _playerInputPresenter;
        private GameObject _camera;

        private void Awake()
        {
            _playerInputPresenter = GetComponent<PlayerInputPresenter>();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();

            if (!isOwner)
            {
                enabled = false;
                return;
            }

            SetupOwner();
        }

        #region Setup

        private void SetupOwner()
        {
            SetupMouseLook();
            SetupCinemachineCamera();
        }

        private void SetupMouseLook()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SetupCinemachineCamera()
        {
            _camera = Instantiate(_tppCameraPrefab);
            _camera.transform.position = _cameraTarget.position;

            CinemachineCamera cinemachineCamera = _camera.GetComponent<CinemachineCamera>();
            cinemachineCamera.Follow = _cameraTarget;
            cinemachineCamera.LookAt = _cameraTarget;
        }

        #endregion
    }
}