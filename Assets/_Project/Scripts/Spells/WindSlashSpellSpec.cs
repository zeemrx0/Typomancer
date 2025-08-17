using LNE.GAS;
using UnityEngine;

namespace LNE.Spells
{
    public class WindSlashSpellSpec : AbilitySpec
    {
        private GameObject _windSlash;

        private WindSlashSpellConfig Config => (WindSlashSpellConfig)AbilityConfig;

        public override void ActivateAbility(params object[] args)
        {
            _windSlash = Object.Instantiate(
                Config.WindSlashPrefab,
                Owner.transform.position,
                Owner.transform.rotation
            );

            // Move the wind slash forward for 1 second, then destroy it
            Vector3 forwardDirection = Owner.transform.forward;
            Vector3 targetPosition =
                _windSlash.transform.position + forwardDirection * 10f; // Adjust distance as needed

            _windSlash.GetComponent<ProjectilePresenter>()
                .SetOwner(Owner)
                .SetLifeSpan(0.5f)
                .SetDamage(Config.Damage)
                .SetTargetPosition(targetPosition);

            EndAbility();
        }

        public override void CancelAbility()
        {
        }

        public override void EndAbility()
        {
            _windSlash = null;
        }
    }
}