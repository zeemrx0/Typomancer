using System.Collections.Generic;
using LNE.GAS.Attributes;
using UnityEngine;

namespace LNE.GAS
{
    [CreateAssetMenu(fileName = "ASC_", menuName = "GAS/Ability System Component Config")]
    public class AbilitySystemComponentConfig : ScriptableObject
    {
        [field: SerializeField] public List<AttributeConfig> Attributes { get; set; }
        [field: SerializeField] public List<AbilityConfig> Abilities { get; set; }
    }
}