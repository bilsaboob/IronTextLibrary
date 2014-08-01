﻿using IronText.Collections;
using IronText.Reflection.Reporting;
using IronText.Reflection.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace IronText.Reflection
{
    /// <summary>
    /// TODO:
    /// - Predefined entities should not be deletable.
    /// - Organize API to avoid IsPredefined checks.
    /// </summary>
    [Serializable]
    public sealed class Grammar : IGrammarScope
    {
        public const string UnnamedTokenName = "<unnamed token>";
        public const string UnknownTokenName = "<unknown token>";

        [NonSerialized]
        private readonly Joint _joint = new Joint();

        [NonSerialized]
        private readonly ReportCollection _reports = new ReportCollection();

        public Grammar()
        {
            Options     = RuntimeOptions.Default;

            Productions      = new ProductionCollection(this);
            Symbols          = new SymbolCollection(this);
            Matchers         = new MatcherCollection(this);
            Mergers          = new MergerCollection(this);
            Globals          = new SemanticScope();

            // TODO: Valid indexes for predefined symbols
#if false
            Symbols[PredefinedTokens.Propagated]      = new Symbol("#");
            Symbols[PredefinedTokens.Epsilon]         = new Symbol("$eps");
            Symbols[PredefinedTokens.AugmentedStart]  = new Symbol("$start");
            Symbols[PredefinedTokens.Eoi]             = new Symbol("$")
                                          { 
                                              Categories = SymbolCategory.DoNotInsert 
                                                         | SymbolCategory.DoNotDelete 
                                          };
            Symbols[PredefinedTokens.Error]           = new Symbol("$error");
#endif
            this.Epsilon = Symbols.Add("$eps");
            this.Propagated = Symbols.Add("#");
            this.AugmentedStart = Symbols.Add("$start");
            this.Eoi = new Symbol("$") {
                    Categories = SymbolCategory.DoNotInsert
                               | SymbolCategory.DoNotDelete
                };
            Symbols.Add(Eoi);

            this.Error = Symbols.Add("$error");

            var startStub = new Symbol("$start-stub");
            AugmentedProduction = Productions.Add(this.AugmentedStart, new Symbol[] { startStub }, null);
        }

        public RuntimeOptions Options { get; set; }

        public Joint Joint { get { return _joint; } }

        public string StartName
        {
            get {  return Start == null ? null : Start.Name; }
            set {  Start = value == null ? null : Symbols.ByName(value, createMissing: true); }
        }

        internal Symbol AugmentedStart { get; set; }

        private Symbol Epsilon         { get; set; }

        private Symbol Propagated      {  get; set; }

        private Symbol Eoi             {  get; set; }

        internal Symbol Error          {  get; private set; }

        public Symbol Start
        {
            get { return AugmentedProduction.Pattern[0]; }
            set { AugmentedProduction.SetAt(0, value); }
        }

        internal Production         AugmentedProduction { get; private set; }

        public SemanticScope        Globals             { get; private set; }

        public SymbolCollection     Symbols             { get; private set; }

        public ProductionCollection Productions         { get; private set; }

        public MatcherCollection    Matchers            { get; private set; }

        public MergerCollection     Mergers             { get; private set; }

        public ReportCollection     Reports             { get { return _reports; } }

        public void BuildIndexes()
        {
            Symbols.BuildIndexes();
            Productions.BuildIndexes();
            Matchers.BuildIndexes();
            Mergers.BuildIndexes();
        }

        public override string ToString()
        {
            var writerType = Type.GetType("IronText.Reflection.DefaultTextGrammarWriter, IronText.Compiler");
            if (writerType == null)
            {
                return "IronText.Reflection.Grammar";
            }

            var writer = (IGrammarTextWriter)Activator.CreateInstance(writerType);

            using (var output = new StringWriter())
            {
                writer.Write(this, output);
                return output.ToString();
            }
        }

        public void Inline()
        {
            ConvertNullableNonOptToOpt();
            RecursivelyEliminateEmptyProductions();

            var symbolsToInline = (from symbol in Symbols
                                   let asNonAmb = symbol as Symbol
                                   where CanInline(asNonAmb)
                                   select asNonAmb)
                                  .ToArray();

            foreach (var symbol in symbolsToInline)
            {
                Inline(symbol);
            }
        }

        public void RecursivelyEliminateEmptyProductions()
        {
            while (EliminateEmptyProductions())
            {
            }
        }

        public bool EliminateEmptyProductions()
        {
            var old = Productions.DuplicateResolver;
            Productions.DuplicateResolver = ProductionDuplicateResolver.Instance;
            try
            {
                return InternalEliminateEmptyProductions();
            }
            finally
            {
                Productions.DuplicateResolver = old;
            }
        }

        private bool InternalEliminateEmptyProductions()
        {
            bool result = false;

            var nullableSymbols = FindNullableSymbols().ToArray();
            foreach (var symbol in nullableSymbols)
            {
                result = result || Inline(symbol);
            }

            return result;
        }

        private IEnumerable<Symbol> FindNullableSymbols()
        {
            var result = Symbols
                    .OfType<Symbol>()
                    .Where(s => s.Productions.Count != 0
                             && s.Productions.Any(p => p.Pattern.Length == 0)
                             && !s.IsRecursive);

            return result;
        }

        private static bool CanInline(Symbol symbol)
        {
            if (symbol == null 
                || symbol.IsPredefined 
                || symbol.IsStart
                || symbol.HasSideEffects)
            {
                return false;
            }

            var result =
                symbol.Productions.Count == 1 
                && (symbol.Productions.All(p => p.Size <= 1)
                    || 
                    symbol.Productions.All(
                        p => p.Pattern.All(s => s.IsTerminal)));

            if (result)
            {
                result = !symbol.IsRecursive;
            }

            return result;
        }

        public bool Inline(Symbol symbol)
        {
            bool result = false;

            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }

            if (symbol.IsTerminal)
            {
                throw new ArgumentException("Cannot inline terminal symbol.", "symbol");
            }

            var productionsToExtend = GetProductionsHavingInput(symbol).ToList();

            for (int i = 0; i != productionsToExtend.Count; ++i)
            {
                var prod = productionsToExtend[i];

                int pos = Array.IndexOf(prod.Pattern, symbol);
                if (pos >= 0)
                {
                    productionsToExtend.AddRange(Extend(prod, pos));
                    result = true;
                }
            }

            return result;
        }

        private IEnumerable<Production> GetProductionsHavingInput(Symbol symbol)
        {
            foreach (var prod in Productions)
            {
                if (prod.Pattern.Contains(symbol) && !prod.IsHidden)// && prod.IsUsed)
                {
                    yield return prod;
                }
            }
        }

        public IEnumerable<Production> Extend(Production source, int position)
        {
            var result = new List<Production>();

            var symbol = source.Pattern[position];

            source.Hide();

            var producitonsToInline = symbol.Productions.ToArray();
            foreach (var inlinedProd in producitonsToInline)
            {
                var extended = new ProductionInliner(inlinedProd).Execute(source, position);
                Productions.Add(extended);
                result.Add(extended);

                if (!inlinedProd.IsUsed)
                {
                    inlinedProd.Hide();
                }
            }

            return result;
        }

        public Symbol Decompose(Symbol nonTerm, Func<Production,bool> criteria, string newSymbolName)
        {
            Symbol newSymbol = Symbols.Add(newSymbolName);
            newSymbol.Joint.AddAll(nonTerm.Joint);

            foreach (var prod in nonTerm.Productions.ToArray())
            {
                if (criteria(prod))
                {
                    var newProd = Productions.Add(
                        new Production(
                            newSymbol,
                            prod.Components,
                            contextRef: prod.ContextRef,
                            flags: prod.Flags));
                    newProd.ExplicitPrecedence = prod.ExplicitPrecedence;

                    newProd.Joint.AddAll(prod.Joint);

                    prod.Hide();
                }
            }

            Productions.Add(new Production(nonTerm, newSymbol));

            return newSymbol;
        }

        public Symbol[] FindOptionalPatternSymbols()
        {
            return Symbols.OfType<Symbol>().Where(IsOptionalSymbol).ToArray();
        }

        public bool InlineOptionalSymbols()
        {
            bool result = false;

            var symbolsToInline = FindOptionalPatternSymbols();
            foreach (var symbol in symbolsToInline)
            {
                Inline(symbol);
                result = true;
            }

            return result;
        }

        private static bool IsOptionalSymbol(Symbol symbol)
        {
            if (symbol == null)
            {
                return false;
            }

            var prods = symbol.Productions;
            bool result = prods.Count == 2
                        && prods.Any(p => p.Pattern.Length == 0)
                        && prods.Any(p => p.Pattern.Length == 1);
            return result;
        }

        public void ConvertNullableNonOptToOpt()
        {
            var nullableSymbols = FindNullableNonOptSymbols();
            foreach (var symbol in nullableSymbols)
            {
                Decompose(symbol, prod => !prod.Pattern.All(nullableSymbols.Contains), symbol.Name + "_d$");
            }
        }

        public IEnumerable<Symbol> FindNullableNonOptSymbols()
        {
            var result = Symbols
                   .OfType<Symbol>()
                   .Where(s => 
                       s.Productions.Any(p => p.Pattern.Length == 0)
                       && s.Productions.Any(p => p.Pattern.Length != 0)
                       && (s.Productions.Count != 2 
                          || s.Productions.Any(p => p.Pattern.Length > 1)));
            return result.ToArray();
        }
    }
}
