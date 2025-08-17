using System;
using System.Collections.Generic;
using LNE.GAS.Attributes;
using PurrNet;
using UnityEngine;

namespace LNE.GAS
{
    public class AbilitySystemComponent : NetworkBehaviour
    {
        [SerializeField] private AbilitySystemComponentConfig _config;

        private readonly Dictionary<string, AbilitySpec> _abilities = new();
        private readonly Dictionary<string, AttributeValue> _attributes = new();

        // Event that fires when any attribute changes
        public event Action<string, float, float> OnAnyAttributeChanged;

        private void Awake()
        {
            InitializeAbilities();
            InitializeAttributes();
        }

        private void Update()
        {
            TickAbilities();
        }

        private void InitializeAbilities()
        {
            foreach (AbilityConfig abilityConfig in _config.Abilities)
            {
                GrantAbility(abilityConfig);
            }
        }

        private void TickAbilities()
        {
            foreach (AbilitySpec abilitySpec in _abilities.Values)
            {
                abilitySpec.Tick();
            }
        }

        public void GrantAbility(AbilityConfig abilityConfig)
        {
            AbilitySpec abilitySpec = abilityConfig.GetAbilitySpec(this);
            _abilities.Add(abilitySpec.UniqueName, abilitySpec);
        }

        public void TryActivateAbility(AbilityConfig abilityConfig)
        {
            if (!_abilities.TryGetValue(abilityConfig.UniqueName, out AbilitySpec abilitySpec))
            {
                return;
            }

            abilitySpec.ActivateAbility();
        }

        private void InitializeAttributes()
        {
            foreach (AttributeConfig attributeConfig in _config.Attributes)
            {
                AddAttribute(attributeConfig);
            }
        }

        public void AddAttribute(AttributeConfig attributeConfig)
        {
            AttributeValue attributeValue = new(
                attributeConfig.Name,
                attributeConfig.BaseValue
            );

            // Subscribe to the individual attribute's change event
            attributeValue.OnValueChanged += (oldValue, newValue) =>
            {
                OnAnyAttributeChanged?.Invoke(attributeValue.Name, oldValue, newValue);
            };

            _attributes.Add(attributeValue.Name, attributeValue);
        }

        public float TryGetAttributeCurrentValue(string attributeName)
        {
            if (!_attributes.TryGetValue(attributeName, out AttributeValue attributeValue))
            {
                throw new KeyNotFoundException($"Attribute {attributeName} not found");
            }

            return attributeValue.CurrentValue;
        }

        public void SetAttributeCurrentValue(string attributeName, float value)
        {
            if (!_attributes.TryGetValue(attributeName, out AttributeValue _))
            {
                throw new KeyNotFoundException($"Attribute {attributeName} not found");
            }

            _attributes[attributeName].CurrentValue = value;
        }

        /// <summary>
        /// Subscribe to a specific attribute's value changes
        /// </summary>
        /// <param name="attributeName">Name of the attribute to subscribe to</param>
        /// <param name="callback">Callback that receives (oldValue, newValue)</param>
        public void SubscribeToAttribute(string attributeName, Action<float, float> callback)
        {
            if (!_attributes.TryGetValue(attributeName, out AttributeValue attributeValue))
            {
                throw new KeyNotFoundException($"Attribute {attributeName} not found");
            }

            attributeValue.OnValueChanged += callback;
        }

        /// <summary>
        /// Unsubscribe from a specific attribute's value changes
        /// </summary>
        /// <param name="attributeName">Name of the attribute to unsubscribe from</param>
        /// <param name="callback">Callback to remove</param>
        public void UnsubscribeFromAttribute(string attributeName, Action<float, float> callback)
        {
            if (!_attributes.TryGetValue(attributeName, out AttributeValue attributeValue))
            {
                throw new KeyNotFoundException($"Attribute {attributeName} not found");
            }

            attributeValue.OnValueChanged -= callback;
        }

        /// <summary>
        /// Get all attribute names currently registered
        /// </summary>
        /// <returns>Collection of attribute names</returns>
        public IEnumerable<string> GetAttributeNames()
        {
            return _attributes.Keys;
        }

        /// <summary>
        /// Get the AttributeValue object for direct access (use with caution)
        /// </summary>
        /// <param name="attributeName">Name of the attribute</param>
        /// <returns>AttributeValue object</returns>
        public AttributeValue GetAttributeValue(string attributeName)
        {
            if (!_attributes.TryGetValue(attributeName, out AttributeValue attributeValue))
            {
                throw new KeyNotFoundException($"Attribute {attributeName} not found");
            }

            return attributeValue;
        }
    }
}