﻿using IronText.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace IronText.Runtime
{
    public class Interpreter : IDisposable
    {
        private object      context;
        private ILogging    logging;
        private LoggingKind logKind;

        public Interpreter(ILanguageRuntime language)
            : this(language.CreateDefaultContext(), language)
        {
        }

        public Interpreter(object context, ILanguageRuntime language)
        {
            this.LanguageRuntime = language;
            this.context = context;
            this.logging = new MemoryLogging();

            // Default behavior
            this.LoggingKind = LoggingKind.ThrowOnError;
        }

        internal protected ILanguageRuntime LanguageRuntime { get; private set; }

        public LoggingKind LoggingKind
        {
            get { return this.logKind; }
    
            set
            {
                if (logKind != value)
                {
                    switch (value)
                    {
                        case LoggingKind.Collect:
                            this.logging = new MemoryLogging();
                            break;
                        case LoggingKind.Console:
                            this.logging = new TextLogging(Console.Out);
                            break;
                        case LoggingKind.ThrowOnError:
                            this.logging = ExceptionLogging.Instance;
                            break;
                        case LoggingKind.None:
                            this.logging = NullLogging.Instance;
                            break;
                        case LoggingKind.Custom:
                            break;
                        default:
                            throw new ArgumentException("Logging kind is not supported.", "value");
                    }

                    this.logKind = value;
                }
            }
        }

        public ILogging CustomLogging
        {
            get { return logKind == LoggingKind.Custom ? this.logging : null; }
            set
            {
                this.LoggingKind = LoggingKind.Custom;
                this.logging = value;
            }
        }

        public object Context
        {
            get { return context; }
            set { context = value; }
        }

        public int ErrorCount { get { return logging.ErrorCount; } }

        public int WarningCount { get { return logging.WarningCount; } }
        
        public List<LogEntry> LogEntries
        {
            get 
            { 
                var asMemoryLogging = logging as MemoryLogging;
                if (asMemoryLogging == null)
                {
                    throw new NotSupportedException("Current LogKind does not support log entries list.");
                }

                return asMemoryLogging.Entries;
            }
        }

        public IEnumerable<Msg> Scan(string input)
        {
            // Note: StringReader is not disposed because resulting
            //       enumerable will use text reader after return
            //       from this method.
            return Scan(new StringReader(input), HLoc.MemoryString);
        }

        public IEnumerable<Msg> Scan(TextReader input, string document)
        {
            Clear();

            var result = LanguageRuntime.CreateScanner(context, input, document, GetCurrentLogging());
            return result;
        }

        public bool Recognize(StreamReader input, string document)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            Clear();

            var scanner  = LanguageRuntime.CreateScanner(context, input, document, GetCurrentLogging());
            var producer = NullProducer<int>.Instance;
            var parser   = LanguageRuntime.CreateParser(producer, GetCurrentLogging());
            scanner.Accept(parser);

            return ErrorCount == 0;
        }

        public bool Parse(TextReader input, string document = null)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (document == null)
            {
                document = HLoc.MemoryString;
            }

            Clear();

            var scanner  = LanguageRuntime.CreateScanner(context, input, document, GetCurrentLogging());
            var producer = LanguageRuntime.CreateProducer(context);
            var parser   = LanguageRuntime.CreateParser(producer, logging);
            scanner.Accept(parser);

            return ErrorCount == 0;
        }

        public bool Parse(string text)
        {
            Clear();
            using (var input = new StringReader(text))
            {
                return Parse(input);
            }
        }

        public bool Parse(IEnumerable<Msg> input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            Clear();

            var producer = LanguageRuntime.CreateProducer(context);
            var parser   = LanguageRuntime.CreateParser(producer, GetCurrentLogging());
            parser.Feed(input).Done();

            return ErrorCount == 0;
        }

        public SppfNode BuildTree(string input)
        {
            using (var reader = new StringReader(input))
            {
                return BuildTree(reader, HLoc.MemoryString);
            }
        }

        public SppfNode BuildTree(TextReader input, string document = null)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (document == null)
            {
                document = HLoc.MemoryString;
            }

            Clear();

            var scanner    = LanguageRuntime.CreateScanner(context, input, document, GetCurrentLogging());
            var producer = new SppfProducer(LanguageRuntime.RuntimeGrammar);
            var parser   = LanguageRuntime.CreateParser(producer, logging);
            scanner.Accept(parser);

            return producer.Result;
        }

        private void Clear()
        {
            logging.Reset();
        }

        ~Interpreter()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.LanguageRuntime = null;
                this.context = null;
                this.logging = null;
            }
        }

        private ILogging GetCurrentLogging()
        {
            return this.logging;
        }
    }
}
