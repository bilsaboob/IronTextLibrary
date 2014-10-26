﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace IronText.Reflection
{
    class ProductionInliner : IProductionComponentVisitor<IProductionComponent>
    {
        private int position;
        private readonly Production inlined;

        public ProductionInliner(Production inlined)
        {
            this.inlined = inlined;
        }

        public Production Execute(Production source, int symbolPosition)
        {
            this.position = symbolPosition;
            var result = (Production)VisitProduction(source);
            result.ExplicitPrecedence = source.ExplicitPrecedence;
            return result;
        }

        public IProductionComponent VisitSymbol(Symbol symbol)
        {
            IProductionComponent result;

            if (0 == position)
            {
                Debug.Assert(symbol == inlined.Outcome);
                result = inlined;
            }
            else
            {
                result = symbol;
            }

            --position;
            return result;
        }

        public IProductionComponent VisitProduction(Production production)
        {
            int count = production.ChildComponents.Length;
            var inlinedComponents = new IProductionComponent[count];
            for (int i = 0; i != count; ++i)
            {
                inlinedComponents[i] = production.ChildComponents[i].Accept(this);
            }

            var result = new Production(
                production.Outcome,
                inlinedComponents,
                production.ContextRef,
                flags: production.Flags);
            result.Joint.AddAll(production.Joint);
            return result;
        }
    }
}
