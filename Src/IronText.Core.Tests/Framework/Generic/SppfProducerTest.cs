﻿using System;
using System.Collections.Generic;
using System.Linq;
using IronText.Diagnostics;
using IronText.Framework;
using IronText.Runtime;
using IronText.Reflection;
using NUnit.Framework;
using IronText.Misc;
using static IronText.Tests.Framework.Generic.GrammarsUnderTest;

namespace IronText.Tests.Framework.Generic
{
    [TestFixture]
    public class SppfProducerTest
    {
        [Test]
        public void SupportsTrivialMergeLanguage()
        {
            using (var interp = new Interpreter<TrivialMergeLanguage>())
            {
                var sppf = interp.BuildTree("a");

                sppf.WriteIndented(interp.GetGrammar(), Console.Out, 0);
                using (var graph = new GvGraphView(nameof(TrivialMergeLanguage) + "0_sppf_amb.gv"))
                {
                    sppf.WriteGraph(graph, interp.GetGrammar(), true);
                }
            }
        }

        [Test]
        public void SupportsLeftRecursion()
        {
            using (var interp = new Interpreter<LeftRecursion>())
            {
                var sppf = interp.BuildTree("a");

                sppf.WriteIndented(interp.GetGrammar(), Console.Out, 0);
                using (var graph = new GvGraphView(typeof(LeftRecursion).Name + "0_sppf_amb.gv"))
                {
                    sppf.WriteGraph(graph, interp.GetGrammar(), true);
                }
            }
        }

        [Test]
        public void TestEmptyRecursiveTree()
        {
            using (var interp = new Interpreter<EmptyRecursiveTree>())
            {
                var sppf = interp.BuildTree("");
                sppf.WriteIndented(interp.GetGrammar(), Console.Out, 0);
                using (var graph = new GvGraphView(typeof(EmptyRecursiveTree).Name + "0_sppf_amb.gv"))
                {
                    sppf.WriteGraph(graph, interp.GetGrammar(), true);
                }
            }
        }

        [Test]
        public void TestRecursiveTree()
        {
            using (var interp = new Interpreter<RecursiveTree>())
            {
                var sppf = interp.BuildTree("a");
                sppf.WriteIndented(interp.GetGrammar(), Console.Out, 0);
                using (var graph = new GvGraphView(typeof(RecursiveTree).Name + "0_sppf_amb.gv"))
                {
                    sppf.WriteGraph(graph, interp.GetGrammar(), true);
                }
            }
        }

        [Test]
        public void TestShareBranchNodesWithTree()
        {
            using (var interp = new Interpreter<ShareBranchNodesWithTree>())
            {
                var sppf = interp.BuildTree("bb");
                sppf.WriteIndented(interp.GetGrammar(), Console.Out, 0);
                using (var graph = new GvGraphView(typeof(ShareBranchNodesWithTree).Name + "_sppf_amb.gv"))
                {
                    sppf.WriteGraph(graph, interp.GetGrammar(), true);
                }
            }
        }

        [Test]
        public void TestAmbiguousWithEpsilonTree()
        {
            using (var interp = new Interpreter<RightNullableWithTree>())
            {
                var sppf = interp.BuildTree("aaab");
                sppf.WriteIndented(interp.GetGrammar(), Console.Out, 0);
                using (var graph = new GvGraphView(typeof(RightNullableWithTree).Name + "_sppf_amb.gv"))
                {
                    sppf.WriteGraph(graph, interp.GetGrammar(), true);
                }
            }
        }

