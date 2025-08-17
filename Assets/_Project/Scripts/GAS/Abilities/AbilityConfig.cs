using UnityEngine;

namespace LNE.GAS
{
    public abstract class AbilityConfig : ScriptableObject
    {
        [field: SerializeField] public string UniqueName { get; private set; }

        public AbilitySpec GetAbilitySpec(AbilitySystemComponent owner)
        {
            AbilitySpec ability = CreateAbilitySpec();
            ability.InitializeAbility(owner, this);
            return ability;
        }

        protected abstract AbilitySpec CreateAbilitySpec();
    }

    public abstract class AbilityConfig<T> : AbilityConfig where T : AbilitySpec, new()
    {
        protected override AbilitySpec CreateAbilitySpec() => new T();
    }
}