﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronText.Framework
{
    class ParserCorrectionModel : List<int>
    {
        public const int Spelling = -1;
        public const int Insertion = -2;

        private readonly string messageFormat;
        private int requiredInputSize;
        private int minimalInputSize;

        public ParserCorrectionModel(string messageFormat, int requiredInputSize = 0)
        {
            this.messageFormat = messageFormat;
            this.requiredInputSize = requiredInputSize;
        }

        public Loc GetHiglightLocation(List<Msg> source)
        {
            for (int i = 0; i != Count; ++i)
            {
                if (this[i] != i)
                {
                    if (source[i].Location.IsUnknown)
                    {
                        break;
                    }

                    return source[i].Location;
                }
            }

            Loc result = source[1].Location;
            if (result.IsUnknown)
            {
                result = source[0].Location;
            }

            return result;
        }

        public HLoc GetHiglightHLocation(List<Msg> source)
        {
            for (int i = 0; i != Count; ++i)
            {
                if (this[i] != i)
                {
                    if (source[i].HLocation.IsUnknown)
                    {
                        break;
                    }

                    return source[i].HLocation;
                }
            }

            HLoc result = source[1].HLocation;
            if (result.IsUnknown)
            {
                result = source[0].HLocation;
            }

            return result;
        }

        public string FormatMessage(BnfGrammar grammar, List<Msg> source, List<Msg> corrected)
        {
            var output = new StringBuilder();
            ProcessMessageFormat(grammar, source, corrected, output);
            return output.ToString();
        }

        private void ProcessMessageFormat(
            BnfGrammar grammar,
            List<Msg> source,
            List<Msg> corrected,
            StringBuilder output)
        {
            int i = 0;
            int count = messageFormat.Length;
            while (i != count)
            {
                char ch = messageFormat[i];
                switch (ch)
                {
                    case '%':
                        ++i;
                        if (i == count || !char.IsDigit(messageFormat[i]))
                        {
                            throw new InvalidOperationException("Invalid message format.");
                        }

                        int correctedIndex = messageFormat[i++] - '0';
                        output.Append(FormatToken(grammar, corrected[correctedIndex]));
                        break;
                    case '$':
                        ++i;
                        if (i == count || !char.IsDigit(messageFormat[i]))
                        {
                            throw new InvalidOperationException("Invalid message format.");
                        }

                        int sourceIndex = messageFormat[i++] - '0';
                        output.Append(FormatToken(grammar, source[sourceIndex]));
                        break;
                    default:
                        output.Append(messageFormat[i++]);
                        break;
                }
            }
        }

        private string FormatToken(BnfGrammar grammar, Msg msg)
        {
            if (msg.Id == BnfGrammar.Eoi)
            {
                return "end of file";
            }

            string result = grammar.TokenName(msg.Id);
            if (!result.StartsWith("'") && msg.Value != null)
            {
                result = msg.Value.ToString();
            }

            return result;
        }

        public int GetRequiredInputSize()
        {
            if (requiredInputSize == 0)
            {
                requiredInputSize = this.Max() + 1;
            }

            return requiredInputSize;
        }

        // Minimal matching length
        public int GetMinimalLength()
        {
            if (minimalInputSize == 0)
            {
                int size = GetRequiredInputSize();
                var original = Enumerable.Range(0, size).Reverse().ToArray();
                var reversed = Enumerable.Reverse(this).ToArray();

                minimalInputSize = size;  
                
                int maxCommonTailCount = Math.Min(size, Count);

                for (int i = 0; i != maxCommonTailCount; ++i)
                {
                    if (original[i] != reversed[i])
                    {
                        break;
                    }

                    --minimalInputSize;
                }
            }

            return minimalInputSize;
        }

        public IEnumerable<int> GetDeletedIndexes()
        {
            int size = GetRequiredInputSize();
            return Enumerable
                    .Range(0, size)
                    .Except(this)
                    .ToArray();
        }
    }
}
