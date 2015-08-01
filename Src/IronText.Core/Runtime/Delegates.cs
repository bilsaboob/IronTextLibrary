﻿
namespace IronText.Runtime
{
    public delegate int TransitionDelegate(int state, int token);

    public delegate int Scan1Delegate(ScanCursor cursor);

    public delegate object TermFactoryDelegate(object context, int action, string text);

    public class ProductionActionArgs : IDataContext
    {
        private readonly ActionNode[] parts;
        private readonly int firstIndex;
        private readonly int partCount;
        private int      _syntaxArgCount;
        private readonly ActionNode resultNode;

        public ProductionActionArgs(
            int          productionIndex,
            ActionNode[] parts,
            int          firstIndex,
            int          count,
            object       context,
            IStackLookback<ActionNode> lookback,
            ActionNode   resultNode)
        {
            this.ProductionIndex = productionIndex;
            this.parts           = parts;
            this.firstIndex      = firstIndex;
            this.partCount       = count;
            this._syntaxArgCount = parts.Length - firstIndex;
            this.Context         = context;
            this.Lookback        = lookback;
            this.resultNode      = resultNode;
        }

        public int      ProductionIndex { get; private set; }        

        public int      SyntaxArgCount  { get {  return _syntaxArgCount; } }

        public object   Context         { get; private set; }

        public IStackLookback<ActionNode> Lookback { get; private set; }

        public ActionNode GetSyntaxArg(int index) { return parts[firstIndex + index]; }

        public object GetOutcomeProperty(string name)
        {
            return resultNode.GetTokenProperty(name);
        }

        public void SetOutcomeProperty(string name, object value)
        {
            this.resultNode.SetTokenProperty(name, value);
        }

        public object GetInherited(int position, string name)
        {
            ActionNode priorNode;
            if (position == 0)
            {
                priorNode = Lookback.GetNodeAt(1);
            }
            else
            {
                priorNode = parts[firstIndex + position - 1];
            }

            object result = priorNode.GetFollowingStateProperty(name);
            return result;
        }

        public void SetInherited(string name, object value)
        {
            resultNode.SetFollwoingStateProperty(name, value);
        }

        public object GetInputProperty(int position, string name)
        {
            var node = GetSyntaxArg(position);
            var result = node.GetTokenProperty(name);
            return result;
        }
    }

    public delegate object ProductionActionDelegate(ProductionActionArgs args);

    public delegate object MergeDelegate(
        int     token,
        object  oldValue,
        object  newValue,
        object  context,        // user provided context
        IStackLookback<ActionNode> lookback   // access to the prior stack states and values
        );
}
