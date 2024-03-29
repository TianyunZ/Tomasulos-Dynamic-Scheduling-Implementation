﻿using System.Collections.Generic;
using System.Linq;

namespace Tomasulo
{
    public class InstructionUnit
    {
        Queue<Instruction> instructions = new Queue<Instruction>();

        public void AddInstruction(Instruction instruct)
        {
            instructions.Enqueue(instruct);
        }

        public void ClearInstructions()
        {
            instructions = new Queue<Instruction>();
        }

        public Instruction GetInstruction()
        {
            return instructions.Dequeue();
        }

        public Instruction PeekAtInstruction()
        {
            return instructions.Peek();
        }

        public Instruction[] GetCurrentInstructions()
        {
            return instructions.ToArray<Instruction>();//将queue转化为数组
        }
    }
}