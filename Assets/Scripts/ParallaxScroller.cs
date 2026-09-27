using UnityEngine;

namespace SkyboundRush
{
    /// <summary>
    /// Smooth infinite horizontal parallax scrolling for background and cloud layers.
    /// </summary>
    public class ParallaxScroller : MonoBehaviour
    {
        [System.Serializable]
        public class ParallaxLayer
        {
            public Transform transform1;
            public Transform transform2;
            public float speed = 1.0f;
            public float width = 20.0f;
        }

        [SerializeField] private ParallaxLayer[] layers;
        [SerializeField] private bool scrollInReadyState = true;

        private bool isScrolling = true;

        private void Update()
        {
            if (!isScrolling) return;

            float dt = Time.deltaTime;

            if (layers == null) return;

            for (int i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                if (layer.transform1 == null || layer.transform2 == null) continue;

                float delta = layer.speed * dt;
                layer.transform1.position += Vector3.left * delta;
                layer.transform2.position += Vector3.left * delta;

                // Wrap tile 1
                if (layer.transform1.position.x <= -layer.width)
                {
                    float rightmostX = layer.transform2.position.x + layer.width;
                    layer.transform1.position = new Vector3(rightmostX, layer.transform1.position.y, layer.transform1.position.z);
                }

                // Wrap tile 2
                if (layer.transform2.position.x <= -layer.width)
                {
                    float rightmostX = layer.transform1.position.x + layer.width;
                    layer.transform2.position = new Vector3(rightmostX, layer.transform2.position.y, layer.transform2.position.z);
                }
            }
        }

        public void SetScrolling(bool enabled)
        {
            isScrolling = enabled;
        }

        public void OnGameReady()
        {
            isScrolling = scrollInReadyState;
        }

        public void OnGameStart()
        {
            isScrolling = true;
        }

        public void OnGameOver()
        {
            isScrolling = false;
        }
    }
}
