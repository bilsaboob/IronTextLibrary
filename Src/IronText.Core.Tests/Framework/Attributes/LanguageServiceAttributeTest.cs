﻿using IronText.Framework;
using IronText.Logging;
using IronText.Runtime;
using NUnit.Framework;

namespace IronText.Tests.Framework.Attributes
{
    [TestFixture]
    public class LanguageServiceAttributeTest
    {
        [Test]
        public void Test()
        {
            var context = new MyServiceConsumer();
            Language.Parse(context, "foo");
            Assert.IsTrue(context.HasScanning, "Missing scanning service");
            Assert.IsTrue(context.HasParsing, "Missing parsing service");
            Assert.IsTrue(context.HasLanguage, "Missing language service");

            Assert.AreEqual(new Loc(0, 3), context.LastNonTermLocation);
            Assert.AreEqual(new Loc(0, 3), context.LastTermLocation);

            Language.Parse(context, "barr");
            Assert.IsTrue(context.Nested.HasScanning, "Missing nested scanning service");
            Assert.IsTrue(context.Nested.HasParsing, "Missing nested parsing service");
            Assert.IsTrue(context.Nested.HasLanguage, "Missing language service");

            Assert.AreEqual(new Loc(0, 4), context.Nested.LastNonTermLocation);
            Assert.AreEqual(new Loc(0, 4), context.Nested.LastTermLocation);
        }

        [Language]
        [GrammarDocument("MyServiceConsumer.gram")]
        [DescribeParserStateMachine("MyServiceConsumer.info")]
        [ScannerDocument("MyServiceConsumer.scan")]
        public class MyServiceConsumer
        {
            public bool HasScanning;
            public bool HasParsing;
            public bool HasLanguage;
            public Loc  LastNonTermLocation;
            public Loc  LastTermLocation;

            public MyServiceConsumer()
            {
                Nested = new MyNestedServiceConsumer();
            }

            public string Result { get; [Produce] set; }

            [SubContext]
            public MyNestedServiceConsumer Nested { get; private set; }

            [LanguageService]
            public IScanning Scanning { get; set; }

            [LanguageService]
            public IParsing Parsing { get; set; }

            [LanguageService]
            public ILanguageRuntime Language { get; set; }

            [Produce("foo")]
            public string Start() 
            {
                HasParsing = Parsing != null;
                if (HasParsing)
                {
                    LastNonTermLocation = Parsing.Location;
                }

                return null;
            }

            [Literal("foo")]
            public object FooTerm()
            { 
                HasScanning = Scanning != null;
                HasLanguage = Language != null;
                if (HasScanning)
                {
                    LastTermLocation = Scanning.Location;
                }

                return null; 
            }
        }

        [Vocabulary]
        public class MyNestedServiceConsumer
        {
            public bool HasParsing;
            public bool HasScanning;
            public bool HasLanguage;
            public Loc LastNonTermLocation;
            public Loc LastTermLocation;

            [LanguageService]
            public IScanning Scanning { get; set; }

            [LanguageService]
            public IParsing Parsing { get; set; }

            [LanguageService]
            public ILanguageRuntime Language { get; set; }

            [LanguageService]
            public ILogging Logging { get; set; }

            [Produce("barr")]
            public string NestedStart() 
            {
                HasParsing = Parsing != null;
                if (HasParsing)
                {
                    LastNonTermLocation = Parsing.Location;
                }

                return null;
            }

            [Literal("barr")]
            public object BarTerm()
            { 
                HasScanning = Scanning != null;
                HasLanguage = Language != null;
                if (HasScanning)
                {
                    LastTermLocation = Scanning.Location;
                }

                return null; 
            }
        }
    }
}
