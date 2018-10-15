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
        private static IList<Testcase> Testcases
        {
            get
            {
                return new List<Testcase>
                {
                    //new Testcase(new List<byte> { }, Encoding.ASCII.GetBytes("Hello, world!"))
                    new Testcase(new List<byte> { }, Encoding.ASCII.GetBytes("ping"))
                    /*new Testcase(new List<byte> { 0 }, new List<byte> { 0 }),
                    new Testcase(new List<byte> { 32 }, new List<byte> { 64 }),
                    new Testcase(new List<byte> { 64 }, new List<byte> { 128 }),
                    new Testcase(new List<byte> { 96 }, new List<byte> { 192 }),
                    new Testcase(new List<byte> { 128 }, new List<byte> { 0 }),
                    new Testcase(new List<byte> { 160 }, new List<byte> { 64 }),
                    new Testcase(new List<byte> { 192 }, new List<byte> { 128 }),
                    new Testcase(new List<byte> { 224 }, new List<byte> { 192 })*/
                    /*new Testcase(new List<byte> { 97, 194 }, new List<byte> { 35 }),
                    new Testcase(new List<byte> { 142, 44 }, new List<byte> { 186 }),
                    new Testcase(new List<byte> { 123, 250 }, new List<byte> { 117 }),
                    new Testcase(new List<byte> { 124, 23 }, new List<byte> { 147 }),
                    new Testcase(new List<byte> { 116, 91 }, new List<byte> { 207 }),
                    new Testcase(new List<byte> { 112, 165 }, new List<byte> { 21 }),
                    new Testcase(new List<byte> { 214, 203 }, new List<byte> { 161 }),
                    new Testcase(new List<byte> { 23, 130 }, new List<byte> { 153 })*/
                };
            }
        }

        static void Main(string[] args)
        {
            var geneticAlgorithm = new GeneticAlgorithm<BrainfuckGen>(
                CreateNewBrainfuckGen, 
                ValidateIndividual,
                CalculateFitness
            );
            geneticAlgorithm.InitializePopulation(100, 8);
            var initialGenerationStatistics = geneticAlgorithm.GetGenerationStatistics();
            var initialProgramText = new string(initialGenerationStatistics.BestIndividual.Select(ToBFChar).ToArray());
            PrintGeneration(geneticAlgorithm, initialGenerationStatistics, initialProgramText);

            while (true)
            {
                var generationStatistics = geneticAlgorithm.GetGenerationStatistics();
                var programText = new string(generationStatistics.BestIndividual.Select(ToBFChar).ToArray());
                PrintGeneration(geneticAlgorithm, generationStatistics, programText);

                if (IsCorrectProgram(programText))
                {
                    Console.WriteLine($"Found correct program: {programText}");
                }

                geneticAlgorithm.ComputeNextGeneration(0.1d, 0.5d, 0.01d, 0.01d, 0.01d);
            }
        }

        private static void PrintGeneration<T>(GeneticAlgorithm<T> geneticAlgorithm, GenerationStatistics<T> generationStatistics, string programText) where T : struct
        {
            var generation = geneticAlgorithm.Generation;
            var averageFitness = generationStatistics.AverageFitness;
            var bestFitness = generationStatistics.BestFitness;
            Console.WriteLine($"Generation {generation,5}: {averageFitness,5} average fitness, {bestFitness,5} best fitness for {programText}");
        }

        private static bool IsCorrectProgram(string programText)
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)).Token;
            var task = Task.Run(() =>
            {
                try
                {
                    var program = new BFProgram(programText, new BFMemory(100));
                    foreach (var testcase in Testcases)
                    {
                        int instructions = 0;
                        if (!Enumerable.SequenceEqual(testcase.Output, program.Execute(testcase.Input, cancellationToken, ref instructions)))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                catch (InvalidBFProgramException)
                {
                    return false;
                }
            }, cancellationToken);
            return task.Result;
        }

        private static BrainfuckGen CreateNewBrainfuckGen(Random random)
        {
            return (BrainfuckGen)random.Next(8);
        }

        private static int CalculateFitness(LinkedList<BrainfuckGen> individual) 
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)).Token;
            var task = Task.Run(() => {
                int fitness = 0;
                int instructions = 0;
                var programText = new string(individual.Select(ToBFChar).ToArray());
                try
                {
                    var program = new BFProgram(programText, new BFMemory(100));
                    foreach (var testcase in Testcases)
                    {
                        int testcaseInstructions = 0;
                        fitness += CalculateOutputScore(testcase.Output, program.Execute(testcase.Input, cancellationToken, ref testcaseInstructions));
                        // we only want to add if the program didn't run infinitely long
                        instructions += testcaseInstructions;
                    }
                    checked
                    {
                        fitness *= 10;   // ensure that length penalty is not as harsh as getting closer to the solution
                        fitness -= (instructions / 100);
                        fitness -= programText.Length;
                        fitness = (fitness > 0) ? fitness : 0;
                    }
                }
                catch (InvalidBFProgramException)
                {
                    fitness = 0;
                }
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

        private static bool ValidateIndividual(LinkedList<BrainfuckGen> individual)
        {
            var programText = new string(individual.Select(ToBFChar).ToArray());
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)).Token;
            var task = Task.Run(() => {
                try
                {
                    var program = new BFProgram(programText, new BFMemory(100));
                    int instructions = 0;
                    program.Execute(Testcases[0].Input, cancellationToken, ref instructions);
                    return true;
                }
                catch (InvalidBFProgramException)
                {
                    return false;
                }
            }, cancellationToken);
            return task.Result;
        }
    }
}
