using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using GeneticBrainfuck.Algorithm;
using GeneticBrainfuck.Interpreter;

namespace GeneticBrainfuck
{
    class Program
    {
        static void Main(string[] args)
        {
            var testcases = new List<Testcase>
            {
                new Testcase(new List<byte> { 1 }, new List<byte> { 1 }),
                new Testcase(new List<byte> { 2 }, new List<byte> { 2 }),
                new Testcase(new List<byte> { 3 }, new List<byte> { 3 })
            };
            var geneticAlgorithm = new GeneticAlgorithm<BrainfuckGen>(CreateNewBrainfuckGen, BrainfuckGen.Null, individual => CalculateFitness(individual, testcases));
            geneticAlgorithm.InitializePopulation(100, 2);
            while (true)
            {
                geneticAlgorithm.ComputeNextGeneration(0.02d, 0.001d, 0.001d);
            }
        }

        private static BrainfuckGen CreateNewBrainfuckGen(BrainfuckGen oldGen, Random random)
        {
            var possibleGenes = ((BrainfuckGen[])Enum.GetValues(typeof(BrainfuckGen))).ToList();
            possibleGenes.Remove(oldGen);
            return possibleGenes[random.Next(possibleGenes.Count)];
        }

        private static int CalculateFitness(LinkedList<BrainfuckGen> individual, IList<Testcase> testcases) 
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token;
            var task = Task.Run(() => {
                int fitness = 0;
                var programText = new string(individual.Where(brainfuckGen => brainfuckGen != BrainfuckGen.Null).Select(ToBFChar).ToArray());
                try
                {
                    var program = new BFProgram(programText, new BFMemory(100));
                    foreach (var testcase in testcases)
                    {
                        fitness += CalculateOutputScore(testcase.Output, program.Execute(testcase.Input, cancellationToken));
                    }
                }
                catch (InvalidBFProgramException)
                {
                    // skip
                }
                fitness = (fitness > 0) ? fitness : 0;
                Console.WriteLine($"Fitness for {programText}: {fitness}");
                return fitness;
            }, cancellationToken);
            return task.Result;
        }

        private static char ToBFChar(BrainfuckGen brainfuckGen)
        {
            switch (brainfuckGen)
            {
                case BrainfuckGen.MoveRight:
                    return '>';
                case BrainfuckGen.MoveLeft:
                    return '<';
                case BrainfuckGen.Increment:
                    return '+';
                case BrainfuckGen.Decrement:
                    return '-';
                case BrainfuckGen.Input:
                    return ',';
                case BrainfuckGen.Output:
                    return '.';
                case BrainfuckGen.LoopBegin:
                    return '[';
                case BrainfuckGen.LoopEnd:
                    return ']';
                default:
                    throw new ArgumentException($"Unknown brainfuckGen: {brainfuckGen}");
            }
        }

        private static int CalculateOutputScore(IList<byte> expectedOutput, IList<byte> actualOutput)
        {
            int score = 0;
            for (int i = 0; i < expectedOutput.Count; i++)
            {
                if (i < actualOutput.Count)
                {
                    score += (255 - Math.Abs(actualOutput[i] - expectedOutput[i]));
                }
            }
            for (int i = expectedOutput.Count; i < actualOutput.Count; i++)
            {
                score -= 255;
            }
            return score;
        }
    }
}
