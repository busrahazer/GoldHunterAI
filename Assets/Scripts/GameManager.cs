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
    
    // === OYUN DEĞİŞKENLERİ ===
    public float gameDuration = 60f;     // Oyun süresi (saniye)
    private float timeRemaining;         // Kalan süre
    private int playerScore = 0;         // Oyuncu skoru
    private int aiScore = 0;             // AI skoru
    private bool gameActive = true;      // Oyun aktif mi?
    
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
        timeRemaining = gameDuration;
        UpdateUI();
    }
    
    void Update()
    {
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
        playerScoreText.text = "Player: " + playerScore;
        aiScoreText.text = "AI: " + aiScore;
        timerText.text = "Time: " + Mathf.Ceil(timeRemaining);
    }
    
    void EndGame()
    {
        gameActive = false;
        Debug.Log("OYUN BİTTİ!");
        Debug.Log("Oyuncu Skoru: " + playerScore);
        Debug.Log("AI Skoru: " + aiScore);
        
        if (playerScore > aiScore)
            Debug.Log("OYUNCU KAZANDI!");
        else if (aiScore > playerScore)
            Debug.Log("AI KAZANDI!");
        else
            Debug.Log("BERABERE!");

        // GA'ya AI skorunu bildir
        if (GAManager.instance != null)
        {
            GAManager.instance.RecordGameResult(aiScore);
        }  
    }
}
