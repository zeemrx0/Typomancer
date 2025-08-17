using LNE.GAS;
using PurrNet;

namespace LNE
{
    public class Tree : NetworkBehaviour
    {
        private AbilitySystemComponent _abilitySystemComponent;

        private void Awake()
        {
            _abilitySystemComponent = GetComponent<AbilitySystemComponent>();
        }

        protected override void OnSpawned()
        {
            _abilitySystemComponent.SetAttributeCurrentValue("Health", 30f);
            _abilitySystemComponent.SubscribeToAttribute("Health", OnHealthChanged);
        }

        private void OnHealthChanged(float oldValue, float newValue)
        {
            if (newValue <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}