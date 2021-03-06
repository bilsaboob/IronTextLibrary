﻿using IronText.Reflection;

namespace IronText.Runtime
{
    struct ModifiedReduction
    { 
        public readonly Production Rule;
        public readonly short Size;

        public ModifiedReduction(Production rule, short size)
        {
            this.Rule = rule;
            this.Size = size;
        }
    }
}
