using LNE.GAS;
using UnityEngine;

namespace LNE.Spells
{
    [CreateAssetMenu(fileName = "AC_WindSlashSpell", menuName = "GAS/Ability/Wind Slash Spell")]
    public class WindSlashSpellConfig : AbilityConfig<WindSlashSpellSpec>
    {
        [field: SerializeField] public GameObject WindSlashPrefab { get; private set; }
        [field: SerializeField] public float Damage { get; private set; } = 10f;

        protected override AbilitySpec CreateAbilitySpec()
        {
            return new WindSlashSpellSpec();
        }
    }
}