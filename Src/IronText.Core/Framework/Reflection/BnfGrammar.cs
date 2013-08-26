﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using IronText.Algorithm;

namespace IronText.Framework
{
    [Serializable]
    public sealed partial class BnfGrammar
    {
        // Special Tokens
        private const int SpecialTokenCount = 2;
        // Token IDs without TokenInfo

        private BitSetType tokenSet;
        private readonly List<TokenInfo> tokenInfos;
        public readonly List<BnfRule>      Rules      = new List<BnfRule>();
        private readonly int InternalStartRuleId;

        // Augumented start token $start and EOI term '$'
        public const int NoToken               = -1;
        private const int EpsilonToken         = 0;
        public const int PropogatedToken       = 1;
        public const int AugmentedStart        = 2;
        public const int Eoi                   = 3;
        public const int Error                 = 4;
        public const int PredefinedTokenCount  = 5;

        [NonSerialized]
        private MutableIntSet[] first;

        [NonSerialized]
        private bool[] isNullable;
        private bool frozen;

        public BnfGrammar()
        {
            tokenInfos = new List<TokenInfo>(PredefinedTokenCount);
            for (int i = PredefinedTokenCount; i != 0; --i)
            {
                tokenInfos.Add(null);
            }

            tokenInfos[PropogatedToken] = new TokenInfo { Name = "#" };
            tokenInfos[EpsilonToken]    = new TokenInfo { Name = "$eps" };
            tokenInfos[AugmentedStart]  = new TokenInfo { Name = "$start" };
            tokenInfos[Eoi]             = new TokenInfo 
                                          { 
                                              Name = "$",
                                              Categories = 
                                                         TokenCategory.DoNotInsert 
                                                         | TokenCategory.DoNotDelete 
                                          };
            tokenInfos[Error]           = new TokenInfo { Name = "$error" };

            InternalStartRuleId = DefineRule(AugmentedStart, new[] { -1 });
        }
        
        public BitSetType TokenSet 
        { 
            get 
            {
                Debug.Assert(frozen);
                return this.tokenSet; 
            } 
        }

        public int MaxRuleSize { get; private set; }

        public BnfRule AugmentedRule { get { return Rules[InternalStartRuleId];  } }

        public int? StartToken
        {
            get 
            { 
                int result = this.Rules[InternalStartRuleId].Parts[0];
                return result < 0 ? null : (int?)result;
            }

            set { this.Rules[InternalStartRuleId].Parts[0] = value.HasValue ? value.Value : -1; }
        }

        public int TokenCount { get { return tokenInfos.Count; } }

        public void Freeze()
        {
            this.frozen = true;
            this.tokenSet = new BitSetType(TokenCount);

            EnsureFirsts();
            for (int i = 0; i != TokenCount; ++i)
            {
                if (i == Error)
                {
                    tokenInfos[Error].IsTerm = false;
                }
                else
                {
                    tokenInfos[i].IsTerm = CalcIsTerm(i);
                }
            }

            this.MaxRuleSize = Rules.Select(r => r.Parts.Length).Max();
        }

        private IEnumerable<int> GetDependantTokens(int token)
        {
            foreach (var rule in GetProductionRules(token))
            {
                foreach (int part in rule.Parts)
                {
                    yield return part;
                }
            }
        }

