using System;

namespace LNE.GAS.Attributes
{
    public class AttributeValue
    {
        public event Action<float, float> OnValueChanged;

        public string Name { get; set; }
        public float BaseValue { get; set; }

        private float _currentValue;

        public float CurrentValue
        {
            get => _currentValue;
            set
            {
                OnValueChanged?.Invoke(_currentValue, value);
                _currentValue = value;
            }
        }

        public AttributeValue(string name, float baseValue)
        {
            Name = name;
            BaseValue = baseValue;
            CurrentValue = baseValue;
        }
    }
}