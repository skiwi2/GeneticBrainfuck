using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticBrainfuck.Algorithm
{
    public class GenerationStatistics<T>
    {
        public int AverageFitness { get; private set; }

        public int BestFitness { get; private set; }

        public LinkedList<T> BestIndividual { get; private set; }

        public GenerationStatistics(int averageFitness, int bestFitness, LinkedList<T> bestIndividual)
        {
            AverageFitness = averageFitness;
            BestFitness = bestFitness;
            BestIndividual = bestIndividual;
        }
    }
}
