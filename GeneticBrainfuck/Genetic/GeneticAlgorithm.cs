using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticBrainfuck.Genetic
{
    public class GeneticAlgorithm<T> where T : struct
    {
        private Func<T> CreateRandomGen { get; set; }

        private T DeletedGenValue { get; set; }

        private Func<LinkedList<T>, int> CalculateFitness { get; set; }

        private Random Random { get; set; }

        private List<LinkedList<T>> Population { get; set; }

        public GeneticAlgorithm(Func<T> createRandomGen, T deletedGenValue, Func<LinkedList<T>, int> calculateFitness)
        {
            CreateRandomGen = createRandomGen;
            DeletedGenValue = deletedGenValue;
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
                linkedList.AddLast(CreateRandomGen());
            }
            return linkedList;
        }

        public void ComputeNextGeneration(double mutationRate, double insertionRate, double deletionRate)
        {
            var individualResults = ComputeIndividualResults();
            var newPopulation = CreateNewPopulation(individualResults);
            MutatePopulation(newPopulation, mutationRate, insertionRate, deletionRate);
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
                individualResult.NormalizedFitness = individualResult.Fitness / totalFitness;
            }

            var sortedIndividualResults = individualResults.OrderByDescending(individualResult => individualResult.NormalizedFitness).ToList();
            sortedIndividualResults[0].CumulativeNormalizedFitness = sortedIndividualResults[0].NormalizedFitness;
            for (int i = 1; i < sortedIndividualResults.Count; i++)
            {
                sortedIndividualResults[i].CumulativeNormalizedFitness = sortedIndividualResults[i - 1].CumulativeNormalizedFitness;
            }

            return sortedIndividualResults;
        }

        private List<LinkedList<T>> CreateNewPopulation(List<IndividualResult<T>> individualResults)
        {
            var newPopulation = new List<LinkedList<T>>(Population.Count);
            for (int i = 0; i < Population.Count; i++)
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

        private void MutatePopulation(List<LinkedList<T>> newPopulation, double mutationRate, double insertionRate, double deletionRate)
        {
            foreach (var individual in newPopulation)
            {
                var node = individual.First;
                int position = 0;
                while (node != null)
                {
                    if (mutationRate >= Random.NextDouble())
                    {
                        node.Value = CreateRandomGen();
                    }
                    if (insertionRate >= Random.NextDouble())
                    {
                        foreach (var innerIndividual in newPopulation)
                        {
                            innerIndividual.AddAfter(GetNthNode(innerIndividual, position), CreateRandomGen());
                        }
                        node = node.Next;
                    }
                    if (deletionRate >= Random.NextDouble())
                    {
                        node.Value = DeletedGenValue;
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
