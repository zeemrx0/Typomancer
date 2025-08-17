using UnityEngine;

namespace LNE.GAS.Attributes
{
    [CreateAssetMenu(fileName = "Attribute_", menuName = "GAS/Attribute Config")]
    public class AttributeConfig : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public float BaseValue { get; private set; }
    }
}