using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticBrainfuck
{
    public enum BrainfuckGen : byte
    {
        MoveRight = 0,
        MoveLeft = 1,
        Increment = 2,
        Decrement = 3,
        Input = 4,
        Output = 5,
        LoopBegin = 6,
        LoopEnd = 7
    }
}
