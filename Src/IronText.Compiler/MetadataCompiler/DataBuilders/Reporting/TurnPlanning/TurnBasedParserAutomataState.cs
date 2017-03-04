﻿using IronText.Reporting;
using System.Linq;
using System.Collections.ObjectModel;
using IronText.Automata.TurnPlanning;
using IronText.Reflection;
using IronText.Collections;
using System;

namespace IronText.MetadataCompiler.DataBuilders.Reporting.TurnPlanning
{
    class TurnBasedParserAutomataState : IParserState
    {
        public void Init(
            Grammar                 grammar,
            TurnBasedNameProvider   nameProvider,
            int                     index,
            ShrodingerTokenDfaState state,
            TurnDfaStateDetails     details,
            Func<ShrodingerTokenDfaState,TurnBasedParserAutomataState> toReportState)
        {
            this.Index = index;

            DotItems = details
                .Positions
                .Select(p => (IParserDotItem)new TurnBasedParserDotItem(nameProvider, p))
                .ToList()
                .AsReadOnly();

            Transitions = state
                .Transitions
                .Select(t =>
                    new TurnBasedParserTransition(
                        grammar.Symbols.NameOf(t.Key),
                        t.Value
                        .AllAlternatives()
                        .Select(d =>
                            new TurnBasedParserDecision(
                                nameProvider.NameOfTurn(d.Turn),
                                toReportState(d.NextState)))))
                .Cast<IParserTransition>()
                .ToList()
                .AsReadOnly();
        }

        public int                                   Index       { get; private set; }

        public ReadOnlyCollection<IParserDotItem>    DotItems    { get; private set;}

        public ReadOnlyCollection<IParserTransition> Transitions { get; private set;}
    }
}
