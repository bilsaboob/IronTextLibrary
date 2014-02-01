﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronText.Collections;

namespace IronText.Reflection
{
    public class MergerCollection : IndexedCollection<Merger, IEbnfEntities>
    {
        public MergerCollection(IEbnfEntities ebnfContext)
            : base(ebnfContext)
        {
        }
    }
}
