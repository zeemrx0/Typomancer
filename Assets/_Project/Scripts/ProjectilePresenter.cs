using DG.Tweening;
using LNE.GAS;
using PurrNet;
using UnityEngine;

namespace LNE
{
    public class ProjectilePresenter : NetworkBehaviour
    {
        [Header("Projectile Settings")] [SerializeField]
        private LayerMask _targetLayerMask = -1;

        [SerializeField] private GameObject _mainVfx;
        [SerializeField] private GameObject _dissipationVfx;
        [SerializeField] private float _dissipationDuration = 1f;
        [SerializeField] private bool _destroyOnHit = true;

        private AbilitySystemComponent _abilityOwner;
        private float _lifespan;
        private float _lifetime;
        private float _damage;
        private Vector3 _targetPosition;

        private Vector3 _startPosition;

        private bool _hasHit;

        private Tween _destroySequence;

        public ProjectilePresenter SetOwner(AbilitySystemComponent abilityOwner)
        {
            _abilityOwner = abilityOwner;
            return this;
        }

        public ProjectilePresenter SetLifeSpan(float lifeSpan)
        {
            _lifespan = lifeSpan;
            return this;
        }

        public ProjectilePresenter SetDamage(float damage)
        {
            _damage = damage;
            return this;
        }

        public ProjectilePresenter SetTargetPosition(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
            return this;
        }

        protected override void OnSpawned()
        {
            _startPosition = transform.position;
        }

        private void Update()
        {
            if (!isOwner)
            {
                return;
            }

            TryDestroyOnEndLifespan();

            // Move the projectile
            Vector3 direction = (_targetPosition - _startPosition).normalized;
            transform.position += direction * (10f * Time.deltaTime);
        }

        private void TryDestroyOnEndLifespan()
        {
            _lifetime += Time.deltaTime;

            if (_lifetime >= _lifespan - _dissipationDuration)
            {
                TryStarDestroySequence();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Projectile hit " + other.name);

            if (!isOwner || _hasHit)
            {
                return;
            }

            if (other.gameObject == _abilityOwner.gameObject)
            {
                return;
            }

            // Check if the collided object is on the target layer
            if ((_targetLayerMask.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            ApplyHitFeedback(other);
            ApplyHitDamage(other);

            if (_hasHit && _destroyOnHit)
            {
                TryStarDestroySequence();
            }
        }

        private void ApplyHitDamage(Collider other)
        {
            if (other.gameObject.TryGetComponent(out AbilitySystemComponent abilitySystemComponent))
            {
                float currentHealth = abilitySystemComponent.TryGetAttributeCurrentValue("Health");
                abilitySystemComponent.SetAttributeCurrentValue("Health", currentHealth - _damage);
            }

            _hasHit = true;
        }

        private void ApplyHitFeedback(Collider other)
        {
            // Calculate hit direction (from projectile to target)
            Vector3 hitDirection = (other.transform.position - transform.position).normalized;

            // Try to find HitFeedbackPresenter on the hit object
            HitFeedbackPresenter hitFeedback = other.GetComponent<HitFeedbackPresenter>();
            if (hitFeedback)
            {
                hitFeedback.Hit(hitDirection);
            }
        }

        private void TryStarDestroySequence()
        {
            if (_destroySequence != null)
            {
                return;
            }

            PreDestroy();
            _destroySequence = DOVirtual.DelayedCall(_dissipationDuration, Destroy);
        }

        private void PreDestroy()
        {
            _mainVfx.GetComponent<ParticleSystem>().Stop();
            _dissipationVfx.GetComponent<ParticleSystem>().Play();
        }

        private void Destroy()
        {
            transform.DOKill();
            Destroy(gameObject);
        }
    }
}