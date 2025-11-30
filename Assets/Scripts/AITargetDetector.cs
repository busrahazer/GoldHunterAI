using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AITargetDetector : MonoBehaviour
{
    // === AYARLAR ===
    public float detectionRadius = 10f;  // Algılama yarıçapı
    public LayerMask targetLayer;        // Hangi layer'daki objeler hedef?
    
    // === HEDEF LİSTESİ ===
    private List<CollectibleObject> availableTargets = new List<CollectibleObject>();
    
    // Her frame'de hedefleri güncelle
    void Update()
    {
        DetectTargets();
    }
    
    void DetectTargets()
    {
        availableTargets.Clear();
        
        // Çevredeki tüm collider'ları bul
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        
        foreach (Collider2D col in colliders)
        {
            CollectibleObject collectible = col.GetComponent<CollectibleObject>();
            
            // Eğer collectible component varsa listeye ekle
            if (collectible != null)
            {
                availableTargets.Add(collectible);
            }
        }
    }
    
    // Tüm hedefleri döndür
    public List<CollectibleObject> GetAvailableTargets()
    {
        return availableTargets;
    }
    
    // Gizmos ile algılama alanını göster (editörde)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
