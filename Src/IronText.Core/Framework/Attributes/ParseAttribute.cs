﻿using System;
using System.Linq;
using System.Reflection;
using IronText.Extensibility;

namespace IronText.Framework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple=true)]
    public class ParseAttribute : RuleMethodAttribute
    {
        public ParseAttribute(params string[] keywordMask) 
        {
            this.KeywordMask = keywordMask ?? new string[0];
        }

        public string[] KeywordMask { get; set; }

        protected override TokenRef[] DoGetRuleMask(MethodInfo methodInfo, ITokenPool tokenPool)
        {
            int placeholderCount = KeywordMask.Count(item => item == null);
            int nonPlaceholderParameterCount = methodInfo.GetParameters().Length - placeholderCount;
            if (nonPlaceholderParameterCount < 0)
            {
                throw new InvalidOperationException("Insufficient rule-method arguments in " + this);
            }

            return KeywordMask
                .Select(item => item == null ? null : tokenPool.GetLiteral(item))
                .ToArray();
        }
    }
}
