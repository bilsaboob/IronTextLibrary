﻿using System;
using System.IO;
using IronText.Logging;
using IronText.Reflection;
using IronText.Runtime;

namespace IronText.Runtime
{
    public interface ILanguage
    {
        LanguageName Name { get; }

        EbnfGrammar  Grammar { get; }

        /// <summary>
        /// Determines whether parsing algorithm being used is deterministic.
        /// </summary>
        bool IsDeterministic { get; }

        object CreateDefaultContext();

        IProducer<Msg> CreateActionProducer(object context);

        IScanner CreateScanner(object context, TextReader input, string document, ILogging logging = null);

        IPushParser CreateParser<TNode>(IProducer<TNode> producer, ILogging logging = null);

        int Identify(string literal);

        int Identify(Type tokenType);

        void Heatup();
    }

    public static class LanguageExtensions
    {
        public static Msg Literal(this ILanguage @this, string keyword)
        {
            var id = @this.Identify(keyword);
            return new Msg(id, null, Loc.Unknown);
        }

        public static Msg Token<T>(this ILanguage @this) where T : new()
        {
            return @this.Token(new T());
        }

        public static Msg Token<T>(this ILanguage @this, T value)
        {
            var id = @this.IdentifyTokenValue(value);
            return new Msg(id, value, Loc.Unknown);
        }

        public static int IdentifyTokenValue(this ILanguage @this, object token)
        {
            if (token == null)
            {
                return EbnfGrammar.Eoi;
            }

            return @this.Identify(token.GetType());
        }
    }
}