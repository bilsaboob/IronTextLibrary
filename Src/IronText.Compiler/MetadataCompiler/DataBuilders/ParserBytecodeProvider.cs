﻿using IronText.Algorithm;
using IronText.Automata.Lalr1;
using IronText.Runtime;
using System.Collections.Generic;

namespace IronText.MetadataCompiler
{
    class ParserBytecodeProvider
    {
        public ParserBytecodeProvider(ILrParserTable parserTable)
        {
            var instructions = new List<ParserAction>();

            var table = parserTable.GetParserActionTable();
            int rowCount    = table.RowCount;
            int columnCount = table.ColumnCount;

            var startTable = new MutableTable<int>(rowCount, columnCount);

            for (int r = 0; r != rowCount; ++r)
                for (int c = 0; c != columnCount; ++c)
                {
                    var action = table.Get(r, c);
                    int start = instructions.Count;

                    startTable.Set(r, c, start);
                    instructions.Add(action);
                    switch (action.Kind)
                    {
                        case ParserActionKind.Resolve:
                        case ParserActionKind.Reduce:
                            instructions.Add(ParserAction.ContinueAction);
                            break;
                        case ParserActionKind.Shift:
                            instructions.Add(ParserAction.ExitAction);
                            break;
                        default:
                            // safety instruction to avoid invalid instruction access
                            instructions.Add(ParserAction.InternalErrorAction);
                            break;
                    }
                }

            this.Instructions = instructions.ToArray();
            this.StartTable   = startTable;
        }

        public ParserAction[] Instructions { get; }

        public ITable<int>    StartTable   { get; }
    }
}