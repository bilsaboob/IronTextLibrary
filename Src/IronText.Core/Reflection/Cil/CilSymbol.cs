﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace IronText.Reflection.Managed
{
    class CilSymbol
    {
        public CilSymbol()
        {
            this.Literals = new HashSet<string>();
        }

        public Symbol          Symbol     { get; set; }

        public Type            ThisContext { get; set; }

        public Type            Type       { get; set; }

        public HashSet<string> Literals   { get; private set; }

        public SymbolCategory  Categories { get; set; }

        public int Id
        { 
            get { return Symbol == null ? -1 : Symbol.Index; } 
        }

        public string Name
        {
            get
            {
                if (Literals.Count == 1)
                {
                    return CilSymbolNaming.GetLiteralName(Literals.First());
                }

                return CilSymbolNaming.GetTypeName(Type);
            }
        }

        public override string ToString() { return Name; }
    }
}
