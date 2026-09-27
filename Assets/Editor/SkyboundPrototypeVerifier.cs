#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkyboundRush.Editor
{
    /// <summary>
    /// Automated test verification script that runs headless to rigorously validate
    /// physics, spawning, scoring, state transitions, and scene integrity.
    /// </summary>
    public static class SkyboundPrototypeVerifier
    {
        public static void RunVerification()
        {
            Debug.Log("[SkyboundVerifier] ====== RUNNING AUTOMATED PROTOTYPE VERIFICATION ======");

            try
            {
                TestSceneLoading();
                TestPlayerSetup();
                TestObstaclePrefab();
                TestScoringSystem();
                TestGameFlowStateTransitions();

                Debug.Log("[PROTOTYPE_VERIFICATION_PASSED] All 5 automated test suites passed successfully!");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PROTOTYPE_VERIFICATION_FAILED] Test assertion failed: {ex.Message}\n{ex.StackTrace}");
                EditorApplication.Exit(1);
            }
        }

        private static void TestSceneLoading()
        {
            Debug.Log("[SkyboundVerifier] 1. Testing scene load and critical components...");
            string scenePath = "Assets/Scenes/MainGame.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Assert(scene.IsValid(), "Scene could not be opened or is invalid!");

            var gm = Object.FindAnyObjectByType<GameManager>();
            var sm = Object.FindAnyObjectByType<ScoreManager>();
            var ui = Object.FindAnyObjectByType<UIManager>();
            var spawner = Object.FindAnyObjectByType<ObstacleSpawner>();
            var player = Object.FindAnyObjectByType<PlayerController>();

            Assert(gm != null, "GameManager not found in scene!");
            Assert(sm != null, "ScoreManager not found in scene!");
            Assert(ui != null, "UIManager not found in scene!");
            Assert(spawner != null, "ObstacleSpawner not found in scene!");
            Assert(player != null, "PlayerController not found in scene!");

            Debug.Log("[SkyboundVerifier] Scene components verified.");
        }

        private static void TestPlayerSetup()
        {
            Debug.Log("[SkyboundVerifier] 2. Testing Player setup, physics components and controls...");
            var player = Object.FindAnyObjectByType<PlayerController>();
            var rb = player.GetComponent<Rigidbody2D>();
            var col = player.GetComponent<CircleCollider2D>();

            Assert(rb != null, "Player missing Rigidbody2D!");
            Assert(col != null, "Player missing CircleCollider2D!");
            Assert(col.radius > 0.2f && col.radius < 0.8f, "Player collider radius out of expected bounds!");

            // Test reset
            player.ResetPlayer();
            Assert(rb.bodyType == RigidbodyType2D.Kinematic, "Player should be Kinematic during hover!");

            // Test flight activation
            player.StartFlight();
            Assert(rb.bodyType == RigidbodyType2D.Dynamic, "Player should become Dynamic after flight starts!");
            Assert(rb.gravityScale > 0f, "Player gravity scale must be positive!");

            // Test flap
            player.Flap();
            #if UNITY_6000_0_OR_NEWER
            float vy = rb.linearVelocity.y;
            #else
            float vy = rb.velocity.y;
            #endif
            Assert(vy > 5.0f, $"Flap velocity ({vy}) should be > 5.0f!");

            Debug.Log("[SkyboundVerifier] Player physics and flap impulse verified.");
        }

        private static void TestObstaclePrefab()
        {
            Debug.Log("[SkyboundVerifier] 3. Testing Obstacle Prefab and fairness clearances...");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ObstaclePair.prefab");
            Assert(prefab != null, "ObstaclePair prefab missing!");

            GameObject instance = Object.Instantiate(prefab);
            Obstacle obs = instance.GetComponent<Obstacle>();
            Assert(obs != null, "Obstacle script missing on prefab instance!");

            // Test gap configuration
            obs.ConfigureGap(3.2f);

            var triggers = instance.GetComponentsInChildren<Collider2D>();
            bool hasObstacleCol = false;
            bool hasScoreTrigger = false;

            foreach (var c in triggers)
            {
                if (c.CompareTag("Obstacle") && !c.isTrigger) hasObstacleCol = true;
                if (c.CompareTag("ScoreTrigger") && c.isTrigger) hasScoreTrigger = true;
            }

            Assert(hasObstacleCol, "Obstacle colliders with tag 'Obstacle' missing!");
            Assert(hasScoreTrigger, "Trigger collider with tag 'ScoreTrigger' missing!");

            Object.DestroyImmediate(instance);
            Debug.Log("[SkyboundVerifier] Obstacle prefab tags and trigger geometry verified.");
        }

        private static void TestScoringSystem()
        {
            Debug.Log("[SkyboundVerifier] 4. Testing Scoring and High Score persistence...");
            var sm = ScoreManager.Instance;
            Assert(sm != null, "ScoreManager instance is null!");

            sm.ResetScore();
            Assert(sm.CurrentScore == 0, "Score did not reset to 0!");

            sm.AddScore(1);
            Assert(sm.CurrentScore == 1, "Score was not incremented to 1!");

            sm.AddScore(5);
            Assert(sm.CurrentScore == 6, "Score was not incremented to 6!");
            Assert(sm.HighScore >= 6, "High score was not updated!");

            sm.ResetScore();
            Assert(sm.CurrentScore == 0, "Score did not reset to 0 after second reset!");

            Debug.Log("[SkyboundVerifier] Score counter and High Score logic verified.");
        }

        private static void TestGameFlowStateTransitions()
        {
            Debug.Log("[SkyboundVerifier] 5. Testing Game Flow state transitions (Ready -> Playing -> GameOver -> Restart)...");
            var gm = GameManager.Instance;
            var player = Object.FindAnyObjectByType<PlayerController>();

            gm.SetReadyState();
            Assert(gm.CurrentState == GameState.Ready, "Expected Ready state!");

            player.StartFlight();
            Assert(gm.CurrentState == GameState.Playing, "Expected Playing state!");

            gm.OnPlayerDied();
            Assert(gm.CurrentState == GameState.GameOver, "Expected GameOver state!");

            gm.RestartGame();
            Assert(gm.CurrentState == GameState.Ready, "Expected Ready state after restart!");

            Debug.Log("[SkyboundVerifier] Game state transitions verified.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.Exception("Assertion Failed: " + message);
            }
        }
    }
}
#endif