        /// <summary>
        /// Fewer values are less dependent to higher values 
        /// Relation of values is non-determined for two mutally 
        /// dependent non-terms.
        /// </summary>
        public int[] GetTokenComplexity()
        {
            var result = Enumerable.Repeat(-1, TokenCount).ToArray();
            var sortedTokens = Graph.ToplogicalSort(new [] { AugmentedStart }, GetDependantTokens).ToArray();
            for (int i = 0; i != sortedTokens.Length; ++i)
            {
                result[sortedTokens[i]] = i;
            }

            return result;
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

        public bool IsBeacon(int token)
        {
            return (tokenInfos[token].Categories & TokenCategory.Beacon) != 0;
        }

        public bool IsDontInsert(int token)
        {
            return (tokenInfos[token].Categories & TokenCategory.DoNotInsert) != 0;
        }

        public bool IsDontDelete(int token)
        {
            return (tokenInfos[token].Categories & TokenCategory.DoNotDelete) != 0;
        }

        private int InternalDefineToken(string name, TokenCategory categories)
        {
            int result = tokenInfos.Count;
            tokenInfos.Add(new TokenInfo { Name = name ?? "token-" + result, Categories = categories });
            return result;
        }

        public bool IsTerm(int token) { return tokenInfos[token].IsTerm; }

        public TokenCategory GetTokenCategories(int token) { return tokenInfos[token].Categories; }

        public bool IsExternal(int token) { return (tokenInfos[token].Categories & TokenCategory.External) != 0; }

        public bool IsNullable(int token) { return isNullable[token]; }

        public bool IsPredefined(int token) { return 0 <= token && token < PredefinedTokenCount; }

        private BnfRule GetRule(int rule) { return this.Rules[rule]; }

        public IEnumerable<BnfRule> GetProductionRules(int left) { return tokenInfos[left].Productions; }

        private bool CalcIsTerm(int token)
        {
            bool result = !Rules.Any(rule => rule.Left == token);
            return result;
        }

        public string TokenName(int token) 
        {
            if (token < 0)
            {
                return "<undefined token>";
            }

            return tokenInfos[token].Name; 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="parts"></param>
        /// <returns>Rule ID or -1 if there is no such rule</returns>
        public int FindRuleId(int left, int[] parts)
        {
            for (int i = 0; i != Rules.Count; ++i)
            {
                var rule = Rules[i];
                if (rule.Left == left
                    && rule.Parts.Length == parts.Length
                    && Enumerable.SequenceEqual(rule.Parts, parts))
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

            int result = this.Rules.Count;

            var rule = new BnfRule
                {
                    Id = result,
                    Left = left,
                    Parts = parts,
                };

            this.Rules.Add(rule);

            tokenInfos[left].Productions.Add(rule);
            
            return result;
        }

        // TODO: Optmize
        public IEnumerable<BnfRule> TokenRules(int token)
        {
            var result = this.Rules.Where(r => r.Left == token).ToArray();
            if (result.Length == 0)
            {
                throw new InvalidOperationException("Term token has no rules.");
            }

            return result;
        }

        public IEnumerable<int> EnumerateTokens()
        {
            return Enumerable.Range(0, tokenInfos.Count);
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

        public int FirstNonNullableCount(IEnumerable<int> tokens)
        {
            return TrimRightNullable(tokens).Count();
        }

        public IEnumerable<int> TrimRightNullable(IEnumerable<int> tokens)
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
            int count = TokenCount;
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

            var recursiveRules = new List<BnfRule>();

            // Init FIRST using rules without recursion in first part
            foreach (var rule in Rules)
            {
                if (rule.Parts.Length == 0)
                {
                    first[rule.Left].Add(EpsilonToken);
                }
                else if (CalcIsTerm(rule.Parts[0]))
                {
                    first[rule.Left].Add(rule.Parts[0]);
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
                    if (InternalAddFirsts(rule.Parts, first[rule.Left]))
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
            var casted = obj as BnfGrammar;
            return Equals(casted);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Rules.Sum(rule => rule.GetHashCode())
                    + tokenInfos.Sum(t => t.GetHashCode());
            }
        }

        public bool Equals(BnfGrammar other)
        {
            return other != null
                && Enumerable.SequenceEqual(other.Rules, this.Rules)
                && Enumerable.SequenceEqual(other.tokenInfos, this.tokenInfos);
        }

        public override string ToString()
        {
            var output = new StringBuilder();
            output
                .Append("Terminals: ")
                .Append(string.Join(" ", EnumerateTokens().Where(IsTerm).Select(TokenName)))
                .AppendLine()
                .Append("Non-Terminals: ")
                .Append(string.Join(" ", EnumerateTokens().Where(t => !IsTerm(t)).Select(TokenName)))
                .AppendLine()
                .AppendFormat("Start Token: {0}", StartToken == null ? "<undefined>" : TokenName(StartToken.Value))
                .AppendLine()
                .Append("Rules:")
                .AppendLine();
            foreach (var rule in Rules)
            {
                output.AppendFormat("{0:D2}: {1} -> {2}", rule.Id, TokenName(rule.Left), string.Join(" ", rule.Parts.Select(TokenName))).AppendLine();
            }

            return output.ToString();
        }


        class TokenInfo
        {
            public string Name;       // Display name
            public TokenCategory Categories;
            public bool IsTerm;
            public Precedence Precedence;
            public readonly List<BnfRule> Productions = new List<BnfRule>();

            public override bool Equals(object obj)
            {
                var casted = obj as TokenInfo;
                return casted != null
                    && casted.Name == Name
                    && casted.Categories == Categories
                    && object.Equals(casted.Precedence, Precedence)
                    ;
            }

            public override int GetHashCode()
            {
                int result = 0;
                unchecked
                {
                    if (Name != null)
                    {
                        result += Name.GetHashCode();
                    }

                    result += Categories.GetHashCode();
                    if (Precedence != null)
                    {
                        result += Precedence.GetHashCode();
                    }
                }

                return result;
            }
        }

        public Precedence GetTermPrecedence(int token)
        {
            if (!IsTerm(token))
            {
                throw new ArgumentException("Precedence is applicable only to terminals.", "token");
            }

            var info = this.tokenInfos[token];
            return info.Precedence;
        }

        public void SetTermPrecedence(int token, Precedence precedence)
        {
            if (!CalcIsTerm(token))
            {
                throw new ArgumentException("Precedence is applicable only to terminals.", "token");
            }

            var info = this.tokenInfos[token];
            info.Precedence = precedence;
        }

        public Precedence GetRulePrecedence(int ruleId)
        {
            var rule = GetRule(ruleId);
            if (rule.Precedence != null)
            {
                return rule.Precedence;
            }

            int index = Array.FindLastIndex(rule.Parts, CalcIsTerm);
            return index < 0 ? null : GetTermPrecedence(rule.Parts[index]);
        }

        public void SetRulePrecedence(int ruleId, Precedence value)
        {
            var rule = GetRule(ruleId);
            rule.Precedence = value;
        }
    }
}
