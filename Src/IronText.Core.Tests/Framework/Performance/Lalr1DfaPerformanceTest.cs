﻿using System;
using IronText.Automata.Lalr1;
using IronText.Framework;
using IronText.Framework.Reflection;
using NUnit.Framework;

namespace IronText.Tests.Framework.Performance
{
    [TestFixture]
    [Explicit]
    public class Lalr1DfaPerformanceTest
    {
        private EbnfGrammar grammar;

        [Test]
        public void Test()
        {
            const int tokenCount = 50;
            const int ruleSize = 10;

            int[] tokens = new int[tokenCount];
            
            this.grammar = new EbnfGrammar();

            for (int i = 0; i != tokenCount; ++i)
            {
                tokens[i] = grammar.DefineSymbol(i.ToString());
            }

            int iterationCount = tokenCount - ruleSize;
            for (int i = 0; i < iterationCount; ++i)
            {
                int outcome = tokens[i];
                int[] pattern = new int[ruleSize];
                Array.Copy(tokens, i + 1, pattern, 0, ruleSize);
                grammar.DefineProduction(outcome, pattern);
            }

            grammar.Freeze();
            
            var target = new Lalr1Dfa(new BnfGrammarAnalysis(grammar), LrTableOptimizations.Default);
        }
    }
}
