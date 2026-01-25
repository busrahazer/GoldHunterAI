 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // === SINGLETON (Tek instance) ===
    public static GameManager instance;
    
    // === UI REFERANSLARI ===
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI aiScoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI gaInfoText;
    public TextMeshProUGUI qLearningInfoText;
    
    // === OYUN DEĞİŞKENLERİ ===
    private float timeRemaining;         // Kalan süre
    private int playerScore = 0;         // Oyuncu skoru
    private int aiScore = 0;             // AI skoru
    public bool gameActive = true;      // Oyun aktif mi?
    public bool IsGameActive
    {
        get { return gameActive; }
    }

    [Header("Game Settings")]
    public float gameDuration = 60f;
    public bool autoRestart = true;  // Otomatik yeniden başlatma
    public float restartDelay = 2f;   // Yeniden başlatma gecikmesi
    private int gamesPlayed = 0;

    // === YENİ KISIM: PREFAB LİSTELERİ ===
    [Header("Spawn Ayarları")]
    public GameObject[] goldPrefabs; // GoldSmall, GoldMedium, GoldLarge
    public GameObject[] rockPrefabs; // RockSmall, RockLarge

    [Header("Training Settings")]
    public int maxGames = 10; // Hedeflenen oyun sayısı
    public float trainingSpeed = 10f; // Hızlandırma çarpanı

    // Spawn Alanı Sınırları 
    public float xMin = -8f;
    public float xMax = 8f;
    public float yMin = -4.5f;
    public float yMax = -0.5f;

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        RestartGame();
    }
    
    void Update()
    {
        // --- HIZ KONTROLÜ (KLAVYE KISAYOLLARI) ---
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetTimeScale(1f);  // Normal Hız
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetTimeScale(5f);  // 5x Hız
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetTimeScale(10f); // 10x Hız 

        if (!gameActive) return;
        
        // Süreyi azalt
        timeRemaining -= Time.deltaTime;
        
        // Süre bittiyse
        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            EndGame();
        }
        
        UpdateUI();
    }

    void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        // Fizik hesaplamalarının stabil kalması için orantılı artırıyor:
        Time.fixedDeltaTime = 0.02f * Time.timeScale; 
        Debug.Log($"Oyun Hızı: {scale}x");
    }
    
    public void AddScore(int points, bool isPlayer)
    {
        if (!gameActive) return;
        
        if (isPlayer)
            playerScore += points;
        else
            aiScore += points;
        
        UpdateUI();
    }
    
    void UpdateUI()
    {
        playerScoreText.text = "QL: " + playerScore;
        aiScoreText.text = "HGA: " + aiScore;
        timerText.text = "Time: " + Mathf.Ceil(timeRemaining);
    }

    // GA bilgilerini güncelle
    public void UpdateGAInfo()
    {
        if (gaInfoText == null || GAManager.instance == null) return;
        
        string info = $"GA Nesil: {GAManager.instance.currentGeneration}\n";
        info += $"En İyi Fitness: {GAManager.instance.bestFitnessEver:F0}\n";
        info += $"Nesil Ort: {GAManager.instance.currentGenerationAvg:F0}";
        
        gaInfoText.text = info;
    }

    public void UpdateQLearningInfo()
    {
        QLearningAgent qAgent = FindObjectOfType<QLearningAgent>();
        if (qLearningInfoText == null || qAgent == null) return;
        
        qLearningInfoText.text = $"Q-Learning: Oyun {qAgent.totalGamesPlayed} | ε: {qAgent.epsilon:F2}";
    }
    
    void EndGame()
    {
        gameActive = false;
        gamesPlayed++;
        
        Debug.Log("\n========== OYUN BİTTİ ==========");
        Debug.Log($"Q-Learning: {playerScore}");
        Debug.Log($"HGA: {aiScore}");
        
        if (playerScore > aiScore)
            Debug.Log("🏆 Q-LEARNING KAZANDI!");
        else if (aiScore > playerScore)
            Debug.Log("🏆 HGA KAZANDI!");
        else
            Debug.Log("🤝 BERABERE!");
        
        Debug.Log("================================\n");
        
        // GA'ya bildirme
        if (GAManager.instance != null)
        {
            GAManager.instance.RecordGameResult(aiScore);
            UpdateGAInfo();
        }
        
        // Q-Learning Agent'a bildirme
        QLearningAgent qAgent = FindObjectOfType<QLearningAgent>();
        if (qAgent != null)
        {
            qAgent.OnGameEnd(playerScore);
            UpdateQLearningInfo();
        }

        // Q-Learning bilgilerini al
        float qEpsilon = qAgent != null ? qAgent.epsilon : 0f;
        
        // GA bilgilerini al
        int gaGen = GAManager.instance != null ? GAManager.instance.currentGeneration : 0;
        float gaBest = GAManager.instance != null ? GAManager.instance.bestFitnessEver : 0f;
        
        // Tracking'e kaydet
        if (LearningTracker.instance != null)
        {
            LearningTracker.instance.RecordGame(gamesPlayed, playerScore, aiScore, 
                                            qEpsilon, gaGen, gaBest);
        }
        
        UpdateGAInfo();
        UpdateQLearningInfo();

        if (gamesPlayed >= maxGames)
        {
            Debug.Log($" HEDEFLENEN {maxGames} OYUN TAMAMLANDI! EĞİTİM BİTTİ.");
            
            // Zamanı durdur
            Time.timeScale = 0;       
        }

        // Otomatik yeniden başlat
        if (autoRestart)
        {
            Invoke(nameof(RestartGame), restartDelay);
        }
    }

    void RestartGame()
    {
        Debug.Log("\n YENİ OYUN BAŞLIYOR...\n");
        // Skorları sıfırla
        playerScore = 0;
        aiScore = 0;
        timeRemaining = gameDuration;
        gameActive = true;
        
        // Eski objeleri yok et
        CollectibleObject[] collectibles = FindObjectsOfType<CollectibleObject>();
        foreach (var obj in collectibles)
        {
            Destroy(obj.gameObject);
        }
        
        // Yeni objeler spawn et
        SpawnObjects();
        
        UpdateUI();
        UpdateGAInfo();
        UpdateQLearningInfo();
    }
    
    void SpawnObjects()
    {
        // 1. ALTINLARI OLUŞTUR
        // Her oyun 7 ile 17 arasında rastgele sayıda altın
        int goldCount = Random.Range(7, 18); 

        for (int i = 0; i < goldCount; i++)
        {
            // Rastgele bir pozisyon seç
            Vector3 randomPos = new Vector3(
                Random.Range(xMin, xMax), 
                Random.Range(yMin, yMax), 
                0
            );

            // Listeden RASTGELE bir altın türü seç (Small, Medium veya Large)
            if (goldPrefabs.Length > 0)
            {
                GameObject selectedPrefab = goldPrefabs[Random.Range(0, goldPrefabs.Length)];
                Instantiate(selectedPrefab, randomPos, Quaternion.identity);
            }
        }
        
        // 2. TAŞLARI OLUŞTUR
        // Her oyun 5 ile 10 arasında rastgele taş
        int rockCount = Random.Range(5, 11);

        for (int i = 0; i < rockCount; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(xMin, xMax), 
                Random.Range(yMin, yMax), 
                0
            );

            // Listeden rastgele bir taş seç
            if (rockPrefabs.Length > 0)
            {
                GameObject selectedPrefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
                Instantiate(selectedPrefab, randomPos, Quaternion.identity);
            }
        }
        
        Debug.Log($"Yeni Harita Oluşturuldu: {goldCount} Altın, {rockCount} Taş");
    }

}
