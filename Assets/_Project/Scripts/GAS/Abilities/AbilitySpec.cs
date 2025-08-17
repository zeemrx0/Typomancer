namespace LNE.GAS
{
    public abstract class AbilitySpec
    {
        public string UniqueName { get; private set; }
        public AbilitySystemComponent Owner { get; private set; }
        public AbilityConfig AbilityConfig { get; private set; }

        public bool IsActive { get; private set; }

        public void InitializeAbility(AbilitySystemComponent owner, AbilityConfig abilityConfig)
        {
            UniqueName = abilityConfig.UniqueName;
            Owner = owner;
            AbilityConfig = abilityConfig;
            OnInitializeAbility();
        }

        protected virtual void OnInitializeAbility()
        {
        }

        public abstract void ActivateAbility(params object[] args);

        public void Tick()
        {
            if (!IsActive)
            {
                return;
            }

            TickAbility();
        }

        protected virtual void TickAbility()
        {
        }

        public abstract void CancelAbility();

        public abstract void EndAbility();
    }
}