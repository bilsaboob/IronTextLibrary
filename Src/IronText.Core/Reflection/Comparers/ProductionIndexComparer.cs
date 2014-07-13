﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronText.Reflection
{
    internal class ProductionIndexComparer : IEqualityComparer, IEqualityComparer<Production>
    {
        public static readonly ProductionIndexComparer Instance = new ProductionIndexComparer();

        public bool Equals(Production x, Production y)
        {
            return y != null
                && x.Index == y.Index
                && x.Outcome.Index == y.Outcome.Index
                && Enumerable.SequenceEqual(x.Pattern, y.Pattern, SymbolIndexComparer.Instance)
                ;
        }

        public int GetHashCode(Production obj)
        {
            return unchecked(obj.Index + obj.Outcome.Index + obj.Pattern.Length);
        }

        public bool Equals(object x, object y)
        {
            if (x == y)
            {
                return true;
            }

            var xAsProd = x as Production;
            if (xAsProd == null)
            {
                return false;
            }

            var yAsProd = y as Production;
            if (yAsProd == null)
            {
                return false;
            }

            return Equals(xAsProd, yAsProd);
        }

        public int GetHashCode(object obj)
        {
            return GetHashCode((Production)obj);
        }
    }
}