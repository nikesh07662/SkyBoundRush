using System;
using UnityEngine;

namespace SkyboundRush
{
    /// <summary>
    /// Tracks the current score and local high score.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        private static ScoreManager instance;
        public static ScoreManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<ScoreManager>();
                    if (instance != null && instance.HighScore == 0)
                    {
                        instance.HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        private const string HighScoreKey = "SkyboundRush_HighScore";

        public int CurrentScore { get; private set; } = 0;
        public int HighScore { get; private set; } = 0;

        public event Action<int> OnScoreChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        public void AddScore(int amount = 1)
        {
            CurrentScore += amount;

            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
                PlayerPrefs.Save();
            }

            OnScoreChanged?.Invoke(CurrentScore);
        }

        public void ResetScore()
        {
            CurrentScore = 0;
            OnScoreChanged?.Invoke(CurrentScore);
        }
    }
}