        [Test]
        public void TestAmbiguousTree()
        {
            var lang = Language.Get(typeof(NondeterministicCalcForTree));
            var gram = lang.GetGrammar();

            using (var interp = new Interpreter<NondeterministicCalcForTree>())
            {
                var sppf = interp.BuildTree("3^3^3");
                sppf.WriteIndented(lang.GetGrammar(), Console.Out, 0);
                using (var graph = new GvGraphView(typeof(NondeterministicCalcForTree).Name + "_sppf_amb.gv"))
                {
                    sppf.WriteGraph(graph, lang.GetGrammar());
                }

                var allNodes = sppf.Flatten().ToArray();

                var NUM = lang.Identify("3");
                var numNodes = allNodes.Where(n => n.GetTokenId(gram) == NUM).Distinct(ReferenceComparer<SppfNode>.Default).ToArray();
                Assert.AreEqual(3, numNodes.Length, "Leaf SPPF nodes should be shared");

                var POW = lang.Identify("^");
                var powNodes = allNodes.Where(n => n.GetTokenId(gram) == POW).Distinct(ReferenceComparer<SppfNode>.Default).ToArray();
                Assert.AreEqual(2, powNodes.Length, "Leaf SPPF nodes should be shared");
            }
        }

        [Test]
        public void TestDeterministicTree()
        {
            using (var interp = new Interpreter<NondeterministicCalcForTree>())
            {
                var sppf = interp.BuildTree("3^3");
                sppf.WriteIndented(interp.GetGrammar(), Console.Out, 0);
                using (var graph = new GvGraphView(typeof(NondeterministicCalcForTree).Name + "_sppf_det.gv"))
                {
                    sppf.WriteGraph(graph, interp.GetGrammar());
                }
            }
        }

        [Test]
        public void TestDeterministicTree0()
        {
            using (var interp = new Interpreter<NondeterministicCalcForTree>())
            {
                var sppf = interp.BuildTree("3");
                sppf.WriteIndented(interp.GetGrammar(), Console.Out, 0);
                using (var graph = new GvGraphView(typeof(NondeterministicCalcForTree).Name + "0_sppf_det.gv"))
                {
                    sppf.WriteGraph(graph, interp.GetGrammar());
                }
            }
        }

        [Language(RuntimeOptions.ForceGeneric)]
        [ParserGraph("NondeterministicCalcForTree.gv")]
        public class NondeterministicCalcForTree
        {
            public readonly List<double> Results = new List<double>();

            [Produce]
            public void AddResult(double e) { Results.Add(e); }

            [Produce(null, "^", null)]
            public double Pow(double e1, double e2) { return Math.Pow(e1, e2); }

            [Produce("3")]
            public double Number() { return 3; }
        }

        [Language(RuntimeOptions.ForceGeneric)]
        [ParserGraph("RightNullableWithTree.gv")]
        public interface RightNullableWithTree
        {
            [Produce(null, null, "a", "b")]
            void S(B b, D d);

            [Produce("a", null, "a", "d")]
            void S(D d);

            [Produce("a")]
            D D(A a, B b);

            [Produce("a")]
            A A(B b1, B b2);

            [Produce]
            A A();

            [Produce]
            B B();
        }

        [Language(RuntimeOptions.ForceGeneric)]
        [ParserGraph(nameof(EmptyRecursiveTree) +"0.gv")]
        [DescribeParserStateMachine(nameof(EmptyRecursiveTree) + "0.info")]
        public interface EmptyRecursiveTree
        {
            [Produce]
            void Start(S s);

            [Produce]
            S Sdouble(S s1, S s2);

            [Produce]
            S Sempty();
        }

        [Language(RuntimeOptions.ForceGeneric)]
        [ParserGraph("RecursiveTree0.gv")]
        [DescribeParserStateMachine(nameof(RecursiveTree) + "0.info")]
        public interface RecursiveTree
        {
            [Produce]
            void Start(S s);

            [Produce]
            S Sdouble(S s1, S s2);

            [Produce("a")]
            S Sa();

            [Produce]
            S Sempty();
        }

        [Language(RuntimeOptions.ForceGeneric)]
        [ParserGraph("ShareBranchNodesWithTree.gv")]
        [DescribeParserStateMachine("ShareBranchNodesWithTree.info")]
        public interface ShareBranchNodesWithTree
        {
            [Produce]
            void Start(S s);

            [Produce("b")]
            S Sdouble(B b, S s1, S s2);

            [Produce("a")]
            S Sa();

            [Produce]
            S Sempty();

            [Produce]
            B Bempty();
        }

        public interface D {}
        public interface A {}
        public interface B {}
        public interface S {}
    }
}