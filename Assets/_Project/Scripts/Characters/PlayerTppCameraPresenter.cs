using PurrNet;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

namespace LNE.Characters
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

        private GameObject _camera;

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
            SetupCinemachineCamera();
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