﻿using System;
using System.Collections.Generic;
using System.Linq;
using IronText.Algorithm;
using IronText.Compiler.Analysis;

namespace IronText.Automata.Lalr1
{
    public sealed class DotState
    {
        public readonly IDotItemSet Items;
        public readonly List<DotTransition> Transitions = new List<DotTransition>();

        private MutableDotItemSet cachedKernel;
        private int index;

        public DotState(int index)
        {
            this.index = index;
            this.Items = new MutableDotItemSet();
        }

        public DotState(int index, IEnumerable<DotItem> dotItems)
        {
            this.index = index;
            this.Items = new MutableDotItemSet(dotItems);
        }

        public int Index { get { return this.index; } }

        public bool IsReduceState
        {
            get
            {
                return Transitions.Count == 0
                    && Items.Count == 1
                    && Items[0].IsReduce
                    && !Items[0].IsAugmented;
            }
        }

        public void Reindex(int newIndex)
        {
            this.index = newIndex;
        }

        public MutableDotItemSet KernelItems
        {
            get
            {
                if (cachedKernel == null)
                {
                    cachedKernel =
                        new MutableDotItemSet(
                            from item in Items where item.IsKernel select item);
                }

                return cachedKernel;
            }
        }

        public int GetStateToken()
        {
            foreach (var item in Items)
            {
                if (item.Position != 0)
                {
                    return item[item.Position - 1];
                }
            }

            return -1;
        }

        public int GetNextIndex(int token)
        {
            var next = GetNext(token);
            return next == null ? -1 : next.Index;
        }

        public DotState GetNext(int token)
        {
            foreach (var t in Transitions)
            {
                if (t.Tokens.Contains(token))
                {
                    return t.To;
                }
            }

            return null;
        }

        public bool AddTransition(int token, DotState to, IntSetType tokenSetType)
        {
            bool result;

            var existing = Transitions.FirstOrDefault(t => t.To == to);
            if (existing == null)
            {
                existing = new DotTransition(tokenSetType.Mutable(), to);
                Transitions.Add(existing);
                result = true;
            }
            else
            {
                result = !existing.Tokens.Contains(token);
            }

            existing.Tokens.Add(token);

            return result;
        }

        public DotItem GetItem(int prodId, int dotPos)
        {
            foreach (var item in Items)
            {
                if (item.ProductionId == prodId && item.Position == dotPos)
                {
                    return item;
                }
            }

            throw new InvalidOperationException("Internal error: dotitem not found");
        }
    }
}
