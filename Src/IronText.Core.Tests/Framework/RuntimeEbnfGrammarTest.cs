﻿using IronText.Framework;
using IronText.Framework.Reflection;
using NUnit.Framework;

namespace IronText.Tests.Framework
{
    [TestFixture]
    public class RuntimeBnfGrammarTest
    {
        [Test]
        public void IsNullableTest()
        {
            var grammar = new EbnfGrammar();

            var S = grammar.Symbols.Add("S");
            var a = grammar.Symbols.Add("a");
            var b = grammar.Symbols.Add("b");
            var A = grammar.Symbols.Add("A");
            var B = grammar.Symbols.Add("B");

            grammar.Start = S;
            grammar.Productions.Define(S, new[] { b, A });
            grammar.Productions.Define(A, new[] { a, A, B });
            grammar.Productions.Define(A, new Symbol[0]);
            grammar.Productions.Define(B, new Symbol[0]);

            var target = new RuntimeEbnfGrammar(grammar);

            Assert.IsTrue(target.IsNullable(A.Index));
            Assert.IsTrue(target.IsNullable(B.Index));

            Assert.IsFalse(target.IsNullable(a.Index));
            Assert.IsFalse(target.IsNullable(b.Index));
            Assert.IsFalse(target.IsNullable(S.Index));
        }
    }
}
