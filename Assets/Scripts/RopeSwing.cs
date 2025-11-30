using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeSwing : MonoBehaviour
{
    // === AYARLAR ===
    public float swingSpeed = 50f;        // Salınma hızı (derece/saniye)
    public float maxSwingAngle = 45f;     // Maksimum açı (sağa-sola)
    
    // === PRIVATE DEĞİŞKENLER ===
    private float currentAngle = 0f;      // Şu anki açı
    private int direction = 1;            // Yön (1 = sağa, -1 = sola)
    
    void Update()
    {
        // Her frame'de açıyı güncelle
        currentAngle += swingSpeed * direction * Time.deltaTime;
        
        // Maksimum açıya ulaştıysa yönü değiştir
        if (currentAngle >= maxSwingAngle)
        {
            currentAngle = maxSwingAngle;
            direction = -1; // Sola dön
        }
        else if (currentAngle <= -maxSwingAngle)
        {
            currentAngle = -maxSwingAngle;
            direction = 1; // Sağa dön
        }
        
        // İpin rotasyonunu ayarla (Z ekseninde dön)
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }

    public float GetCurrentAngle()
    {
        return currentAngle;
    }
}
