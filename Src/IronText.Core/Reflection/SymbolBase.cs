﻿using System;
using System.Diagnostics;
using IronText.Collections;

namespace IronText.Reflection
{
    [DebuggerDisplay("Name = {Name}")]
    public abstract class SymbolBase : IndexableObject<ISharedGrammarEntities>, ICloneable
    {
        /// <summary>
        /// Display name
        /// </summary>
        public string Name { get; protected set; }

        public bool IsPredefined { get { return 0 <= Index && Index < PredefinedTokens.Count; } }

        public abstract SymbolCategory Categories { get; set; }

        /// <summary>
        /// Determines whether symbol is terminal
        /// </summary>
        public virtual bool IsTerminal { get { return false; } }

        public virtual bool IsAmbiguous { get { return false; } }

        public abstract Precedence Precedence { get; set; }

        public abstract ReferenceCollection<Production> Productions { get; }

        public object Clone()
        {
            return DoClone();
        }

        protected abstract SymbolBase DoClone();

        public override string ToString()
        {
            return Name;
        }
    }
}
