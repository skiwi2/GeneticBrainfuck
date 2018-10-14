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
                };
            }
        }

        static void Main(string[] args)
        {
            int generation = 1;

            var geneticAlgorithm = new GeneticAlgorithm<BrainfuckGen>(
                CreateNewBrainfuckGen, 
                BrainfuckGen.Null,
                ValidateIndividual,
                CalculateFitness
            );
            geneticAlgorithm.InitializePopulation(50, 8);
            var initialGenerationStatistics = geneticAlgorithm.GetGenerationStatistics();
            var initialProgramText = new string(initialGenerationStatistics.BestIndividual.Where(brainfuckGen => brainfuckGen != BrainfuckGen.Null).Select(ToBFChar).ToArray());
            Console.WriteLine($"Generation {generation,5}: {initialGenerationStatistics.AverageFitness,5} average fitness, {initialGenerationStatistics.BestFitness,5} best fitness for {initialProgramText}");

            while (true)
            {
                var generationStatistics = geneticAlgorithm.GetGenerationStatistics();
                var programText = new string(generationStatistics.BestIndividual.Where(brainfuckGen => brainfuckGen != BrainfuckGen.Null).Select(ToBFChar).ToArray());
                Console.WriteLine($"Generation {generation,5}: {generationStatistics.AverageFitness,5} average fitness, {generationStatistics.BestFitness,5} best fitness for {programText}");

                if (IsCorrectProgram(programText))
                {
                    Console.WriteLine($"Found correct program: {programText}");
                }

                geneticAlgorithm.ComputeNextGeneration(0.2d, 0.5d, 0.01d, 0.01d, 0.01d);
                generation++;
            }
        }

        private static bool IsCorrectProgram(string programText)
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token;
            var task = Task.Run(() =>
            {
                try
                {
                    var program = new BFProgram(programText, new BFMemory(100));
                    foreach (var testcase in Testcases)
                    {
                        if (!Enumerable.SequenceEqual(testcase.Output, program.Execute(testcase.Input, cancellationToken)))
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

        private static BrainfuckGen CreateNewBrainfuckGen(BrainfuckGen oldGen, Random random)
        {
            var possibleGenes = ((BrainfuckGen[])Enum.GetValues(typeof(BrainfuckGen))).ToList();
            possibleGenes.Remove(BrainfuckGen.Null);
            possibleGenes.Remove(oldGen);
            return possibleGenes[random.Next(possibleGenes.Count)];
        }

        private static int CalculateFitness(LinkedList<BrainfuckGen> individual) 
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token;
            var task = Task.Run(() => {
                int fitness = 0;
                var programText = new string(individual.Where(brainfuckGen => brainfuckGen != BrainfuckGen.Null).Select(ToBFChar).ToArray());
                try
                {
                    var program = new BFProgram(programText, new BFMemory(100));
                    foreach (var testcase in Testcases)
                    {
                        fitness += CalculateOutputScore(testcase.Output, program.Execute(testcase.Input, cancellationToken));
                    }
                }
                catch (InvalidBFProgramException)
                {
                    // skip
                }
                fitness *= 10;   // ensure that length penalty is not as harsh as getting closer to the solution
                fitness -= programText.Length;
                fitness = (fitness > 0) ? fitness : 0;
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
            var programText = new string(individual.Where(brainfuckGen => brainfuckGen != BrainfuckGen.Null).Select(ToBFChar).ToArray());
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token;
            var task = Task.Run(() => {
                try
                {
                    var program = new BFProgram(programText, new BFMemory(100));
                    program.Execute(Testcases[0].Input, cancellationToken);
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
