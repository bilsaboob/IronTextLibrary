﻿using IronText.Collections;
using IronText.Reflection;
using IronText.Reporting;
using IronText.Runtime;
using System.IO;
using System.Linq;
using System.Text;

namespace IronText.Reports
{
    public class ParserStateMachineReport : IReport
    {
        const string Indent = "  ";
        private readonly string fileName;

        public ParserStateMachineReport(string fileName)
        {
            this.fileName = fileName;
        }
        public void Build(IReportData data)
        {
            string path = Path.Combine(data.DestinationDirectory, fileName);

            var conflicts = data.ParserAutomata.GetConflicts().ToArray();

            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                if (conflicts.Length != 0)
                {
                    writer.WriteLine("Found {0} conflicts", conflicts.Length);

                    foreach (var conflict in conflicts)
                    {
                        ReportConflict(data, conflict, writer);
                    }
                }

                PrintTransitions(data, writer);

                PrintSemanticBindings(data, writer);
            }
        }

        private void PrintTransitions(IReportData data, StreamWriter output)
        {
            string title = "Language: " + data.Source.FullLanguageName;

            output.WriteLine(title);
            output.WriteLine();
            output.WriteLine("Grammar:");
            output.Write(data.Grammar);
            output.WriteLine();

            foreach (var state in data.ParserAutomata.States)
            {
                output.Write("State ");
                output.Write(state.Index);
                output.WriteLine();
                output.WriteLine();
                DescribeState(data, state, output, Indent);
                output.WriteLine();

                foreach (var transition in state.Transitions)
                {
                    var decision = transition.AlternateDecisions.ToArray();

                    if (decision.Length == 0)
                    {
                        continue;
                    }

                    output.Write(Indent);
                    output.Write(transition.Symbol);
                    output.Write("             ");

                    if (decision.Length > 1)
                    {
                        output.Write(Indent);
                        output.WriteLine("conflict {");
                        foreach (var alternative in decision)
                        {
                            PrintDecision(output, alternative);
                        }

                        output.Write(Indent);
                        output.WriteLine("}");
                        output.WriteLine();
                    }
                    else
                    {
                        PrintDecision(output, decision[0]);
                    }
                }

                output.WriteLine();
            }
        }

        private void PrintDecision(
            StreamWriter    output,
            IParserDecision decision)
        {
            output.Write(Indent);
            output.Write(Indent);
            output.Write(decision.ActionText);
            if (decision.NextState != null)
            {
                output.Write(" -> state-");
                output.Write(decision.NextState.Index);
            }
            output.WriteLine();
        }

        private void ReportConflict(IReportData data, ParserConflict conflict, StreamWriter message)
        {
            var symbol = conflict.Transition.Symbol;

            const string Indent = "  ";

            message.WriteLine(new string('-', 50));
            message.Write("Conflict on token ");
            message.Write(symbol);
            message.Write(" between actions in state #");
            message.Write(conflict.State.Index + "");
            message.WriteLine(":");
            DescribeState(data, conflict.State, message, Indent).WriteLine();
            int i = 0;
            foreach (var decision in conflict.Transition.AlternateDecisions)
            {
                message.WriteLine("Action #{0}", i);
                DescribeAction(data, decision, message, Indent);

                ++i;
            }

            message.WriteLine(new string('-', 50));
        }

        private StreamWriter DescribeAction(
            IReportData data,
            IParserDecision decision,
            StreamWriter output,
            string indent)
        {
            output.Write(indent);
            output.Write(decision.ActionText);
            output.WriteLine(":");
            DescribeState(data, decision.NextState, output, indent + indent);

            return output;
        }

        private static StreamWriter DescribeState(
            IReportData data,
            IParserState state,
            StreamWriter output,
            string indent)
        {
            foreach (var item in state.DotItems)
            {
                output.Write(indent);
                DescribeItem(data, item, output);
                output.WriteLine();
            }

            return output;
        }

        private static StreamWriter DescribeItem(
            IReportData     data,
            IParserDotItem  item,
            StreamWriter    output,
            bool showLookaheads = true)
        {
            output.Write(item.Outcome);
            output.Write(" ->");
            int i = 0;
            foreach (var symbol in item.Input)
            {
                if (item.Position == i)
                {
                    output.Write(" •");
                }

                output.Write(" ");
                output.Write(symbol);

                ++i;
            }

            if (item.Position == item.InputLength)
            {
                output.Write(" •");
            }

            if (showLookaheads)
            {
                output.Write("  |LA = {");
                output.Write(string.Join(", ", (from la in item.LA select data.Grammar.Symbols[la].Name)));
                output.Write("}");
            }

            return output;
        }

        private static StreamWriter DescribeRule(IReportData data, int ruleId, StreamWriter output)
        {
            var rule = data.Grammar.Productions[ruleId];

            output.Write(rule.Outcome.Name);
            output.Write(" ->");

            foreach (var symbol in rule.Input)
            {
                output.Write(" ");
                output.Write(symbol.Name);
            }

            return output;
        }

        private void PrintSemanticBindings(IReportData data, StreamWriter writer)
        {
            writer.WriteLine();
            writer.WriteLine("Semantic Bindings:");
            writer.WriteLine();

            int i = 0;
            foreach (var semanticBinding in data.SemanticBindings)
            {
                writer.WriteLine("{0}: ", ++i);
                    
                writer.WriteLine(
                    "    {{ {0} }}",
                    semanticBinding.ProvidingProductionText);

                writer.WriteLine(
                    "    =({0})=>",
                    GetSemanticName(semanticBinding.ReferenceName));

                writer.WriteLine(
                    "    {{ {0} }}",
                    semanticBinding.ConsumingProductionText);
            }
        }

        private string GetSemanticName(string name)
        {
            const int MaxLength = 20;

            if (name.Length < MaxLength)
            {
                return name;
            }

            var commaIndex = name.IndexOf(',');
            if (commaIndex > 0)
            {
                return name.Substring(0, commaIndex);
            }

            return name.Substring(0, MaxLength) + "...";
        }
    }
}
