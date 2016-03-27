﻿using IronText.Runtime.Semantics;

namespace IronText.Reflection
{
    public interface ISemanticValue
    {
        IRuntimeValue ToRuntime(int currentProductionPosition);
    }
}