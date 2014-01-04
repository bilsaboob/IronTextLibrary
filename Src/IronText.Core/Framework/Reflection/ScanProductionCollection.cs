﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronText.Collections;

namespace IronText.Framework.Reflection
{
    public class ScanProductionCollection : IndexedCollection<ScanProduction,IEbnfContext>
    {
        public ScanProductionCollection(IEbnfContext context)
            : base(context)
        {
        }
    }
}
