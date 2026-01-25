using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QLearningAgent : MonoBehaviour
{
    // === REFERANSLAR ===
    private AITargetDetector detector;
    private RopeLauncher launcher;
    private RopeSwing ropeSwing;
    
    // === Q-LEARNING PARAMETRELERI ===
    [Header("Q-Learning Settings")]
    public float learningRate = 0.1f;      // Öğrenme hızı (alpha)
    public float discountFactor = 0.9f;    // Gelecek ödül indirimi (gamma)
    public float epsilon = 0.9f;           // Keşif oranı (exploration)
    public float epsilonDecay = 0.995f;    // Epsilon azalma oranı
    public float minEpsilon = 0.1f;       // Minimum epsilon
    
    [Header("Advanced Features")]
    public bool useExperienceReplay = true;
    public int replayBufferSize = 500;
    public int replayBatchSize = 32;
    public bool useRewardShaping = true;
    public bool useSimplifiedState = true;

    // === Q-TABLE ===
    private Dictionary<string, float> qTable = new Dictionary<string, float>();
    private List<Experience> experienceBuffer = new List<Experience>();

    // === DURUM TAKİBİ ===
    private string lastState = "";
    private int lastAction = 0; // 0 = bekle, 1 = ateş
    private float lastReward = 0f;
    public int totalGamesPlayed = 0;
    private int successfulShots = 0;
    private int totalShots = 0;
    
    // === KARAR AYARLARI ===
    public float decisionDelay = 0.3f;
    private float decisionTimer = 0f;

    // === ÖDÜLLENDİRME KAYDI ===
    private float episodeReward = 0f;
    private List<float> rewardHistory = new List<float>();
    
    [System.Serializable]
    private class Experience
    {
        public string state;
        public int action;
        public float reward;
        public string nextState;
        public bool done;
    }
    void Start()
    {
        detector = GetComponent<AITargetDetector>();
        launcher = GetComponent<RopeLauncher>();
        ropeSwing = GetComponent<RopeSwing>();
        
        Debug.Log(" Q-Learning Agent başlatıldı!");
    }
    
    void Update()
    {
        // Oyun bittiyse karar verme
        if (GameManager.instance != null && !GameManager.instance.IsGameActive)
            return;

        if (launcher.IsLaunched()) return;
        
        decisionTimer += Time.deltaTime;
        
        if (decisionTimer >= decisionDelay)
        {
            decisionTimer = 0f;
            MakeQLearningDecision();
        }
    }
    
    void MakeQLearningDecision()
    {
        string currentState = GetState();
        
        // Önceki deneyimi kaydet
        if (!string.IsNullOrEmpty(lastState))
        {
            StoreExperience(lastState, lastAction, lastReward, currentState, false);
            UpdateQValue(lastState, lastAction, lastReward, currentState);
            
            // Experience Replay
            if (useExperienceReplay && experienceBuffer.Count >= replayBatchSize)
            {
                ReplayExperiences();
            }
        }
        
        // Aksiyon seç
        int action = SelectAction(currentState);
        
        // Aksiyonu uygula
        if (action == 1)
        {
            totalShots++;
            launcher.LaunchRope();
            
            // REWARD SHAPING: Boşa atış cezası (anlık)
            if (useRewardShaping)
            {
                var targets = detector.GetAvailableTargets();
                if (targets.Count == 0)
                {
                    GiveReward(-0.3f); // Hedef yokken atış cezası
                }
            }
        }
        else
        {
            // REWARD SHAPING: Bekleme ödülü (küçük)
            if (useRewardShaping)
            {
                GiveReward(0.01f); // Sabır ödülü
            }
        }
        
        lastState = currentState;
        lastAction = action;
        lastReward = 0f;
    }
    
    string GetState()
    {
        var targets = detector.GetAvailableTargets();
        
        if (targets.Count == 0)
        {
            return "no_target";
        }
        
        // En yakın hedefi bul
        CollectibleObject closest = null;
        float minDistance = float.MaxValue;
        
        foreach (var target in targets)
        {   
            if (target == null || target.gameObject == null) 
            {
                continue; // Obje yok edilmişse bu turu geç
            }
            float distance = Vector2.Distance(transform.position, target.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = target;
            }

            if (closest == null) return "no_target";
            
        }
        
        // Durumu kategorize et
        float currentAngle = ropeSwing.GetCurrentAngle();
        Vector2 direction = closest.transform.position - transform.position;
        float targetAngle = Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg;
        float angleDiff = Mathf.Abs(currentAngle - targetAngle);
        
        // Durum: "mesafe_açıfarkı_objetürü"
        string distanceBucket = minDistance < 3f ? "close" : minDistance < 6f ? "medium" : "far";
        string angleBucket = angleDiff < 10f ? "aligned" : angleDiff < 30f ? "near" : "misaligned";
        string type = closest.objectType;
        
        string weightBucket = closest.weight <= 0.75f ? "light" : 
                              closest.weight <= 1.5f ? "medium" : "heavy";  // Yeni ağırlık kategorisi
    
        return $"{distanceBucket}_{angleBucket}_{type}_{weightBucket}";
    }
    
    int SelectAction(string state)
    {
        // Epsilon-greedy: Rastgele veya en iyi aksiyon
        if (Random.value < epsilon)
        {
            // Keşif (exploration)
            return Random.value > 0.3f ? 1 : 0; // %70 ateş, %30 bekle
        }
        else
        {
            // Sömürü (exploitation) - En iyi Q değeri
            float qWait = GetQValue(state, 0);
            float qShoot = GetQValue(state, 1);
            
            return qShoot > qWait ? 1 : 0;
        }
    }
    
    void UpdateQValue(string state, int action, float reward, string nextState)
    {
        // Q-Learning formülü:
        // Q(s,a) = Q(s,a) + α * [r + γ * max(Q(s',a')) - Q(s,a)]
        
        float currentQ = GetQValue(state, action);
        float maxNextQ = Mathf.Max(GetQValue(nextState, 0), GetQValue(nextState, 1));
        
        // Yeni Bilgi = Eski Bilgi + Öğrenme Hızı * (Ödül + Gelecek - Eski)
        float newQ = currentQ + learningRate * (reward + discountFactor * maxNextQ - currentQ);
        
        string key = $"{state}_{action}";
        qTable[key] = newQ;
    }

    // === EXPERIENCE REPLAY ===
    void StoreExperience(string state, int action, float reward, string nextState, bool done)
    {
        if (!useExperienceReplay) return;
        
        Experience exp = new Experience
        {
            state = state,
            action = action,
            reward = reward,
            nextState = nextState,
            done = done
        };
        
        experienceBuffer.Add(exp);
        
        // Buffer boyutu aşarsa eski deneyimleri sil
        if (experienceBuffer.Count > replayBufferSize)
        {
            experienceBuffer.RemoveAt(0);
        }
    }
    
    void ReplayExperiences()
    {
        // Rastgele batch seç ve tekrar öğren
        for (int i = 0; i < replayBatchSize; i++)
        {
            Experience exp = experienceBuffer[Random.Range(0, experienceBuffer.Count)];
            UpdateQValue(exp.state, exp.action, exp.reward, exp.nextState);
        }
    }
    float GetQValue(string state, int action)
    {
        string key = $"{state}_{action}";
        return qTable.ContainsKey(key) ? qTable[key] : 0f;
    }
    
    // Ödül verme fonksiyonları
    public void GiveReward(float reward)
    {
        lastReward += reward;
        episodeReward += reward;
    }
    
    public void OnGoldCollected(int points)
    {
        successfulShots++;
        
        // Dinamik ödül: değer bazlı
        float reward = points / 100f;
        
        // REWARD SHAPING: Büyük altınlara bonus
        if (points >= 500)
            reward += 0.5f;
        
        GiveReward(reward);
        Debug.Log($"💰 Q-Learning: Altın toplandı! Ödül: +{reward:F2}");
    }
    
    public void OnRockCollected(int points)
    {
        // Dinamik ceza: büyük taşa daha çok ceza
        float penalty = -(points / 20f);
        
        // REWARD SHAPING: Büyük taşa ekstra ceza
        if (points >= 20)
            penalty -= 0.3f;
        
        GiveReward(penalty);
        Debug.Log($"Q-Learning: Taş toplandı! Ceza: {penalty:F2}");
    }
    
    public void OnGameEnd(int score)
    {
        totalGamesPlayed++;
        rewardHistory.Add(episodeReward);
        
        // Son deneyimi kaydet
        if (!string.IsNullOrEmpty(lastState))
        {
            StoreExperience(lastState, lastAction, lastReward, "terminal", true);
        }
        
        // Epsilon azalt
        epsilon = Mathf.Max(minEpsilon, epsilon * epsilonDecay);
        
        // İstatistikler
        float hitRate = totalShots > 0 ? (successfulShots * 100f / totalShots) : 0f;
        float avgReward = rewardHistory.Count > 0 ? rewardHistory.Average() : 0f;
        
        Debug.Log($"   Q-Learning Oyun #{totalGamesPlayed} bitti");
        Debug.Log($"   Skor: {score} | ε: {epsilon:F3} | İsabet: {hitRate:F1}%");
        Debug.Log($"   Q-Table: {qTable.Count} | Replay Buffer: {experienceBuffer.Count}");
        Debug.Log($"   Ortalama Ödül: {avgReward:F2}");
        
        // Episode reset
        episodeReward = 0f;
        successfulShots = 0;
        totalShots = 0;
    }
    
    // === METRIK FONKSİYONLARI ===
    public float GetHitRate()
    {
        return totalShots > 0 ? (successfulShots * 100f / totalShots) : 0f;
    }
    
    public float GetAverageReward()
    {
        return rewardHistory.Count > 0 ? rewardHistory.Average() : 0f;
    }
    
    public int GetQTableSize()
    {
        return qTable.Count;
    }
    
    public void PrintTopQValues()
    {
        var sorted = qTable.OrderByDescending(x => x.Value).Take(10);
        Debug.Log("En İyi 10 Q-Değeri:");
        foreach (var pair in sorted)
        {
            Debug.Log($"  {pair.Key} = {pair.Value:F3}");
        }
    }
}