using LNE.Utilities.Constants;
using PurrNet;

namespace LNE.Characters
{
    public class PlayerCharacterMovementView : NetworkBehaviour
    {
        private NetworkAnimator _networkAnimator;

        private void Awake()
        {
            _networkAnimator = GetComponent<NetworkAnimator>();
        }

        public void SetWalkingSpeed(float speed)
        {
            _networkAnimator.SetFloat(AnimatorParameterId.WalkingSpeed, speed);
        }
    }
}