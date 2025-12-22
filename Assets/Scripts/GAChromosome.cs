using UnityEngine;

[System.Serializable]
public class GAChromosome
{
    // === GENLER (Katsayılar) ===
    public float valueWeight = 1f;
    public float distanceWeight = 1f;
    public float weightPenalty = 1f;
    
    // === FİTNESS ===
    public float fitness = 0f;
    
    // Boş constructor
    public GAChromosome()
    {
        // Varsayılan değerler
        valueWeight = 1f;
        distanceWeight = 1f;
        weightPenalty = 1f;
    }
    
    // Belirli değerlerle oluştur
    public GAChromosome(float vw, float dw, float wp)
    {
        valueWeight = vw;
        distanceWeight = dw;
        weightPenalty = wp;
    }
    
    // Rastgele değerlerle başlat (Unity Start() içinden çağrılacak)
    public static GAChromosome CreateRandom()
    {
        return new GAChromosome(
            Random.Range(0.5f, 2.5f),
            Random.Range(0.5f, 2.5f),
            Random.Range(0.5f, 2.5f)
        );
    }
    
    // Kromozomu kopyala
    public GAChromosome Clone()
    {
        return new GAChromosome(valueWeight, distanceWeight, weightPenalty)
        {
            fitness = this.fitness
        };
    }
    
    // İki kromozomu çaprazla (crossover)
    public static GAChromosome Crossover(GAChromosome parent1, GAChromosome parent2)
    {
        return new GAChromosome(
            Random.value > 0.5f ? parent1.valueWeight : parent2.valueWeight,
            Random.value > 0.5f ? parent1.distanceWeight : parent2.distanceWeight,
            Random.value > 0.5f ? parent1.weightPenalty : parent2.weightPenalty
        );
    }
    
    // Mutasyon uygula
    public void Mutate(float mutationRate, float mutationAmount)
    {
        if (Random.value < mutationRate)
        {
            valueWeight += Random.Range(-mutationAmount, mutationAmount);
            valueWeight = Mathf.Clamp(valueWeight, 0.1f, 5f);
        }
        
        if (Random.value < mutationRate)
        {
            distanceWeight += Random.Range(-mutationAmount, mutationAmount);
            distanceWeight = Mathf.Clamp(distanceWeight, 0.1f, 5f);
        }
        
        if (Random.value < mutationRate)
        {
            weightPenalty += Random.Range(-mutationAmount, mutationAmount);
            weightPenalty = Mathf.Clamp(weightPenalty, 0.1f, 5f);
        }
    }
    
    public override string ToString()
    {
        return $"VW:{valueWeight:F2} DW:{distanceWeight:F2} WP:{weightPenalty:F2} | Fitness:{fitness:F0}";
    }
}