using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticBrainfuck.Interpreter.Nodes
{
    public class LoopNode : INode
    {
        public ICollection<INode> Children { get; private set; }

        public LoopNode(ICollection<INode> children)
        {
            Children = children;
        }
    }
}
