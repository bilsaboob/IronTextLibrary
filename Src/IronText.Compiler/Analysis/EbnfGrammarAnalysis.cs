﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronText.Framework;
using IronText.Algorithm;
using IronText.Framework.Reflection;

namespace IronText.Compiler.Analysis
{
    /// <summary>
    /// Prebuilds various tables related to <see cref="IronText.Framework.BnfGrammar"/>
    /// </summary>
    sealed class EbnfGrammarAnalysis
    {
        private readonly EbnfGrammar grammar;
        private readonly IBuildtimeNullableFirstTables tables;

        public EbnfGrammarAnalysis(EbnfGrammar grammar)
        {
            this.grammar = grammar;
            this.tables = new NullableFirstTables(grammar);
        }

        /// <summary>
        /// Fewer values are less dependent to higher values 
        /// Relation of values is non-determined for two mutally 
        /// dependent non-terms.
        /// </summary>
        public int[] GetTokenComplexity()
        {
            var result = Enumerable.Repeat(-1, grammar.Symbols.Count).ToArray();
            var sortedTokens = Graph.TopologicalSort(
                                new [] { EbnfGrammar.AugmentedStart },
                                GetDependantTokens)
                                .ToArray();
            for (int i = 0; i != sortedTokens.Length; ++i)
            {
                result[sortedTokens[i]] = i;
            }

            return result;
        }

        private IEnumerable<int> GetDependantTokens(int token)
        {
            foreach (var rule in grammar.Symbols[token].Productions)
            {
                foreach (int part in rule.PatternTokens)
                {
                    yield return part;
                }
            }
        }

        public string SymbolName(int token)
        {
            return grammar.Symbols[token].Name;
        }

        public bool IsTerminal(int token)
        {
            return grammar.Symbols[token].IsTerminal;
        }

        public IEnumerable<Production> GetProductions(int leftToken)
        {
            return grammar.Symbols[leftToken].Productions;
        }

        public SymbolCollection Symbols
        {
            get { return grammar.Symbols; }
        }

        public Precedence GetTermPrecedence(int token)
        {
            return grammar.Symbols[token].Precedence;
        }

        public Production AugmentedProduction
        {
            get { return grammar.AugmentedProduction; }
        }

        public Precedence GetProductionPrecedence(int prodId)
        {
            return grammar.Productions[prodId].EffectivePrecedence;
        }

        public bool IsStartProduction(int prodId)
        {
            return grammar.Productions[prodId].IsStart;
        }

        public BitSetType TokenSet
        {
            get { return tables.TokenSet; }
        }

        public IEnumerable<AmbiguousSymbol> AmbiguousSymbols
        {
            get { return grammar.Symbols.OfType<AmbiguousSymbol>(); }
        }

        public void AddFirst(DotItem item, MutableIntSet output)
        {
            bool isNullable = tables.AddFirst(item.GetPattern(), item.Position, output);

            if (isNullable)
            {
                output.AddAll(item.LA);
            }
        }

        public bool HasFirst(DotItem item, int token)
        {
            return tables.HasFirst(item.GetPattern(), item.Position, token);
        }

        public bool IsTailNullable(DotItem item)
        {
            return tables.IsTailNullable(item.GetPattern(), item.Position);
        }
    }
}
