using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    // === OBJE ÖZELLİKLERİ ===
    public int pointValue = 10;      // Puan değeri
    public float weight = 1f;        // Ağırlık (çekme süresini etkiler)
    public string objectType = "gold"; // Obje tipi
    
    // Obje toplandığında çağrılacak
    public void Collect()
    {
        // Skoru artır
        bool isPlayer = GetComponentInParent<RopeLauncher>() != null;
        GameManager.instance.AddScore(pointValue, isPlayer);
        
        Debug.Log(objectType + " toplandı! Puan: " + pointValue);
        
        // Objeyi yok et
        Destroy(gameObject);
    }
}
