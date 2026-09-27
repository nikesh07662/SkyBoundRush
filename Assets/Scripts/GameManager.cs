using UnityEngine;

namespace SkyboundRush
{
    public enum GameState
    {
        Ready,
        Playing,
        GameOver
    }

    /// <summary>
    /// Coordinates overall game state, restarts, score handling, and procedural sound feedback.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<GameManager>();
                    if (instance != null) instance.EnsureReferences();
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("Scene References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private ObstacleSpawner spawner;
        [SerializeField] private ParallaxScroller parallax;

        [Header("Audio")]
        [SerializeField] private bool enableSound = true;

        public GameState CurrentState { get; private set; } = GameState.Ready;

        private AudioSource audioSource;
        private AudioClip flapSound;
        private AudioClip scoreSound;
        private AudioClip hitSound;

        private float gameOverTime = 0f;
        private const float RestartCooldown = 0.35f;

        public void EnsureReferences()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (spawner == null) spawner = FindAnyObjectByType<ObstacleSpawner>();
            if (parallax == null) parallax = FindAnyObjectByType<ParallaxScroller>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            EnsureReferences();

            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            GenerateProceduralAudioClips();
        }

        private void Start()
        {
            SetReadyState();
        }

        private void Update()
        {
            if (CurrentState == GameState.GameOver)
            {
                if (Time.time - gameOverTime >= RestartCooldown)
                {
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(0))
                    {
                        RestartGame();
                    }
                }
            }
        }

        public void SetReadyState()
        {
            CurrentState = GameState.Ready;

            if (player != null) player.ResetPlayer();
            if (spawner != null) spawner.ClearAllObstacles();
            if (parallax != null) parallax.OnGameReady();
            if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
            if (UIManager.Instance != null) UIManager.Instance.ShowReady();
        }

        public void OnGameStarted()
        {
            CurrentState = GameState.Playing;

            if (spawner != null) spawner.StartSpawning();
            if (parallax != null) parallax.OnGameStart();
            if (UIManager.Instance != null) UIManager.Instance.ShowPlaying();

            PlaySound(flapSound, 0.6f);
        }

        public void AddScore()
        {
            if (CurrentState != GameState.Playing) return;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(1);
            }

            PlaySound(scoreSound, 0.7f);
        }

        public void OnPlayerDied()
        {
            if (CurrentState == GameState.GameOver) return;

            CurrentState = GameState.GameOver;
            gameOverTime = Time.time;

            if (spawner != null) spawner.StopSpawning();
            if (parallax != null) parallax.OnGameOver();

            int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
            int bestScore = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : 0;
            bool isNewBest = finalScore >= bestScore && finalScore > 0;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOver(finalScore, bestScore, isNewBest);
            }

            PlaySound(hitSound, 0.85f);
        }

        public void RestartGame()
        {
            SetReadyState();
        }

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (!enableSound || clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip, volume);
        }

        #region Procedural Audio Synthesis
        private void GenerateProceduralAudioClips()
        {
            flapSound = CreateChirpClip(440f, 660f, 0.08f);
            scoreSound = CreateBellClip(880f, 1320f, 0.18f);
            hitSound = CreateNoiseThudClip(160f, 60f, 0.22f);
        }

        private AudioClip CreateChirpClip(float startFreq, float endFreq, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float envelope = Mathf.Sin(t * Mathf.PI);
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * (i / (float)sampleRate)) * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("FlapChirp", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateBellClip(float freq1, float freq2, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Exp(-t * 5.0f);
                float wave = Mathf.Sin(2 * Mathf.PI * freq1 * (i / (float)sampleRate)) +
                             0.5f * Mathf.Sin(2 * Mathf.PI * freq2 * (i / (float)sampleRate));
                data[i] = wave * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create("ScoreBell", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateNoiseThudClip(float startFreq, float endFreq, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float envelope = Mathf.Exp(-t * 8.0f);
                float tone = Mathf.Sin(2 * Mathf.PI * freq * (i / (float)sampleRate));
                float noise = (Random.value * 2f - 1f) * 0.3f;
                data[i] = (tone + noise) * envelope * 0.6f;
            }

            AudioClip clip = AudioClip.Create("ImpactThud", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion
    }
}
