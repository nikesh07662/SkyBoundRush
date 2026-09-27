using UnityEngine;

namespace SkyboundRush
{
    /// <summary>
    /// Attached to the obstacle pair prefab. Handles horizontal translation,
    /// dynamic gap spacing adjustments, and viewport despawning.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Horizontal speed towards the left.")]
        [SerializeField] private float speed = 3.2f;

        [Tooltip("X-coordinate threshold past which the obstacle is automatically destroyed.")]
        [SerializeField] private float despawnX = -12.5f;

        [Header("Pillars References")]
        [SerializeField] private Transform topSpire;
        [SerializeField] private Transform bottomSpire;
        [SerializeField] private BoxCollider2D scoreTrigger;

        private bool isMoving = true;

        private void Update()
        {
            if (!isMoving) return;

            transform.position += Vector3.left * (speed * Time.deltaTime);

            if (transform.position.x < despawnX)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Adjusts the vertical gap spacing between the top and bottom spires.
        /// </summary>
        public void ConfigureGap(float gapHeight)
        {
            float halfGap = gapHeight * 0.5f;
            float spireHalfHeight = 3.0f; // 6.0 units total height / 2

            if (topSpire != null)
            {
                // Top spire bottom tip sits at +halfGap
                topSpire.localPosition = new Vector3(0f, halfGap + spireHalfHeight, 0f);
            }

            if (bottomSpire != null)
            {
                // Bottom spire top tip sits at -halfGap
                bottomSpire.localPosition = new Vector3(0f, -(halfGap + spireHalfHeight), 0f);
            }

            if (scoreTrigger != null)
            {
                scoreTrigger.transform.localPosition = Vector3.zero;
                scoreTrigger.size = new Vector2(0.5f, gapHeight);
                scoreTrigger.offset = Vector2.zero;
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        public void StopMoving()
        {
            isMoving = false;
        }
    }
}
