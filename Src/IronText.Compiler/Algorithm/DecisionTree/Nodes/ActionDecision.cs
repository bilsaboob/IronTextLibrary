﻿
namespace IronText.Algorithm
{
    public sealed class ActionDecision : Decision
    {
        public ActionDecision(int action)
        {
            this.Action = action;
        }

        public readonly int Action;

        public override int Decide(int value)
        {
            return Action;
        }

        public override void Accept(IDecisionVisitor program)
        {
            program.Visit(this);
        }
    }
}
