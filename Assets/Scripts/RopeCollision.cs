using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeCollision : MonoBehaviour
{
    private bool hasCollected = false;
    private RopeLauncher launcher;
    
    void Start()
    {
        // Parent'taki RopeLauncher'ı bul
        launcher = GetComponentInParent<RopeLauncher>();
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        // Eğer zaten bir obje toplandıysa, başka obje toplama
        if (hasCollected)
        {
            Debug.Log("Zaten toplandı, atlanıyor");
            return;
        }
        
        Debug.Log("Çarpışma algılandı: " + collision.gameObject.name);
        
        // Çarpılan objenin CollectibleObject component'i var mı?
        CollectibleObject collectible = collision.GetComponent<CollectibleObject>();
        
        if (collectible != null)
        {
            Debug.Log("Collectible bulundu!");
            hasCollected = true;
            
            // Parent isminden Player/AI ayırt et
            Transform parent = transform.parent;
            bool isPlayer = parent.name.Contains("Player"); // "Player" kelimesi var mı?
            
            Debug.Log($"{collectible.objectType} toplandı! Parent: {parent.name} | isPlayer: {isPlayer}");
        
            // Q-Learning Agent'a ödül ver
            QLearningAgent qAgent = GetComponentInParent<QLearningAgent>();
            if (qAgent != null)
            {
                if (collectible.objectType == "gold")
                    qAgent.OnGoldCollected(collectible.pointValue);
                else
                    qAgent.OnRockCollected(collectible.pointValue);
            }
            // Skoru ekle
            if (GameManager.instance != null)
            {
                GameManager.instance.AddScore(collectible.pointValue, isPlayer);
            }
            
            // Objeyi yok et
            Destroy(collision.gameObject);
            
            // İpi geri çek
            if (launcher != null)
            {
                launcher.StartRetract();
            }
        }
        else
        {
            Debug.Log("Collectible component bulunamadı!");
        }
    }
    
    // İp TAM OLARAK başlangıç pozisyonuna döndüğünde reset yap
    public void ResetCollection()
    {
        hasCollected = false;
        Debug.Log("RopeCollision reset edildi - yeni obje toplanabilir");
    }
}