using UnityEngine;

namespace LNE.Characters
{
    [CreateAssetMenu(fileName = "CC_", menuName = "Config/Character/Character Config")]
    public class CharacterConfig : ScriptableObject
    {
        [field: SerializeField] public float MaxWalkingSpeed { get; set; } = 5f;
        [field: SerializeField] public float MaxAcceleration { get; set; } = 20.0f;
        [field: SerializeField] public float BrakingDeceleration { get; set; } = 20.0f;
        [field: SerializeField] public float GroundFriction { get; set; } = 8.0f;
        [field: SerializeField] public float AirFriction { get; set; } = 0.5f;
        [field: SerializeField] public float AirControl { get; set; } = 0.3f;
        [field: SerializeField] public Vector3 Gravity { get; set; } = Vector3.down * 9.81f;
        [field: SerializeField] public float RotationRate { get; set; } = 540.0f;
    }
}