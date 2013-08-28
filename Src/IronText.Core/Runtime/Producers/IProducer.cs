﻿using IronText.Algorithm;

namespace IronText.Framework
{
    /// <summary>
    /// Represents abstract producer for parser results.
    /// </summary>
    /// <remarks>
    /// This absraction can represent following scenarios:
    ///  - Grammar actions. Values are merged according to a grammar merge rules (like in Elkhound).
    ///  - SPPF (Shared Packed Parse Forest). SPPF is bulit as it is described in paper 
    ///    (Right-nullable GLR parsers).
    ///  - AST. Classical AST is built when there is single derivation alternative, otherwise
    ///    alternative derivation branches are merged according to the user defined logic or failure is
    ///    is reported.
    /// </remarks>
    /// <typeparam name="T">Node of the tree or just merged value representation</typeparam>
    public interface IProducer<T>
    {
        ReductionOrder ReductionOrder { get; }

        // Producer used just before error recovery start
        IProducer<T> GetErrorRecoveryProducer();

        T Result { get; set; }

        // Leaf corresponding to the input terminal
        T CreateLeaf(Msg msg);

        // Branch for production rule
        T CreateBranch(BnfRule rule, ArraySlice<T> parts, IStackLookback<T> lookback);

        // Merge derivation alternatives
        T Merge(T alt1, T alt2, IStackLookback<T> lookback);

        // Get epsilon node corresponding to the non-term
        T GetEpsilonNonTerm(int nonTerm, IStackLookback<T> lookback);

        // Fill epsilon suffix
        void FillEpsilonSuffix(int ruleId, int prefixSize, T[] buffer, int destIndex, IStackLookback<T> lookback);
    }
}
