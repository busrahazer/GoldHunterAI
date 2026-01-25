using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GAManager : MonoBehaviour
{
    public static GAManager instance;
    
    public AIHeuristicController aiController;
    
    [Header("GA Settings")]
    public int populationSize = 20;
    public float mutationRate = 0.3f;
    public float mutationAmount = 0.5f;
    public int elitismCount = 2;
    
    private List<GAChromosome> population = new List<GAChromosome>();
    public int currentGeneration = 0;
    private int currentIndividualIndex = 0; // Hangi birey test ediliyor?
    
    [Header("Statistics")]
    public float bestFitnessEver = 0f;
    public float currentGenerationBest = 0f;
    public float currentGenerationAvg = 0f;
    
    private GAChromosome bestChromosomeEver;
    private List<float> fitnessHistory = new List<float>(); // Tüm fitness geçmişi
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Sahne değişse de kaybolmasın
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializePopulation();
        LoadNextIndividual();
    }
    
    void InitializePopulation()
    {
        if (population.Count > 0) return; // Zaten var
        
        population.Clear();
        
        for (int i = 0; i < populationSize; i++)
        {
            population.Add(GAChromosome.CreateRandom());
        }
        
        Debug.Log($" GA Başlatıldı | Popülasyon: {populationSize} | Nesil: {currentGeneration}");
    }
    
    void LoadNextIndividual()
    {
        if (currentIndividualIndex >= population.Count)
        {
            // Tüm bireyler test edildi, evrim zamanı
            EvolvePopulation();
            currentIndividualIndex = 0;
            currentGeneration++;
        }
        
        GAChromosome current = population[currentIndividualIndex];
        ApplyChromosome(current);
        
        Debug.Log($"🧬 Birey {currentIndividualIndex + 1}/{populationSize} yüklendi | Nesil: {currentGeneration}");
    }
    
    void ApplyChromosome(GAChromosome chromosome)
    {
        if (aiController == null)
        {
            Debug.LogError("❌ AI Controller bağlı değil!");
            return;
        }
        
        aiController.valueWeight = chromosome.valueWeight;
        aiController.distanceWeight = chromosome.distanceWeight;
        aiController.weightPenalty = chromosome.weightPenalty;
    }
    
    public void RecordGameResult(int aiScore)
    {
        if (currentIndividualIndex >= population.Count) return;
        
        GAChromosome current = population[currentIndividualIndex];
        current.fitness = aiScore;
        
        fitnessHistory.Add(aiScore); // Geçmişe ekle
        
        Debug.Log($" Birey {currentIndividualIndex + 1} Fitness: {aiScore}");
        
        if (aiScore > bestFitnessEver)
        {
            bestFitnessEver = aiScore;
            bestChromosomeEver = current.Clone();
            Debug.Log($"🏆 YENİ REKOR! {bestFitnessEver}");
        }
        
        currentIndividualIndex++;
        
        // Sıradaki bireyi yükle
        LoadNextIndividual();
    }
    
    void EvolvePopulation()
    {
        population = population.OrderByDescending(c => c.fitness).ToList();
        
        currentGenerationBest = population[0].fitness;
        currentGenerationAvg = population.Average(c => c.fitness);
        
        Debug.Log($"\n===  EVRİM: NESİL {currentGeneration} → {currentGeneration + 1} ===");
        Debug.Log($" En İyi: {population[0]}");
        Debug.Log($" En Kötü: {population[population.Count - 1]}");
        Debug.Log($" Ortalama: {currentGenerationAvg:F0}\n");
        
        List<GAChromosome> newPopulation = new List<GAChromosome>();
        
        // Elitizm
        for (int i = 0; i < elitismCount && i < population.Count; i++)
        {
            newPopulation.Add(population[i].Clone());
        }
        
        // Çaprazlama ve Mutasyon
        while (newPopulation.Count < populationSize)
        {
            GAChromosome parent1 = TournamentSelection();
            GAChromosome parent2 = TournamentSelection();
            
            GAChromosome child = GAChromosome.Crossover(parent1, parent2);
            child.Mutate(mutationRate, mutationAmount);
            
            newPopulation.Add(child);
        }
        
        population = newPopulation;
    }
    
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
    
    // Fitness geçmişini göster
    public string GetFitnessHistory()
    {
        if (fitnessHistory.Count == 0) return "Henüz veri yok";
        
        string history = "Son 10 Oyun Fitness:\n";
        int start = Mathf.Max(0, fitnessHistory.Count - 10);
        for (int i = start; i < fitnessHistory.Count; i++)
        {
            history += $"Oyun {i + 1}: {fitnessHistory[i]:F0}\n";
        }
        
        return history;
    }
}