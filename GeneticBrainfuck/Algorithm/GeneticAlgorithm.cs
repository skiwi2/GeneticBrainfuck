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

        private Func<LinkedList<T>, int> CalculateFitness { get; set; }

        private Random Random { get; set; }

        private List<LinkedList<T>> Population { get; set; }

        public GeneticAlgorithm(Func<T, Random, T> createNewRandomGen, T nullGenValue, Func<LinkedList<T>, int> calculateFitness)
        {
            CreateNewRandomGen = createNewRandomGen;
            NullGenValue = nullGenValue;
            CalculateFitness = calculateFitness;
            Random = new Random();
        }

        public void InitializePopulation(int populationSize, int individualSize)
        {
            Population = new List<LinkedList<T>>(populationSize);
            for (int i = 0; i < populationSize; i++)
            {
                Population.Add(CreateIndividual(individualSize));
            }
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
            var individualResults = ComputeIndividualResults();

            var numberOfElites = (int)(Math.Ceiling(elitismFactor * Population.Count));

            var elites = RetrieveElites(individualResults, numberOfElites);
            var newPopulation = CreateNewPopulation(individualResults, Population.Count - numberOfElites);
            MutateNewPopulation(newPopulation, elites, mutationRate, insertionRate, deletionRate);
            newPopulation.AddRange(elites);
            Debug.Assert(newPopulation.Count == Population.Count);
            Population = newPopulation;
        }

        private List<IndividualResult<T>> ComputeIndividualResults()
        {
            var individualResults = new List<IndividualResult<T>>(Population.Count);
            foreach (var individual in Population)
            {
                individualResults.Add(new IndividualResult<T>(individual, CalculateFitness(individual)));
            }

            var totalFitness = individualResults.Select(individualResult => individualResult.Fitness).Sum();
            foreach (var individualResult in individualResults)
            {
                individualResult.NormalizedFitness = (double)individualResult.Fitness / totalFitness;
            }

            var sortedIndividualResults = individualResults.OrderByDescending(individualResult => individualResult.NormalizedFitness).ToList();
            sortedIndividualResults[0].CumulativeNormalizedFitness = sortedIndividualResults[0].NormalizedFitness;
            for (int i = 1; i < sortedIndividualResults.Count; i++)
            {
                sortedIndividualResults[i].CumulativeNormalizedFitness = sortedIndividualResults[i - 1].CumulativeNormalizedFitness + sortedIndividualResults[i].NormalizedFitness;
            }

            return sortedIndividualResults;
        }

        private List<LinkedList<T>> RetrieveElites(List<IndividualResult<T>> individualResults, int numberOfElites)
        {
            var elites = new List<LinkedList<T>>();
            foreach (var individualResult in individualResults.Take(numberOfElites))
            {
                elites.Add(individualResult.Individual);
            }
            return elites;
        }

        private List<LinkedList<T>> CreateNewPopulation(List<IndividualResult<T>> individualResults, int newPopulationSize)
        {
            var newPopulation = new List<LinkedList<T>>(newPopulationSize);
            for (int i = 0; i < newPopulationSize; i++)
            {
                var leftParent = GetWeightedRandomIndividual(individualResults);
                var rightParent = GetWeightedRandomIndividual(individualResults);
                newPopulation.Add(Crossover(leftParent, rightParent));
            }
            return newPopulation;
        }

        private LinkedList<T> GetWeightedRandomIndividual(List<IndividualResult<T>> individualResults)
        {
            var threshold = Random.NextDouble();
            var dummyIndividualResult = new IndividualResult<T>(null, 0)
            {
                CumulativeNormalizedFitness = threshold
            };
            var comparer = Comparer<IndividualResult<T>>.Create((left, right) => left.CumulativeNormalizedFitness.CompareTo(right.CumulativeNormalizedFitness));
            var index = individualResults.BinarySearch(dummyIndividualResult, comparer);
            if (index > 0)
            {
                return individualResults[index].Individual;
            }
            else
            {
                return individualResults[~index].Individual;
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
                    if (mutationRate >= Random.NextDouble())
                    {
                        node.Value = CreateNewRandomGen(node.Value, Random);
                    }
                    if (!EqualityComparer<T>.Default.Equals(node.Value, NullGenValue) && insertionRate >= Random.NextDouble())
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
                    if (!EqualityComparer<T>.Default.Equals(node.Value, NullGenValue) && deletionRate >= Random.NextDouble())
                    {
                        node.Value = NullGenValue;
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
