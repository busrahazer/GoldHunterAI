using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeLauncher : MonoBehaviour
{
    // === REFERANSLAR ===
    public Transform rope;                // İp objesi
    public KeyCode launchKey = KeyCode.Space;  // Fırlatma tuşu
    
    // === AYARLAR ===
    public float extendSpeed = 5f;        // İpin uzama hızı
    public float retractSpeed = 8f;       // İpin geri çekilme hızı
    public float maxLength = 6f;          // Maksimum ip uzunluğu
    
    // === DURUM DEĞİŞKENLERİ ===
    private bool isLaunched = false;      // İp fırlatıldı mı?
    private bool isRetracting = false;    // İp geri mi çekiliyor?
    private float currentLength = 1f;     // Şu anki ip uzunluğu
    private Vector3 originalScale;        // İpin orijinal boyutu
    private RopeSwing ropeSwing;          // İp salınma script'i
    
    void Start()
    {
        // Başlangıç değerlerini kaydet
        originalScale = rope.localScale;
        ropeSwing = GetComponent<RopeSwing>();
    }
    
    void Update()
    {
        // Space tuşuna basıldıysa ve ip fırlatılmamışsa
        if (Input.GetKeyDown(launchKey) && !isLaunched)
        {
            LaunchRope();
        }
        
        // İp fırlatıldıysa
        if (isLaunched)
        {
            if (!isRetracting)
            {
                // İpi uzat
                ExtendRope();
            }
            else
            {
                // İpi geri çek
                RetractRope();
            }
        }
    }
    
    public void LaunchRope()
    {
        isLaunched = true;
        isRetracting = false;
        ropeSwing.enabled = false; // Salınmayı durdur
    }
    
    void ExtendRope()
    {
        // İpi uzat
        currentLength += extendSpeed * Time.deltaTime;
        rope.localScale = new Vector3(originalScale.x, currentLength, originalScale.z);
        
        // İp pozisyonunu ayarla (aşağı doğru uzasın)
        rope.localPosition = new Vector3(0, -currentLength / 2f, 0);
        
        // Maksimum uzunluğa ulaştıysa geri çekmeye başla
        if (currentLength >= maxLength)
        {
            isRetracting = true;
        }
    }

    // İpi zorla geri çekmeye başlat
    public void StartRetract()
    {
        isRetracting = true;
    }
    
    void RetractRope()
    {
        // İpi geri çek
        currentLength -= retractSpeed * Time.deltaTime;
        rope.localScale = new Vector3(originalScale.x, currentLength, originalScale.z);
        rope.localPosition = new Vector3(0, -currentLength / 2f, 0);
        
        // İp orijinal boyutuna döndüyse
        if (currentLength <= 1f)
        {
            currentLength = 1f;
            isLaunched = false;
            ropeSwing.enabled = true; // Salınmayı tekrar başlat

            RopeCollision ropeCollision = rope.GetComponent<RopeCollision>();
            if (ropeCollision != null)
            {
                ropeCollision.ResetCollection();
            }
        }
    }

    public bool IsLaunched()
    {
        return isLaunched;
    }
}
