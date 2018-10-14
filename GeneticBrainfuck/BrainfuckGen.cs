using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticBrainfuck
{
    public enum BrainfuckGen : byte
    {
        MoveRight = 1,
        MoveLeft = 2,
        Increment = 3,
        Decrement = 4,
        Input = 5,
        Output = 6,
        LoopBegin = 7,
        LoopEnd = 8
    }
}
