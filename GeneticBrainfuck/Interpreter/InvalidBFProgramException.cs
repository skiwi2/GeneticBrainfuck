using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticBrainfuck.Interpreter
{
    public class InvalidBFProgramException : Exception
    {
        public bool Recoverable { get; private set; }

        public InvalidBFProgramException(string message, bool recoverable) : base(message)
        {
            Recoverable = recoverable;
        }
    }
}
