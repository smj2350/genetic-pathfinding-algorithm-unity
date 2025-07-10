using System.Collections.Generic;

public class Genetic
{
    public List<Genome> genomes;
    private readonly List<Genome> _lastGenerationGenomes;

    private const double CrossoverRate = 0.7f;
    private const double MutationRate = 0.001f;

    private const int PopulationSize = 140;
    private const int ChromosomeLength = 70;
    private const int GeneLength = 2;

    public int fittestGenome;

    private double _bestFitnessScore;
    private double _totalFitnessScore;

    public int generation;

    public MazeController mazeController;

    public bool busy;

    public Genetic()
    {
        busy = false;
        genomes = new List<Genome>();
        _lastGenerationGenomes = new List<Genome>();
    }

    private static void Mutate(IList<int> bits)
    {
        for (var i = 0; i < bits.Count; i++)
        {
            if (UnityEngine.Random.value < MutationRate)
            {
                bits[i] = bits[i] == 0 ? 1 : 0;
            }
        }
    }

    private static void Crossover(IReadOnlyList<int> mom, IReadOnlyList<int> dad, List<int> baby1, List<int> baby2)
    {
        if (UnityEngine.Random.value > CrossoverRate || Equals(mom, dad))
        {
            baby1.AddRange(mom);
            baby2.AddRange(dad);

            return;
        }

        var rnd = new System.Random();

        var crossoverPoint = rnd.Next(0, ChromosomeLength - 1);

        for (var i = 0; i < crossoverPoint; i++)
        {
            baby1.Add(mom[i]);
            baby2.Add(dad[i]);
        }

        for (var i = crossoverPoint; i < mom.Count; i++)
        {
            baby1.Add(dad[i]);
            baby2.Add(mom[i]);
        }
    }

    private Genome RouletteWheelSelection()
    {
        var slice = UnityEngine.Random.value * _totalFitnessScore;
        double total = 0;
        var selectedGenome = 0;

        for (var i = 0; i < PopulationSize; i++)
        {
            total += genomes[i].fitness;

            if (!(total > slice)) continue;
            selectedGenome = i;
            break;
        }
        return genomes[selectedGenome];
    }

    private void UpdateFitnessScores()
    {
        fittestGenome = 0;
        _bestFitnessScore = 0;
        _totalFitnessScore = 0;

        for (var i = 0; i < PopulationSize; i++)
        {
            var directions = Decode(genomes[i].bits);

            genomes[i].fitness = mazeController.TestRoute(directions);

            _totalFitnessScore += genomes[i].fitness;

            if (!(genomes[i].fitness > _bestFitnessScore)) continue;

            _bestFitnessScore = genomes[i].fitness;
            fittestGenome = i;

            if (genomes[i].fitness != 1) continue;

            busy = false;

            return;
        }
    }

    //0 위 1 아래 2 오른쪽 3 왼쪽
    public static IEnumerable<int> Decode(List<int> bits)
    {
        var directions = new List<int>();

        for (var geneIndex = 0; geneIndex < bits.Count; geneIndex += GeneLength)
        {
            var gene = new List<int>();

            for (var bitIndex = 0; bitIndex < GeneLength; bitIndex++)
            {
                gene.Add(bits[geneIndex + bitIndex]);
            }

            directions.Add(GeneToInt(gene));
        }
        return directions;
    }

    private static int GeneToInt(IReadOnlyList<int> gene)
    {
        var value = 0;
        var multiplier = 1;

        for (var i = gene.Count; i > 0; i--)
        {
            value += gene[i - 1] * multiplier;
            multiplier *= 2;
        }
        return value;
    }

    private void CreateStartPopulation()
    {
        genomes.Clear();

        for (var i = 0; i < PopulationSize; i++)
        {
            var baby = new Genome(ChromosomeLength);
            genomes.Add(baby);
        }
    }

    public void Run()
    {
        CreateStartPopulation();
        busy = true;
    }

    public void Epoch()
    {
        if (!busy)
            return;

        UpdateFitnessScores();

        if (!busy)
        {
            _lastGenerationGenomes.Clear();
            _lastGenerationGenomes.AddRange(genomes);
            DisplayManager.Instance.timechk = false;
            return;
        }

        var numberOfNewBabies = 0;

        var babies = new List<Genome>();
        while (numberOfNewBabies < PopulationSize)
        {
            var mom = RouletteWheelSelection();
            var dad = RouletteWheelSelection();
            var baby1 = new Genome();
            var baby2 = new Genome();
            Crossover(mom.bits, dad.bits, baby1.bits, baby2.bits);
            Mutate(baby1.bits);
            Mutate(baby2.bits);
            babies.Add(baby1);
            babies.Add(baby2);

            numberOfNewBabies += 2;
        }

        // 디스플레이용
        _lastGenerationGenomes.Clear();
        _lastGenerationGenomes.AddRange(genomes);
        genomes = babies;

        // 세대 증가
        generation++;
    }
}
