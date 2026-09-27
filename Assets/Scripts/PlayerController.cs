using UnityEngine;

namespace SkyboundRush
{
    /// <summary>
    /// Controls the player character's flight physics, input, visual tilt, and collision detection.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Flight Physics")]
        [Tooltip("Upward impulse velocity applied when flapping.")]
        [SerializeField] private float flapForce = 7.5f;

        [Tooltip("Downward gravity scale when active.")]
        [SerializeField] private float activeGravity = 2.4f;

        [Tooltip("Maximum upper boundary before clamping position.")]
        [SerializeField] private float ceilingY = 4.85f;

        [Header("Visual Tilt")]
        [Tooltip("Maximum upward tilt angle in degrees when ascending.")]
        [SerializeField] private float maxUpwardAngle = 30f;

        [Tooltip("Maximum downward tilt angle in degrees when diving.")]
        [SerializeField] private float maxDownwardAngle = -75f;

        [Tooltip("Speed of rotation interpolation.")]
        [SerializeField] private float tiltSpeed = 7f;

        [Header("Idle Hover")]
        [SerializeField] private float hoverFrequency = 3.5f;
        [SerializeField] private float hoverAmplitude = 0.25f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem flapParticles;
        [SerializeField] private ParticleSystem deathParticles;
        [SerializeField] private TrailRenderer wingTrail;

        private Rigidbody2D rb;
        private Vector3 startPosition;
        private bool isDead = false;
        private bool isHovering = true;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (startPosition == Vector3.zero && transform.position != Vector3.zero)
            {
                startPosition = transform.position;
            }
        }

        private void Start()
        {
            ResetPlayer();
        }

        private void Update()
        {
            if (isDead)
            {
                // In dead state, smoothly tilt nose down
                ApplyTilt();
                return;
            }

            if (isHovering)
            {
                // Gentle floating bob before run begins
                float newY = startPosition.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
                transform.position = new Vector3(startPosition.x, newY, startPosition.z);
                transform.rotation = Quaternion.identity;

                if (HasFlapInput())
                {
                    StartFlight();
                    Flap();
                }
                return;
            }

            // Active flight input
            if (HasFlapInput())
            {
                Flap();
            }

            // Ceiling clamp
            if (transform.position.y > ceilingY)
            {
                transform.position = new Vector3(transform.position.x, ceilingY, transform.position.z);
                if (GetVerticalVelocity() > 0)
                {
                    SetVerticalVelocity(0f);
                }
            }

            ApplyTilt();
        }

        private bool HasFlapInput()
        {
            return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        }

        public void Flap()
        {
            if (isDead) return;

            SetVerticalVelocity(flapForce);

            if (flapParticles != null)
            {
                flapParticles.Play();
            }
        }

        public void StartFlight()
        {
            EnsureInitialized();
            isHovering = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = activeGravity;

            if (wingTrail != null)
            {
                wingTrail.emitting = true;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStarted();
            }
        }

        private void ApplyTilt()
        {
            float vY = GetVerticalVelocity();
            // Map vertical velocity between -10 and +7 to rotation angle
            float t = Mathf.InverseLerp(-10f, 7f, vY);
            float targetAngle = Mathf.Lerp(maxDownwardAngle, maxUpwardAngle, t);

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, tiltSpeed * Time.deltaTime);
        }

        private void SetVerticalVelocity(float y)
        {
            EnsureInitialized();
            #if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector2(0f, y);
            #else
            rb.velocity = new Vector2(0f, y);
            #endif
        }

        private float GetVerticalVelocity()
        {
            EnsureInitialized();
            #if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity.y;
            #else
            return rb.velocity.y;
            #endif
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDead) return;

            if (other.CompareTag("ScoreTrigger"))
            {
                if (GameManager.Instance != null) GameManager.Instance.AddScore();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDead) return;

            Die();
        }

        private void Die()
        {
            isDead = true;
            if (deathParticles != null)
            {
                deathParticles.transform.position = transform.position;
                deathParticles.Play();
            }

            if (wingTrail != null)
            {
                wingTrail.emitting = false;
            }

            if (GameManager.Instance != null) GameManager.Instance.OnPlayerDied();
        }

        public void ResetPlayer()
        {
            EnsureInitialized();
            isDead = false;
            isHovering = true;
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;

            rb.bodyType = RigidbodyType2D.Kinematic;
            SetVerticalVelocity(0f);

            if (wingTrail != null)
            {
                wingTrail.Clear();
                wingTrail.emitting = false;
            }
        }
    }
}
