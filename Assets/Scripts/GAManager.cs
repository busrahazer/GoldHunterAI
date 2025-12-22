using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GAManager : MonoBehaviour
{
    // === SINGLETON ===
    public static GAManager instance;
    
    // === REFERANSLAR ===
    public AIHeuristicController aiController;
    
    // === GA AYARLARI ===
    [Header("GA Settings")]
    public int populationSize = 10;           // Küçük başlayalım
    public float mutationRate = 0.3f;         // %30 mutasyon
    public float mutationAmount = 0.5f;       // Mutasyon miktarı
    public int elitismCount = 2;              // En iyi 2 birey korunur
    
    // === POPÜLASYON ===
    private List<GAChromosome> population = new List<GAChromosome>();
    private int currentGeneration = 0;
    
    // === İSTATİSTİKLER ===
    [Header("Statistics (Read Only)")]
    public float bestFitnessEver = 0f;
    public float currentGenerationBest = 0f;
    public float currentGenerationAvg = 0f;
    
    private GAChromosome bestChromosomeEver;
    
    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        InitializePopulation();
        LoadBestChromosome();
    }
    
    // İlk popülasyonu oluştur
    void InitializePopulation()
    {
        population.Clear();
        
        // Rastgele kromozomlar oluştur
        for (int i = 0; i < populationSize; i++)
        {
            population.Add(GAChromosome.CreateRandom());
        }
        
        Debug.Log($" GA Başlatıldı | Popülasyon: {populationSize} | Nesil: {currentGeneration}");
    }
    
    // En iyi kromozomu AI'a yükle (oyun başında)
    void LoadBestChromosome()
    {
        if (population.Count == 0) return;
        
        // İlk başta popülasyondan rastgele birini seç
        GAChromosome firstChromosome = population[Random.Range(0, population.Count)];
        
        if (bestChromosomeEver != null)
        {
            // Eğer daha önce eğitim yapılmışsa, en iyiyi kullan
            ApplyChromosome(bestChromosomeEver);
            Debug.Log($" En iyi öğrenilmiş kromozom yüklendi: {bestChromosomeEver}");
        }
        else
        {
            // İlk oyunda rastgele kromozom
            ApplyChromosome(firstChromosome);
            Debug.Log($" İlk kromozom yüklendi: {firstChromosome}");
        }
    }
    
    // Kromozomu AI'a uygula
    void ApplyChromosome(GAChromosome chromosome)
    {
        aiController.valueWeight = chromosome.valueWeight;
        aiController.distanceWeight = chromosome.distanceWeight;
        aiController.weightPenalty = chromosome.weightPenalty;
    }
    
    // Oyun bittiğinde fitness kaydet
    public void RecordGameResult(int aiScore)
    {
        // Şu anki kromozoma fitness ata
        GAChromosome currentChromosome = new GAChromosome(
            aiController.valueWeight,
            aiController.distanceWeight,
            aiController.weightPenalty
        );
        currentChromosome.fitness = aiScore;
        
        // Popülasyona ekle (eğer yoksa)
        bool found = false;
        for (int i = 0; i < population.Count; i++)
        {
            if (IsSimilar(population[i], currentChromosome))
            {
                population[i].fitness = Mathf.Max(population[i].fitness, aiScore);
                found = true;
                break;
            }
        }
        
        if (!found && population.Count < populationSize * 2)
        {
            population.Add(currentChromosome);
        }
        
        Debug.Log($" AI Performansı Kaydedildi: {aiScore} puan");
        
        // En iyi kromozomu güncelle
        if (aiScore > bestFitnessEver)
        {
            bestFitnessEver = aiScore;
            bestChromosomeEver = currentChromosome.Clone();
            Debug.Log($"YENİ REKOR! En İyi Fitness: {bestFitnessEver}");
            Debug.Log($"En İyi Kromozom: {bestChromosomeEver}");
        }
        
        // Her 5 oyunda bir evrim
        if (population.Count >= populationSize)
        {
            EvolvePopulation();
        }
    }
    
    // Yeni nesil oluştur
    void EvolvePopulation()
    {
        // Fitness'e göre sırala
        population = population.OrderByDescending(c => c.fitness).ToList();
        
        // İstatistikleri hesapla
        currentGenerationBest = population[0].fitness;
        currentGenerationAvg = population.Average(c => c.fitness);
        
        Debug.Log($"");
        Debug.Log($"=== 🧬 EVRİM: NESİL {currentGeneration} → {currentGeneration + 1} ===");
        Debug.Log($"📈 En İyi: {population[0]}");
        Debug.Log($"📉 En Kötü: {population[population.Count - 1]}");
        Debug.Log($"📊 Ortalama: {currentGenerationAvg:F0}");
        Debug.Log($"");
        
        List<GAChromosome> newPopulation = new List<GAChromosome>();
        
        // 1. Elitizm: En iyileri koru
        for (int i = 0; i < elitismCount && i < population.Count; i++)
        {
            newPopulation.Add(population[i].Clone());
        }
        
        // 2. Yeni nesil oluştur
        while (newPopulation.Count < populationSize)
        {
            GAChromosome parent1 = TournamentSelection();
            GAChromosome parent2 = TournamentSelection();
            
            GAChromosome child = GAChromosome.Crossover(parent1, parent2);
            child.Mutate(mutationRate, mutationAmount);
            
            newPopulation.Add(child);
        }
        
        population = newPopulation;
        currentGeneration++;
        
        // Yeni neslin en iyi kromozomunu AI'a yükle
        LoadBestChromosome();
    }
    
    // Turnuva seçimi
    GAChromosome TournamentSelection()
    {
        int tournamentSize = 3;
        GAChromosome best = null;
        
        for (int i = 0; i < tournamentSize; i++)
        {
            GAChromosome candidate = population[Random.Range(0, population.Count)];
            
            if (best == null || candidate.fitness > best.fitness)
            {
                best = candidate;
            }
        }
        
        return best;
    }
    
    // İki kromozom benzer mi?
    bool IsSimilar(GAChromosome a, GAChromosome b)
    {
        float threshold = 0.1f;
        return Mathf.Abs(a.valueWeight - b.valueWeight) < threshold &&
               Mathf.Abs(a.distanceWeight - b.distanceWeight) < threshold &&
               Mathf.Abs(a.weightPenalty - b.weightPenalty) < threshold;
    }
}