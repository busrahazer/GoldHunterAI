using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class LearningTracker : MonoBehaviour
{
    public static LearningTracker instance;
    
    [Header("Tracking Settings")]
    public bool enableTracking = true;
    public string csvFileName = "learning_results.csv";
    
    private List<GameResult> gameResults = new List<GameResult>();
    private string csvPath;
    
    [System.Serializable]
    public class GameResult
    {
        public int gameNumber;
        public int qLearningScore;
        public int hgaScore;
        public string winner;
        public float qLearningEpsilon;
        public int gaGeneration;
        public float gaBestFitness;
        public string timestamp;
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        csvPath = Path.Combine(Application.persistentDataPath, csvFileName);
        Debug.Log($" Tracking dosyası: {csvPath}");
    }
    
    void Start()
    {
        if (enableTracking)
        {
            CreateCSVFile();
        }
    }
    
    void CreateCSVFile()
    {
        if (File.Exists(csvPath)) return; // Zaten var
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("GameNumber,QLearningScore,HGAScore,Winner,QLearningEpsilon,GAGeneration,GABestFitness,Timestamp");
        
        File.WriteAllText(csvPath, sb.ToString());
        Debug.Log($" CSV dosyası oluşturuldu: {csvPath}");
    }
    
    public void RecordGame(int gameNumber, int qLearningScore, int hgaScore, 
                          float qLearningEpsilon, int gaGeneration, float gaBestFitness)
    {
        if (!enableTracking) return;
        
        string winner = qLearningScore > hgaScore ? "Q-Learning" : 
                       hgaScore > qLearningScore ? "HGA" : "Draw";
        
        GameResult result = new GameResult
        {
            gameNumber = gameNumber,
            qLearningScore = qLearningScore,
            hgaScore = hgaScore,
            winner = winner,
            qLearningEpsilon = qLearningEpsilon,
            gaGeneration = gaGeneration,
            gaBestFitness = gaBestFitness,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        gameResults.Add(result);
        AppendToCSV(result);
        
        // Her 10 oyunda bir özet yazdır
        if (gameNumber % 10 == 0)
        {
            PrintSummary();
        }
    }
    
    void AppendToCSV(GameResult result)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{result.gameNumber},{result.qLearningScore},{result.hgaScore}," +
                     $"{result.winner},{result.qLearningEpsilon:F3},{result.gaGeneration}," +
                     $"{result.gaBestFitness:F0},{result.timestamp}");
        
        File.AppendAllText(csvPath, sb.ToString());
    }
    
    void PrintSummary()
    {
        if (gameResults.Count == 0) return;
        
        int qLearningWins = 0;
        int hgaWins = 0;
        int draws = 0;
        float avgQLearning = 0;
        float avgHGA = 0;
        
        foreach (var result in gameResults)
        {
            if (result.winner == "Q-Learning") qLearningWins++;
            else if (result.winner == "HGA") hgaWins++;
            else draws++;
            
            avgQLearning += result.qLearningScore;
            avgHGA += result.hgaScore;
        }
        
        avgQLearning /= gameResults.Count;
        avgHGA /= gameResults.Count;
        
        Debug.Log("\n ========== ÖZET ==========");
        Debug.Log($"Toplam Oyun: {gameResults.Count}");
        Debug.Log($"Q-Learning Galibiyetleri: {qLearningWins} ({(qLearningWins * 100f / gameResults.Count):F1}%)");
        Debug.Log($"HGA Galibiyetleri: {hgaWins} ({(hgaWins * 100f / gameResults.Count):F1}%)");
        Debug.Log($"Beraberlikler: {draws}");
        Debug.Log($"Ortalama Q-Learning Skoru: {avgQLearning:F0}");
        Debug.Log($"Ortalama HGA Skoru: {avgHGA:F0}");
        Debug.Log("============================\n");
    }
    
    public void ExportResults()
    {
        Debug.Log($"📁 Sonuçlar kaydedildi: {csvPath}");
        Debug.Log("Dosyayı Excel'de açabilirsin!");
    }
}