using UnityEngine;
using UnityEngine.UI;

namespace SkyboundRush
{
    /// <summary>
    /// Manages on-screen UI states: Ready prompt, active Score HUD, and Game Over scoreboard.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;
        public static UIManager Instance
        {
            get
            {
                if (instance == null) instance = FindAnyObjectByType<UIManager>();
                return instance;
            }
            private set => instance = value;
        }

        [Header("HUD Elements")]
        [SerializeField] private Text scoreText;
        [SerializeField] private GameObject readyPanel;

        [Header("Game Over Elements")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text bestScoreText;
        [SerializeField] private Text newBestBadge;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Start()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += UpdateScoreDisplay;
            }
        }

        private void OnDestroy()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
            }
        }

        public void ShowReady()
        {
            if (readyPanel != null) readyPanel.SetActive(true);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(true);
                scoreText.text = "0";
            }
        }

        public void ShowPlaying()
        {
            if (readyPanel != null) readyPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (scoreText != null) scoreText.gameObject.SetActive(true);
        }

        public void ShowGameOver(int finalScore, int bestScore, bool isNewBest)
        {
            if (readyPanel != null) readyPanel.SetActive(false);
            if (scoreText != null) scoreText.gameObject.SetActive(false);

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                if (finalScoreText != null) finalScoreText.text = "SCORE: " + finalScore;
                if (bestScoreText != null) bestScoreText.text = "BEST: " + bestScore;
                if (newBestBadge != null) newBestBadge.gameObject.SetActive(isNewBest && finalScore > 0);
            }
        }

        public void UpdateScoreDisplay(int currentScore)
        {
            if (scoreText != null)
            {
                scoreText.text = currentScore.ToString();
                StopAllCoroutines();
                StartCoroutine(PunchScoreText());
            }
        }

        private System.Collections.IEnumerator PunchScoreText()
        {
            if (scoreText == null) yield break;
            Vector3 originalScale = Vector3.one;
            scoreText.transform.localScale = originalScale * 1.35f;
            float elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.deltaTime;
                scoreText.transform.localScale = Vector3.Lerp(originalScale * 1.35f, originalScale, elapsed / 0.18f);
                yield return null;
            }
            scoreText.transform.localScale = originalScale;
        }
    }
}
