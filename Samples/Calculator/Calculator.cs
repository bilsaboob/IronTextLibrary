﻿using System;
using System.Collections.Generic;
using IronText.Framework;

namespace Calculator
{
    [Language]
    [NonAssoc(0, "=")]
    [LeftAssoc(1, "-")]
    [LeftAssoc(1, "+")]
    [LeftAssoc(2, "*")]
    [LeftAssoc(2, "/")]
    [LeftAssoc(2, "%")]
    [RightAssoc(10, "^")]
    // [DescribeParserStateMachine("Calculator.info")]
    // [ParserGraph("Calculator.gv")]
    public class Calculator
    {
        const string _ = null;

        public readonly Dictionary<string,double> Variables = new Dictionary<string,double>();

        [ParseResult]
        public double Result { get; set; }

        public bool Done { get; set; }

        [Parse]
        public double Number(Const<double> c) { return c == null ? 0 : c.Value; }

        [Parse]
        public double VarRef(string name) { return Variables[name]; }

        [Parse(_, "+", _)]
        public double Plus(double x, double y) { return  x + y; }
        
        [Parse(_, "-", _)]
        public double Minus(double x, double y) { return  x - y; }

        [Parse(_, "*", _)]
        public double Prod(double x, double y) { return  x * y; }

        [Parse(_, "/", _)]
        public double Div(double x, double y) { return  x / y; }

        [Parse(_, "%", _)]
        public double Mod(double x, double y) { return  x % y; }

        [Parse(_, "^", _)]
        public double Pow(double x, double y) { return  Math.Pow(x, y); }

        [Parse("sqrt", "(", _, ")")]
        public double Sqrt(double x) { return Math.Sqrt(x); }

        [Parse("sin", "(", _, ")")]
        public double Sin(double x) { return Math.Sin(x); }

        [Parse("cos", "(", _, ")")]
        public double Cos(double x) { return Math.Cos(x); }

        [Parse(_, "=", _)]
        public double Let(string var, double rexpr) { Variables[var] = rexpr; return 0; }

        [Parse("print", "(", _, ")")]
        public double Print(double expr) { Console.WriteLine(expr); return 0; }

        [Parse("exit")]
        [Parse("quit")]
        public void Exit() { Done = true; }

        [Scan("blank+")]
        public void Space() { }

        [Scan("alpha alnum*")]
        public string Identifier(string name) { return name; }

        [Scan("digit+ ('.' digit*)?")]
        public Const<double> Number(string text)
        { 
            return new Const<double>(double.Parse(text));
        }

        public class Const<T> 
        {
            public readonly T Value;

            public Const(T value) { Value = value; }

            public override string ToString()
            {
                return Value.ToString();
            }
        }
    }
}
