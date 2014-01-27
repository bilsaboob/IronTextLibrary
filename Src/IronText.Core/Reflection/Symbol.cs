﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using IronText.Collections;

namespace IronText.Reflection
{
    /// <summary>
    /// Deterministic symbol
    /// </summary>
    public class Symbol : SymbolBase
    {
        private readonly ReferenceCollection<Production> _productions;

        public Symbol(string name)
        {
            this.Name             = name ?? EbnfGrammar.UnnamedTokenName;
            this._productions     = new ReferenceCollection<Production>();
            this.ProvidedContexts = new ReferenceCollection<ProductionContext>();
            this.Joint            = new Joint();
        }

        public bool IsAugmentedStart { get { return EbnfGrammar.AugmentedStart == Index; } }

        public bool IsStart { get { return Context.Start == this; } }

        /// <summary>
        /// Categories token belongs to
        /// </summary>
        public override SymbolCategory Categories { get; set; }

        /// <summary>
        /// Determines whether symbol is terminal
        /// </summary>
        public override bool IsTerminal { get { return Productions.Count == 0; } }

        public override ReferenceCollection<Production> Productions { get { return _productions; } }

        public Joint Joint { get; private set; }

        public ReferenceCollection<ProductionContext> ProvidedContexts { get; private set; }

        /// <summary>
        /// Determines token-level precedence
        /// </summary>
        /// <remarks>
        /// If production has no associated precedence, it is calculated from
        /// the last terminal token in a production pattern.
        /// </remarks>
        public override Precedence Precedence { get; set; }

        public override bool Equals(object obj)
        {
            var casted = obj as Symbol;
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

        protected override SymbolBase DoClone()
        {
            return new Symbol(Name)
            {
                Precedence = Precedence,
                Categories = Categories
            };
        }
    }
}