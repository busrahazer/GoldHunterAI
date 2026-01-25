using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHeuristicController : MonoBehaviour
{
    // === REFERANSLAR ===
    private AITargetDetector detector;
    private RopeLauncher launcher;
    private RopeSwing ropeSwing;
    
    // === HEURISTIC KATSAYILARI ===
    public float valueWeight = 1f;      // Puan önem katsayısı
    public float distanceWeight = 1f;   // Mesafe önem katsayısı
    public float weightPenalty = 1f;    // Ağırlık ceza katsayısı
    
    // === KARAR AYARLARI ===
    public float decisionDelay = 0.5f;  // Karar verme gecikmesi (saniye)
    private float decisionTimer = 0f;
    
    // === HEDEF TAKIBI ===
    private CollectibleObject currentTarget = null;
    private float targetAngle = 0f;     // Hedefin açısı
    
    void Start()
    {
        detector = GetComponent<AITargetDetector>();
        launcher = GetComponent<RopeLauncher>();
        ropeSwing = GetComponent<RopeSwing>();
    }
    
    void Update()
    {
        // Oyun bittiyse karar verme
        if (GameManager.instance != null && !GameManager.instance.IsGameActive)
        return;
        // Eğer ip fırlatılmışsa karar verme
        if (launcher.IsLaunched())
            return;
        
        // DESTROY EDİLMİŞ HEDEF KONTROLÜ
        if (currentTarget == null)
        {
            currentTarget = null;
        }
        
        // Karar zamanlayıcısı
        decisionTimer += Time.deltaTime;
        
        if (decisionTimer >= decisionDelay)
        {
            decisionTimer = 0f;
            MakeDecision();
        }
        
        // Hedef varsa ve ip doğru açıdaysa ateş et
        if (currentTarget != null)
        {
            CheckAndShoot();
        }
    }
    
    void MakeDecision()
    {
        var targets = detector.GetAvailableTargets();
        
        if (targets.Count == 0)
        {
            currentTarget = null;
            return;
        }
        
        // En iyi hedefi bul
        CollectibleObject bestTarget = null;
        float bestScore = float.MinValue;
        
        foreach (CollectibleObject target in targets)
        {
            if (target == null || target.gameObject == null)
            {
                continue; // Yoksa hesaplama yapma, sıradakine geç
            }
            float score = CalculateHeuristicScore(target);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }
        
        currentTarget = bestTarget;
        
        if (currentTarget != null)
        {
            // Hedefe doğru açıyı hesapla
            Vector2 direction = currentTarget.transform.position - transform.position;
            targetAngle = Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg;
            
            // Debug.Log($"AI Hedef seçti: {currentTarget.objectType} | Skor: {bestScore:F2} | Açı: {targetAngle:F1}°");
        }
    }
    
    float CalculateHeuristicScore(CollectibleObject target)
    {
        // Mesafeyi hesapla
        float distance = Vector2.Distance(transform.position, target.transform.position);
        
        if (distance < 0.1f) distance = 0.1f; // Sıfıra bölmeyi önle
        
        // HEURİSTİK FORMÜL:
        // Score = (Puan / Mesafe) × (1 / Ağırlık)
        float score = (target.pointValue * valueWeight / distance) * (1f / (target.weight * weightPenalty));
        
        return score;
    }
    
    void CheckAndShoot()
    {
        if (currentTarget == null) return;
        
        // İpin şu anki açısını al
        float currentRopeAngle = ropeSwing.GetCurrentAngle();
        
        // Açı farkını hesapla
        float angleDifference = Mathf.Abs(currentRopeAngle - targetAngle);
        
        // Eğer ip hedefe yakın açıdaysa ateş et
        if (angleDifference < 5f) // 5 derece tolerans
        {
            // Debug.Log($"AI Ateş ediyor! Açı: {currentRopeAngle:F1}° | Hedef açı: {targetAngle:F1}°");
            launcher.LaunchRope();
            currentTarget = null; // Hedefi sıfırla
        }
    }
}
