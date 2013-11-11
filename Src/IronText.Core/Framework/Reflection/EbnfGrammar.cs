﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using IronText.Algorithm;

namespace IronText.Framework.Reflection
{
    internal interface IRuntimeBnfGrammar
    {
        List<Production> Productions { get; }

        bool IsNullable(int token);

        IEnumerable<Production> GetProductions(int leftToken);
    }

    public interface IBuildtimeBnfGrammar
    {
        string SymbolName(int token);

        bool IsTerminal(int token);

        IEnumerable<Production> GetProductions(int leftToken);

        int SymbolCount { get; }

        int AmbSymbolCount { get; }

        Precedence GetTermPrecedence(int token);

        Production AugmentedProduction { get; }

        Precedence GetProductionPrecedence(int ruleId);

        bool IsStartProduction(int ruleId);

        BitSetType TokenSet { get; }

        IEnumerable<AmbiguousSymbol> AmbiguousSymbols { get; }

        bool AddFirst(int[] tokenChain, int startIndex, MutableIntSet output);

        bool HasFirst(int[] tokenChain, int startIndex, int token);

        bool IsTailNullable(int[] tokens, int startIndex);
    }

    public sealed class EbnfGrammar 
        : IRuntimeBnfGrammar
        , IBuildtimeBnfGrammar
    {
        // Predefined tokens
        public const int NoToken               = -1;
        private const int EpsilonToken         = 0;
        public const int PropogatedToken       = 1;
        public const int AugmentedStart        = 2;
        public const int Eoi                   = 3;
        public const int Error                 = 4;
        public const int PredefinedTokenCount  = 5;

        // Special Tokens
        private const int SpecialTokenCount = 2;
        // Token IDs without TokenInfo

        // pending token count
        private int allTokenCount = PredefinedTokenCount;
        private BitSetType tokenSet;
        public readonly List<Symbol> Symbols;
        private readonly List<AmbiguousSymbol> ambSymbols;
        private readonly int AugmentedProductionId;

        private MutableIntSet[] first;

        private bool[] isNullable;
        private bool frozen;

        public EbnfGrammar()
        {   
            Productions = new List<Production>();
            Symbols = new List<Symbol>(PredefinedTokenCount);
            ambSymbols = new List<AmbiguousSymbol>();
            for (int i = PredefinedTokenCount; i != 0; --i)
            {
                Symbols.Add(null);
            }

            Symbols[PropogatedToken] = new Symbol { Name = "#" };
            Symbols[EpsilonToken]    = new Symbol { Name = "$eps" };
            Symbols[AugmentedStart]  = new Symbol { Name = "$start" };
            Symbols[Eoi]             = new Symbol 
                                          { 
                                              Name = "$",
                                              Categories = 
                                                         TokenCategory.DoNotInsert 
                                                         | TokenCategory.DoNotDelete 
                                          };
            Symbols[Error]           = new Symbol { Name = "$error" };

            AugmentedProductionId = DefineRule(AugmentedStart, new[] { -1 });
        }

        public List<Production> Productions { get; private set; }

        public BitSetType TokenSet 
        { 
            get 
            {
                Debug.Assert(frozen);
                return this.tokenSet; 
            } 
        }

        public int MaxRuleSize { get; private set; }

        public Production AugmentedProduction { get { return Productions[AugmentedProductionId];  } }

        public int? StartToken
        {
            get 
            { 
                int result = this.Productions[AugmentedProductionId].Pattern[0];
                return result < 0 ? null : (int?)result;
            }

            set { this.Productions[AugmentedProductionId].Pattern[0] = value.HasValue ? value.Value : -1; }
        }

        public int SymbolCount { get { return Symbols.Count; } }

        public int AmbSymbolCount { get { return ambSymbols.Count; } }

        public IEnumerable<AmbiguousSymbol> AmbiguousSymbols { get { return ambSymbols; } }

        public void Freeze()
        {
            this.frozen = true;
            this.tokenSet = new BitSetType(SymbolCount);

            EnsureFirsts();
            for (int i = PredefinedTokenCount; i != SymbolCount; ++i)
            {
                if (i == Error)
                {
                    Symbols[Error].IsTerm = false;
                }
                else
                {
                    Symbols[i].IsTerm = CalcIsTerm(i);
                }
            }

            Symbols[Eoi].IsTerm = true;

            this.MaxRuleSize = Productions.Select(r => r.Pattern.Length).Max();
        }

        public int DefineToken(string name, TokenCategory categories = TokenCategory.None)
        {
            Debug.Assert(!frozen);

            int result = InternalDefineToken(name, categories);
            if (null == StartToken)
            {
                StartToken = result;
            }

            return result;
        }

        public int DefineAmbToken(int mainToken, IEnumerable<int> tokens)
        {
            int result = allTokenCount++;
            var ambToken = new AmbiguousSymbol(result, mainToken, tokens);
            ambSymbols.Add(ambToken);
            return result;
        }

        public bool IsStartProduction(int ruleId)
        {
            return Productions[ruleId].Outcome == AugmentedProduction.Pattern[0];
        }

        internal bool IsBeacon(int token)
        {
            if (token >= Symbols.Count)
            {
                return false;
            }

            return (Symbols[token].Categories & TokenCategory.Beacon) != 0;
        }

        internal bool IsDontInsert(int token)
        {
            if (token >= Symbols.Count)
            {
                return false;
            }

            return (Symbols[token].Categories & TokenCategory.DoNotInsert) != 0;
        }

        internal bool IsDontDelete(int token)
        {
            if (token >= Symbols.Count)
            {
                return false;
            }

            return (Symbols[token].Categories & TokenCategory.DoNotDelete) != 0;
        }

        private int InternalDefineToken(string name, TokenCategory categories)
        {
            int result = allTokenCount++;
            Symbols.Add(new Symbol { Id = result, Name = name ?? "token-" + result, Categories = categories });
            return result;
        }

        public bool IsTerminal(int token) { return Symbols[token].IsTerm; }

        public bool IsNonTerm(int token) 
        {
            var ti = Symbols[token];
            return !ti.IsTerm && (token >= PredefinedTokenCount);
        }

        internal TokenCategory GetTokenCategories(int token) { return Symbols[token].Categories; }

        internal bool IsExternal(int token) { return (Symbols[token].Categories & TokenCategory.External) != 0; }

        public bool IsNullable(int token) { return isNullable[token]; }

        public bool IsPredefined(int token) { return 0 <= token && token < PredefinedTokenCount; }

        private Production GetRule(int rule) { return this.Productions[rule]; }

        public IEnumerable<Production> GetProductions(int left) { return Symbols[left].Productions; }

        private bool CalcIsTerm(int token)
        {
            bool result = !Productions.Any(rule => rule.Outcome == token);
            return result;
        }

        public string SymbolName(int token) 
        {
            if (token < 0)
            {
                return "<undefined token>";
            }

            return Symbols[token].Name; 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="parts"></param>
        /// <returns>Rule ID or -1 if there is no such rule</returns>
        internal int FindRuleId(int left, int[] parts)
        {
            for (int i = 0; i != Productions.Count; ++i)
            {
                var rule = Productions[i];
                if (rule.Outcome == left
                    && rule.Pattern.Length == parts.Length
                    && Enumerable.SequenceEqual(rule.Pattern, parts))
                {
                    return i;
                }
            }

            return -1;
        }

        public int DefineRule(int left, int[] parts)
        {
            Debug.Assert(!frozen);

            if (IsExternal(left))
            {
                throw new InvalidOperationException(
                    "Unable to define rule for external token. This token should be represented by the external reciver logic.");
            }

            int result = this.Productions.Count;

            var rule = new Production
                {
                    Id = result,
                    Outcome = left,
                    Pattern = parts,
                };

            this.Productions.Add(rule);

            Symbols[left].Productions.Add(rule);
            
            return result;
        }

        // TODO: Optmize
        internal IEnumerable<Production> TokenRules(int token)
        {
            var result = this.Productions.Where(r => r.Outcome == token).ToArray();
            if (result.Length == 0)
            {
                throw new InvalidOperationException("Term token has no rules.");
            }

            return result;
        }

        public IEnumerable<int> EnumerateTokens()
        {
            return Symbols.Select(ti => ti.Id);
        }

        /// <summary>
        /// Firsts set of the token chain
        /// </summary>
        /// <param name="tokenChain"></param>
        /// <param name="output"></param>
        /// <returns><c>true</c> if chain is nullable, <c>false</c> otherwise</returns>
        public bool AddFirst(int[] tokenChain, int startIndex, MutableIntSet output)
        {
            bool result = true;

            while (startIndex != tokenChain.Length)
            {
                int token = tokenChain[startIndex];

                output.AddAll(first[token]);
                if (!isNullable[token])
                {
                    result = false;
                    break;
                }

                ++startIndex;
            }

            return result;
        }

        public bool HasFirst(int[] tokenChain, int startIndex, int token)
        {
            while (startIndex != tokenChain.Length)
            {
                int t = tokenChain[startIndex];

                if (first[t].Contains(token))
                {
                    return true;
                }

                if (!isNullable[t])
                {
                    return false;
                }

                ++startIndex;
            }

            return false;
        }

        public bool IsTailNullable(int[] tokens, int startIndex)
        {
            bool result = true;

            while (startIndex != tokens.Length)
            {
                if (!isNullable[tokens[startIndex]])
                {
                    result = false;
                    break;
                }

                ++startIndex;
            }

            return result;
        }

        internal int FirstNonNullableCount(IEnumerable<int> tokens)
        {
            return TrimRightNullable(tokens).Count();
        }

        internal IEnumerable<int> TrimRightNullable(IEnumerable<int> tokens)
        {
            return tokens.Reverse().SkipWhile(IsNullable).Reverse();
        }

        private void EnsureFirsts()
        {
            if (this.first == null)
            {
                BuildFirstFollowing();
            }
        }

        private void BuildFirstFollowing()
        {
            BuildFirst();
        }

        private void BuildFirst()
        {
            int count = SymbolCount;
            this.first     = new MutableIntSet[count];
            this.isNullable = new bool[count];

            for (int i = 0; i != count; ++i)
            {
                first[i] = tokenSet.Mutable();
                if (CalcIsTerm(i))
                {
                    first[i].Add(i);
                }
            }

            var recursiveRules = new List<Production>();

            // Init FIRST using rules without recursion in first part
            foreach (var rule in Productions)
            {
                if (rule.Pattern.Length == 0)
                {
                    first[rule.Outcome].Add(EpsilonToken);
                }
                else if (CalcIsTerm(rule.Pattern[0]))
                {
                    first[rule.Outcome].Add(rule.Pattern[0]);
                }
                else
                {
                    recursiveRules.Add(rule);
                }
            }

            // Iterate until no more changes possible
            bool changed;
            do
            {
                changed = false;

                foreach (var rule in recursiveRules)
                {
                    if (InternalAddFirsts(rule.Pattern, first[rule.Outcome]))
                    {
                        changed = true;
                    }
                }
            }
            while (changed);

            for (int i = 0; i != count; ++i)
            {
                bool hasEpsilon = first[i].Contains(EpsilonToken);
                if (hasEpsilon)
                {
                    isNullable[i] = hasEpsilon;
                    first[i].Remove(EpsilonToken);
                }
            }
        }

        // Fill FIRST set for the chain of tokens.
        // Returns true if anything was added, false otherwise.
        private bool InternalAddFirsts(IEnumerable<int> chain, MutableIntSet result)
        {
            bool changed = false;

            bool nullable = true;
            foreach (int item in chain)
            {
                bool itemNullable = false;
                foreach (var f in first[item].ToArray())
                {
                    if (f == EpsilonToken)
                    {
                        itemNullable = true; // current part is nullable
                        continue;
                    }

                    if (!result.Contains(f))
                    {
                        result.Add(f);
                        changed = true;
                    }
                }

                if (!itemNullable)
                {
                    nullable = false;
                    break;
                }
            }

            if (nullable && !result.Contains(EpsilonToken))
            {
                result.Add(EpsilonToken);
                changed = true;
            }

            return changed;
        }

        public override bool Equals(object obj)
        {
            var casted = obj as EbnfGrammar;
            return Equals(casted);
        }

        public override string ToString()
        {
            var output = new StringBuilder();
            output
                .Append("Terminals: ")
                .Append(string.Join(" ", EnumerateTokens().Where(IsTerminal).Select(SymbolName)))
                .AppendLine()
                .Append("Non-Terminals: ")
                .Append(string.Join(" ", EnumerateTokens().Where(t => !IsTerminal(t)).Select(SymbolName)))
                .AppendLine()
                .AppendFormat("Start Token: {0}", StartToken == null ? "<undefined>" : SymbolName(StartToken.Value))
                .AppendLine()
                .Append("Rules:")
                .AppendLine();
            foreach (var rule in Productions)
            {
                output.AppendFormat("{0:D2}: {1} -> {2}", rule.Id, SymbolName(rule.Outcome), string.Join(" ", rule.Pattern.Select(SymbolName))).AppendLine();
            }

            return output.ToString();
        }


        public Precedence GetTermPrecedence(int token)
        {
            if (!IsTerminal(token))
            {
                throw new ArgumentException("Precedence is applicable only to terminals.", "token");
            }

            var info = this.Symbols[token];
            return info.Precedence;
        }

        public void SetTermPrecedence(int token, Precedence precedence)
        {
            if (!CalcIsTerm(token))
            {
                throw new ArgumentException("Precedence is applicable only to terminals.", "token");
            }

            var info = this.Symbols[token];
            info.Precedence = precedence;
        }

        public Precedence GetProductionPrecedence(int ruleId)
        {
            var rule = GetRule(ruleId);
            if (rule.Precedence != null)
            {
                return rule.Precedence;
            }

            int index = Array.FindLastIndex(rule.Pattern, CalcIsTerm);
            return index < 0 ? null : GetTermPrecedence(rule.Pattern[index]);
        }

        public void SetRulePrecedence(int ruleId, Precedence value)
        {
            var rule = GetRule(ruleId);
            rule.Precedence = value;
        }
    }
}