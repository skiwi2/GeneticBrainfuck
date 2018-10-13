using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticBrainfuck.Algorithm
{
    public class IndividualStatistics<T>
    {
        public LinkedList<T> Individual { get; private set; }

        public int Fitness { get; private set; }

        public double NormalizedFitness { get; set; }

        public double CumulativeNormalizedFitness { get; set; }

        public IndividualStatistics(LinkedList<T> individual, int fitness)
        {
            Individual = individual;
            Fitness = fitness;
            NormalizedFitness = 0d;
            CumulativeNormalizedFitness = 0d;
        }
    }
}
