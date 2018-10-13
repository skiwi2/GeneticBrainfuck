using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticBrainfuck.Algorithm

{
    public class GeneticAlgorithm<T> where T : struct
    {
        private Func<T, Random, T> CreateNewRandomGen { get; set; }

        private T NullGenValue { get; set; }

        private Predicate<LinkedList<T>> ValidateIndividual { get; set; }

        private Func<LinkedList<T>, int> CalculateFitness { get; set; }

        private Random Random { get; set; }

        private List<LinkedList<T>> Population { get; set; }

        private List<IndividualStatistics<T>> IndividualStatistics { get; set; }

        public GeneticAlgorithm(Func<T, Random, T> createNewRandomGen, T nullGenValue, Predicate<LinkedList<T>> validateIndividual, Func<LinkedList<T>, int> calculateFitness)
        {
            CreateNewRandomGen = createNewRandomGen;
            NullGenValue = nullGenValue;
            ValidateIndividual = validateIndividual;
            CalculateFitness = calculateFitness;
            Random = new Random();
        }

        public GenerationStatistics<T> GetGenerationStatistics()
        {
            int averageFitness = (int)Math.Round(IndividualStatistics.Select(individualResult => individualResult.Fitness).Average());
            return new GenerationStatistics<T>(averageFitness, IndividualStatistics[0].Fitness, IndividualStatistics[0].Individual);
        }

        public void InitializePopulation(int populationSize, int individualSize)
        {
            Population = new List<LinkedList<T>>(populationSize);
            for (int i = 0; i < populationSize; i++)
            {
                Population.Add(CreateIndividual(individualSize));
            }
            ComputeIndividualStatistics();
        }

        private LinkedList<T> CreateIndividual(int individualSize)
        {
            var linkedList = new LinkedList<T>();
            for (int i = 0; i < individualSize; i++)
            {
                linkedList.AddLast(CreateNewRandomGen(NullGenValue, Random));
            }
            return linkedList;
        }

        public void ComputeNextGeneration(double elitismFactor, double mutationRate, double insertionRate, double deletionRate)
        {
            var numberOfElites = (int)(Math.Ceiling(elitismFactor * Population.Count));

            var elites = RetrieveElites(numberOfElites);
            var newPopulation = CreateNewPopulation(Population.Count - numberOfElites);
            MutateNewPopulation(newPopulation, elites, mutationRate, insertionRate, deletionRate);
            newPopulation.AddRange(elites);
            Debug.Assert(newPopulation.Count == Population.Count);
            Population = newPopulation;
            ComputeIndividualStatistics();
        }

        private void ComputeIndividualStatistics()
        {
            var individualStatistics = new List<IndividualStatistics<T>>(Population.Count);
            foreach (var individual in Population)
            {
                individualStatistics.Add(new IndividualStatistics<T>(individual, CalculateFitness(individual)));
            }

            var totalFitness = individualStatistics.Select(individualResult => individualResult.Fitness).Sum();
            foreach (var individualResult in individualStatistics)
            {
                individualResult.NormalizedFitness = (double)individualResult.Fitness / totalFitness;
            }

            var sortedIndividualStatistics = individualStatistics.OrderByDescending(individualResult => individualResult.NormalizedFitness).ToList();
            sortedIndividualStatistics[0].CumulativeNormalizedFitness = sortedIndividualStatistics[0].NormalizedFitness;
            for (int i = 1; i < sortedIndividualStatistics.Count; i++)
            {
                sortedIndividualStatistics[i].CumulativeNormalizedFitness = sortedIndividualStatistics[i - 1].CumulativeNormalizedFitness + sortedIndividualStatistics[i].NormalizedFitness;
            }

            IndividualStatistics = sortedIndividualStatistics;
        }

        private List<LinkedList<T>> RetrieveElites(int numberOfElites)
        {
            var elites = new List<LinkedList<T>>();
            foreach (var individualResult in IndividualStatistics.Take(numberOfElites))
            {
                elites.Add(individualResult.Individual);
            }
            return elites;
        }

        private List<LinkedList<T>> CreateNewPopulation(int newPopulationSize)
        {
            var newPopulation = new List<LinkedList<T>>(newPopulationSize);
            for (int i = 0; i < newPopulationSize; i++)
            {
                LinkedList<T> newIndividual = null;
                do
                {
                    var leftParent = GetWeightedRandomIndividual();
                    LinkedList<T> rightParent;
                    do
                    {
                        rightParent = GetWeightedRandomIndividual();
                    }
                    while (Enumerable.SequenceEqual(leftParent, rightParent));
                    newIndividual = Crossover(leftParent, rightParent);
                }
                while (!ValidateIndividual(newIndividual));
                newPopulation.Add(newIndividual);
            }
            return newPopulation;
        }

        private LinkedList<T> GetWeightedRandomIndividual()
        {
            if (Double.IsNaN(IndividualStatistics[0].NormalizedFitness))
            {
                // apparently the sum of fitnesses was zero, so we can just pick any individual
                return IndividualStatistics[Random.Next(Population.Count)].Individual;
            }

            var threshold = Random.NextDouble();
            var dummyIndividualResult = new IndividualStatistics<T>(null, 0)
            {
                CumulativeNormalizedFitness = threshold
            };
            var comparer = Comparer<IndividualStatistics<T>>.Create((left, right) => left.CumulativeNormalizedFitness.CompareTo(right.CumulativeNormalizedFitness));
            var index = IndividualStatistics.BinarySearch(dummyIndividualResult, comparer);
            if (index > 0)
            {
                return IndividualStatistics[index].Individual;
            }
            else
            {
                return IndividualStatistics[~index].Individual;
            }
        }

        private LinkedList<T> Crossover(LinkedList<T> leftParent, LinkedList<T> rightParent)
        {
            Debug.Assert(leftParent.Count == rightParent.Count);
            var crossoverPoint = Random.Next(leftParent.Count);
            // TODO could be more efficient
            var newIndividual = new LinkedList<T>();
            foreach (var leftGen in leftParent.Take(crossoverPoint))
            {
                newIndividual.AddLast(leftGen);
            }
            foreach (var rightGen in rightParent.Skip(crossoverPoint))
            {
                newIndividual.AddLast(rightGen);
            }
            return newIndividual;
        }

        private void MutateNewPopulation(List<LinkedList<T>> newPopulation, List<LinkedList<T>> elites, double mutationRate, double insertionRate, double deletionRate)
        {
            foreach (var individual in newPopulation)
            {
                var node = individual.First;
                int position = 0;
                while (node != null)
                {
                    if (!EqualityComparer<T>.Default.Equals(node.Value, NullGenValue) && mutationRate >= Random.NextDouble())
                    {
                        node.Value = CreateNewRandomGen(node.Value, Random);
                    }
                    if (!EqualityComparer<T>.Default.Equals(node.Value, NullGenValue) && deletionRate >= Random.NextDouble())
                    {
                        node.Value = NullGenValue;
                    }
                    if (insertionRate >= Random.NextDouble())
                    {
                        if (EqualityComparer<T>.Default.Equals(node.Value, NullGenValue))
                        {
                            node.Value = CreateNewRandomGen(node.Value, Random);
                        }
                        else
                        {
                            foreach (var innerIndividual in newPopulation)
                            {
                                innerIndividual.AddAfter(GetNthNode(innerIndividual, position), NullGenValue);
                            }
                            foreach (var elite in elites)
                            {
                                elite.AddAfter(GetNthNode(elite, position), NullGenValue);
                            }
                            node.Value = CreateNewRandomGen(NullGenValue, Random);
                            node = node.Next;
                        }
                    }
                    node = node.Next;
                    position++;
                }
            }
        }

        private LinkedListNode<T> GetNthNode(LinkedList<T> linkedList, int index)
        {
            var node = linkedList.First;
            int position = 0;
            while (node != null)
            {
                if (position == index)
                {
                    return node;
                }
                node = node.Next;
                position++;
            }
            return null;
        }
    }
}
