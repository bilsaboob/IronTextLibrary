﻿using System;
using System.Collections.Generic;
using System.Linq;
using IronText.Algorithm;
using IronText.Framework;
using IronText.Lib.Shared;

namespace IronText.Lib.IL.Generators
{
    class SparseSwitchGenerator : SwitchGenerator, IDecisionProgramWriter
    {
        private SwitchGeneratorAction action;
        private List<Ref<Labels>>     labels;

        public int                    MaxLinearCount = 3;
        private EmitSyntax emit;
        private Pipe<EmitSyntax> ldvalue;
        private readonly IIntMap<int>  intMap;
        private readonly IntInterval   possibleBounds;
        private readonly IIntFrequency frequency;

        public SparseSwitchGenerator(
            IntArrow<int>[] intArrows,
            int defaultValue,
            IntInterval possibleBounds,
            IIntFrequency frequency = null)
            : this(
                new MutableIntMap<int>(intArrows, defaultValue), 
                possibleBounds,
                frequency)
        {
        }

        public SparseSwitchGenerator(IIntMap<int> intMap, IntInterval possibleBounds)
            : this(intMap, possibleBounds, null)
        {
        }

        public SparseSwitchGenerator(
            IIntMap<int>  intMap,
            IntInterval   possibleBounds,
            IIntFrequency frequency)
        {
            this.intMap = intMap;
            if (frequency == null)
            {
                this.possibleBounds = new IntInterval(int.MinValue, int.MaxValue);
                this.frequency = new UniformIntFrequency(possibleBounds);
            }
            else
            {
                this.possibleBounds = possibleBounds;
                this.frequency = frequency;
            }
        }

        protected override void DoBuild(
            EmitSyntax            emit,
            Pipe<EmitSyntax>      ldvalue,
            SwitchGeneratorAction action)
        {
            this.action = action;

#if false
            var decisionTree = new DecisionTreeBuilder(intMap.DefaultValue) { MaxLinearCount = this.MaxLinearCount };
            var node = decisionTree.BuildBinaryTree(intMap.Enumerate().ToArray());
#else
            var decisionTree = new DecisionTreeBuilder(-1);
            var node = decisionTree.BuildBalanced(
                    intMap,
                    possibleBounds,
                    frequency);
#endif
            this.emit = emit;
            this.ldvalue = ldvalue;
            this.labels = new List<Ref<Labels>>(64);
            node.PrintProgram(this);

            // Debug.Write(node);
        }

        void IDecisionProgramWriter.Action(Decision labelNode, int action)
        {
            emit.Label(GetNodeLabel(labelNode).Def);

            this.action(emit, action);
        }

        void IDecisionProgramWriter.Jump(Decision labelNode, Decision destination)
        {
            emit
                .Label(GetNodeLabel(labelNode).Def)

                .Br(GetNodeLabel(destination));
        }

        void IDecisionProgramWriter.CondJump(Decision labelNode, RelationalOperator op, int operand, Decision destination)
        {
            emit
                .Label(GetNodeLabel(labelNode).Def)
                .Do(ldvalue)
                .Ldc_I4(operand)
                ;

            var label = GetNodeLabel(destination);
            switch (op)
            {
                case RelationalOperator.Equal:          emit.Beq(label);    break;
                case RelationalOperator.NotEqual:       emit.Bne_Un(label); break;
                case RelationalOperator.Less:           emit.Blt(label);    break;
                case RelationalOperator.Greater:        emit.Bgt(label);    break;
                case RelationalOperator.LessOrEqual:    emit.Ble(label);    break;
                case RelationalOperator.GreaterOrEqual: emit.Bge(label);    break;
                default:
                    throw new InvalidOperationException("Not supported operator");
            }
        }

        public void JumpTable(Decision labelNode, int startElement, Decision[] elementToAction)
        {
            emit
                .Label(GetNodeLabel(labelNode).Def)
                .Do(ldvalue)
                .Ldc_I4(startElement)
                .Sub()
                .Switch(elementToAction.Select(GetNodeLabel).ToArray())
                ;
        }

        private Ref<Labels> GetNodeLabel(Decision node)
        {
            if (!node.Label.HasValue)
            {
                node.Label = labels.Count;
                labels.Add(emit.Labels.Generate().GetRef());
            }

            return labels[node.Label.Value];
        }

    }
}
