using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticBrainfuck
{
    public class Testcase
    {
        public IList<byte> Input { get; private set; }

        public IList<byte> Output { get; private set; }

        public Testcase(IList<byte> input, IList<byte> output)
        {
            Input = input;
            Output = output;
        }
    }
}
