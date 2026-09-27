using System.Collections.Generic;
using UnityEngine;

namespace SkyboundRush
{
    /// <summary>
    /// Procedurally spawns obstacle pairs at randomized fair heights and modulates
    /// speed and gap size as score progresses.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("Prefab Reference")]
        [SerializeField] private GameObject obstaclePrefab;

        [Header("Spawn Layout")]
        [Tooltip("Horizontal coordinate where obstacles spawn offscreen to the right.")]
        [SerializeField] private float spawnX = 10.5f;

        [Tooltip("Minimum Y coordinate for the gap center.")]
        [SerializeField] private float minGapY = -1.8f;

        [Tooltip("Maximum Y coordinate for the gap center.")]
        [SerializeField] private float maxGapY = 1.8f;

        [Tooltip("Desired horizontal distance between consecutive obstacle pairs in world units.")]
        [SerializeField] private float obstacleDistance = 6.8f;

        [Header("Difficulty Progression")]
        [SerializeField] private float initialSpeed = 3.2f;
        [SerializeField] private float maxSpeed = 5.0f;
        [SerializeField] private float initialGapHeight = 3.3f;
        [SerializeField] private float minGapHeight = 2.4f;

        [Tooltip("Score count at which maximum difficulty (speed and gap compression) is achieved.")]
        [SerializeField] private int maxDifficultyScore = 35;

        private float timer = 0f;
        private bool isSpawning = false;
        private List<Obstacle> activeObstacles = new List<Obstacle>();

        private void Update()
        {
            if (!isSpawning) return;

            int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
            float difficultyT = Mathf.Clamp01((float)currentScore / maxDifficultyScore);

            float currentSpeed = Mathf.Lerp(initialSpeed, maxSpeed, difficultyT);
            float currentGapHeight = Mathf.Lerp(initialGapHeight, minGapHeight, difficultyT);

            // Compute timer based on distance and speed to maintain fair horizontal spacing
            float spawnInterval = obstacleDistance / currentSpeed;

            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                timer = 0f;
                SpawnObstacle(currentSpeed, currentGapHeight);
            }
        }

        private void SpawnObstacle(float speed, float gapHeight)
        {
            if (obstaclePrefab == null) return;

            float randomY = Random.Range(minGapY, maxGapY);
            Vector3 spawnPosition = new Vector3(spawnX, randomY, 0f);

            GameObject instance = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity, transform);
            Obstacle obstacle = instance.GetComponent<Obstacle>();

            if (obstacle != null)
            {
                obstacle.ConfigureGap(gapHeight);
                obstacle.SetSpeed(speed);
                activeObstacles.Add(obstacle);
            }
        }

        private void Awake()
        {
            if (obstaclePrefab == null)
            {
                #if UNITY_EDITOR
                obstaclePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ObstaclePair.prefab");
                #endif
            }
        }

        public void StartSpawning()
        {
            isSpawning = true;
            float spawnInterval = obstacleDistance / Mathf.Max(initialSpeed, 1f);
            timer = Mathf.Max(0f, spawnInterval - 0.6f); // 0.6s delay before first obstacle appears
        }

        public void StopSpawning()
        {
            isSpawning = false;

            // Stop movement on all existing obstacles
            activeObstacles.RemoveAll(item => item == null);
            foreach (var obs in activeObstacles)
            {
                if (obs != null) obs.StopMoving();
            }
        }

        public void ClearAllObstacles()
        {
            isSpawning = false;
            timer = 0f;

            activeObstacles.RemoveAll(item => item == null);
            foreach (var obs in activeObstacles)
            {
                if (obs != null)
                {
                    Destroy(obs.gameObject);
                }
            }
            activeObstacles.Clear();

            // Also clean up any lingering obstacles that were children of this spawner
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
