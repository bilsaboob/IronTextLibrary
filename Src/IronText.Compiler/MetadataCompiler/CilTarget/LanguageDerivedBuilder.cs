﻿using System;
using System.Reflection;
using IronText.Automata.Regular;
using IronText.Build;
using IronText.Framework;
using IronText.Lib.IL;
using IronText.Lib.IL.Generators;
using IronText.Lib.Shared;
using IronText.Logging;
using IronText.Misc;
using IronText.Reflection;
using IronText.Reflection.Managed;
using IronText.Runtime;

namespace IronText.MetadataCompiler
{
    public class LanguageDerivedBuilder : IDerivedBuilder<CilDocumentSyntax>
    {
        private const string GetGrammarBytes                    = "GetGrammarBytes";
        private const string GetRtGrammarBytes                  = "GetRtGrammarBytes";
        private const string CreateTokenKeyToIdMethodName       = "CreateTokenKeyToId";
        private const string ProductionActionMethodName         = "ProducitonAction";
        private const string MergeActionMethodName              = "MergeAction";
        private const string TermFactoryMethodName              = "TermFactory";
        private const string GetParserActionMethodName          = "GetParserAction";
        private const string ScanMethodName                     = "ScannerDfa";
        private const string CreateStateToSymbolMethodName      = "CreateStateToSymbol";
        private const string CreateParserActionConflictsMethodName = "CreateParserActionConflicts";
        private const string CreateTokenComplexityTableMethodName = "CreateTokenComplexity";
        private const string CreateMatchActionToTokenTable      = "CreateMatchActionToTokenTable";
        private const string CreateDefaultContextMethodName     = "InternalCreateDefaultContext";

        private readonly LanguageData data;
        private readonly TypedLanguageSource languageName;
        private Ref<Types> declaringTypeRef;
        private readonly ImplementationGenerator implementationGenerator;
        private ILogging logging;

        public LanguageDerivedBuilder(Type definitionType)
        {
            this.languageName = new TypedLanguageSource(definitionType);

            var dataProvider = new LanguageDataProvider(languageName, false);
            ResourceContext.Instance.LoadOrBuild(dataProvider, out this.data);

            // TODO: Share abstraction impelementation between languages i.e. shared plan
            this.implementationGenerator = new ImplementationGenerator(
                                    null,
                                    IsMethodWithNonNullResult);
        }

        public CilDocumentSyntax Build(ILogging logging, CilDocumentSyntax context)
        {
            if (data == null)
            {
                logging.Write(
                    new LogEntry
                    {
                        Severity = Severity.Error,
                        Message = string.Format(
                            "Failed to compile '{0}' language definition.",
                            languageName.FullLanguageName),
                        Origin = languageName.GrammarOrigin
                    });

                return null;
            }

            this.logging = logging;

            this.declaringTypeRef = context.Types.Class_(
                            ClassName.Parse(languageName.LanguageTypeName));

            logging.Write(
                new LogEntry
                {
                    Severity = Severity.Verbose,
                    Message = string.Format("Started Compiling Derived assembly for {0}", languageName.FullLanguageName)
                });

            var result = context
                .Class_()
                        .Public
                        .Named(languageName.LanguageTypeName)
                        .Extends(context.Types.Import(typeof(LanguageBase)))
                    .Do(BuildMethod_CreateGrammar)
                    .Do(BuildMethod_CreateRtGrammar)
                    .Do(BuildMethod_GetParserAction)
                    .Do(BuildMethod_CreateTokenIdentities)
                    .Do(BuildMethod_Scan1)
                    .Do(BuildMethod_TermFactory)
                    .Do(BuildMethod_ProductionAction)
                    .Do(BuildMethod_MergeAction)
                    .Do(BuildMethod_CreateStateToSymbol)
                    .Do(BuildMethod_CreateParserActionConflicts)
                    .Do(BuildMethod_CreateTokenComplexityTable)
                    .Do(BuildMethod_CreateMatchActionToTokenTable)
                    .Do(BuildMethod_CreateDefaultContext)
                    .Do(Build_Ctor)
                .EndClass()
                ;

            logging.Write(
                new LogEntry
                {
                    Severity = Severity.Verbose,
                    Message = string.Format("Done Compiling Derived assembly for {0}", languageName.FullLanguageName)
                });

            implementationGenerator.Generate(context);
            return result;
        }

        private ClassSyntax BuildMethod_ProductionAction(ClassSyntax context)
        {
            var generator = new ProductionActionGenerator();
            return generator.BuildMethod(context, ProductionActionMethodName, data);
        }

        private ClassSyntax BuildMethod_MergeAction(ClassSyntax context)
        {
            var generator = new MergeActionGenerator();
            generator.BuildMethod(context, MergeActionMethodName, data);
            return context;
        }

        private ClassSyntax BuildMethod_Scan1(ClassSyntax context)
        {
            logging.Write(
                new LogEntry
                {
                    Severity = Severity.Verbose,
                    Message = string.Format("Started compiling Scan1 modes for {0} language", languageName.LanguageName)
                });

            ITdfaData dfa = data.ScannerTdfa;
            var dfaSerialization = new DfaSerialization(dfa);
            var generator = new ScannerGenerator(dfaSerialization);

            var emit = context
                .PrivateStaticMethod(ScanMethodName, typeof(Scan1Delegate))
                    .NoInlining
                    .NoOptimization
                .BeginBody();

            generator.Build(emit);

            context = emit.EndBody();

            logging.Write(
                new LogEntry
                {
                    Severity = Severity.Verbose,
                    Message = string.Format("Done compiling Scan1 modes for {0} language", languageName.LanguageName)
                });
            return context;
        }

        private ClassSyntax BuildMethod_TermFactory(ClassSyntax context)
        {
            var generator = new TermFactoryGenerator(data);

            var emit = context.PrivateStaticMethod(TermFactoryMethodName, typeof(TermFactoryDelegate))
                .BeginBody();

            generator.Build(
                emit,
                il => il.Ldarg(0),
                il => il.Ldarg(1),
                il => il.Ldarg(2));

            return emit.EndBody();
        }

        private ClassSyntax BuildMethod_CreateTokenIdentities(ClassSyntax context)
        {
            var generator = new TokenIdentitiesSerializer(data.Grammar);

            return context
                .PrivateStaticMethod(
                    CreateTokenKeyToIdMethodName,
                    typeof(Func<>).MakeGenericType(LanguageBase.Fields.tokenKeyToId.FieldType))
                .BeginBody()
                    .Do(generator.Build)
                    .Ret()
                .EndBody();
        }

        private ClassSyntax BuildMethod_GetParserAction(ClassSyntax context)
        {
            var generator = new ReadOnlyTableGenerator(
                                    data.ParserActionTable,
                                    il => il.Ldarg(0),
                                    il => il.Ldarg(1));

            return context
                .PrivateStaticMethod(GetParserActionMethodName, typeof(TransitionDelegate))        
                    .NoOptimization
                .BeginBody()
                    .Do(generator.Build)
                    .Ret()
                .EndBody();
        }

        private ClassSyntax BuildMethod_CreateDefaultContext(ClassSyntax context)
        {
            return context
                .PrivateStaticMethod(CreateDefaultContextMethodName, typeof(Func<object>))
                .BeginBody()
                    // Plan implementation of abstraction as needed
                    .Do(il=> implementationGenerator
                                .EmitFactoryCode(il, languageName.DefinitionType))
                    .Ret()
                .EndBody();
        }

        private static bool IsMethodWithNonNullResult(MethodInfo method)
        {
            if (method.Name.StartsWith("get_"))
            {
                var prop = method.DeclaringType.GetProperty(method.Name.Substring(4));
                if (prop != null && Attributes.Exists<SubContextAttribute>(prop))
                {
                    return true;
                }
            }

            return Attributes.Exists<DemandAttribute>(method.ReturnType);
        }

        private ClassSyntax BuildMethod_CreateGrammar(ClassSyntax context)
        {
            var generator = new CilByteGenerator<Grammar>(data.Grammar);

            return context
                .PrivateStaticMethod(GetGrammarBytes, typeof(Func<byte[]>))
                .BeginBody()
                    .Do(generator.Build)
                    .Ret()
                .EndBody();
        }

        private ClassSyntax BuildMethod_CreateRtGrammar(ClassSyntax context)
        {
            var generator = new CilByteGenerator<RuntimeGrammar>(data.RuntimeGrammar);

            return context
                .PrivateStaticMethod(GetRtGrammarBytes, typeof(Func<byte[]>))
                .BeginBody()
                    .Do(generator.Build)
                    .Ret()
                .EndBody();
        }

        private ClassSyntax BuildMethod_CreateParserActionConflicts(ClassSyntax context)
        {
            var emit = context
                        .PrivateStaticMethod(CreateParserActionConflictsMethodName, typeof(Func<int[]>))
                        .BeginBody();
            
            var resultLoc = emit.Locals.Generate().GetRef();
            var itemLoc   = emit.Locals.Generate().GetRef();
            var conflicts = data.ParserConflictActionTable;

            emit = emit
                .Local(resultLoc.Def, typeof(int[]))
                .Ldc_I4(conflicts.Length)
                .Newarr(typeof(int))
                .Stloc(resultLoc)
                ;

            for (int i = 0; i != conflicts.Length; ++i)
            {
                emit = emit
                    .Ldloc(resultLoc)
                    .Ldc_I4(i)
                    .Ldc_I4(conflicts[i])
                    .Stelem_I4();
            }

            return emit
                    .Ldloc(resultLoc)
                    .Ret()
                .EndBody();
        }

        private ClassSyntax BuildMethod_CreateMatchActionToTokenTable(ClassSyntax context)
        {
            var emit = context
                        .PrivateStaticMethod(CreateMatchActionToTokenTable, typeof(Func<int[]>))
                        .BeginBody();
            
            var resultLoc = emit.Locals.Generate().GetRef();
            var itemLoc   = emit.Locals.Generate().GetRef();
            var table     = data.MatchActionToToken;

            emit = emit
                .Local(resultLoc.Def, typeof(int[]))
                .Ldc_I4(table.Length)
                .Newarr(typeof(int))
                .Stloc(resultLoc)
                ;

            for (int i = 0; i != table.Length; ++i)
            {
                emit
                    .Ldloc(resultLoc)
                    .Ldc_I4(i)
                    .Ldc_I4(table[i])
                    .Stelem_I4();
            }

            return emit
                    .Ldloc(resultLoc)
                    .Ret()
                .EndBody();
        }

        private ClassSyntax BuildMethod_CreateTokenComplexityTable(ClassSyntax context)
        {
            var emit = context
                        .PrivateStaticMethod(CreateTokenComplexityTableMethodName, typeof(Func<int[]>))
                        .BeginBody();
            
            var resultLoc = emit.Locals.Generate().GetRef();
            var itemLoc   = emit.Locals.Generate().GetRef();
            var table     = data.TokenComplexity;

            emit = emit
                .Local(resultLoc.Def, typeof(int[]))
                .Ldc_I4(table.Length)
                .Newarr(typeof(int))
                .Stloc(resultLoc)
                ;

            for (int i = 0; i != table.Length; ++i)
            {
                emit
                    .Ldloc(resultLoc)
                    .Ldc_I4(i)
                    .Ldc_I4(table[i])
                    .Stelem_I4();
            }

            return emit
                    .Ldloc(resultLoc)
                    .Ret()
                .EndBody();
        }

        private ClassSyntax BuildMethod_CreateStateToSymbol(ClassSyntax context)
        {
            var emit = context
                        .PrivateStaticMethod(CreateStateToSymbolMethodName, typeof(Func<int[]>))
                        .BeginBody();
            var resultLoc = emit.Locals.Generate().GetRef();
            var stateToSymbol = data.StateToToken;

            emit = emit
                .Local(resultLoc.Def, typeof(int[]))
                .Ldc_I4(stateToSymbol.Length)
                .Newarr(typeof(int))
                .Stloc(resultLoc);

            for (int i = 0; i != stateToSymbol.Length; ++i)
            {
                emit = emit
                    .Ldloc(resultLoc)
                    .Ldc_I4(i)
                    .Ldc_I4(stateToSymbol[i])
                    .Stelem_I4();
            }
                
            return emit
                    .Ldloc(resultLoc)
                    .Ret()
                .EndBody();
        }

        private ClassSyntax Build_Ctor(ClassSyntax context)
        {
            var emit = context
                        .Method()
                            .Public.Instance
                            .Returning(context.Types.Void)
                            .Named(".ctor")
                                .BeginArgs()
                                .EndArgs()
                            .BeginBody();

            // Call base constructor:
            // this:
            emit = emit
                .Ldarg(0) // this
                .Call(emit.Methods.Import(typeof(LanguageBase).GetConstructor(Type.EmptyTypes)))
                ;

            emit
                .Ldarg(0)
                .Ldc_I4(data.IsDeterministic ? 1 : 0)
                .Stfld(LanguageBase.Fields.isDeterministic);

            return emit
                // Init grammar
                .Ldarg(0)
                .Call(emit.Methods.Method(
                    _=>_
                        .StartSignature
                        .Returning(emit.Types.Import(typeof(byte[])))
                        .DecaringType(declaringTypeRef)
                        .Named(GetGrammarBytes)
                        .BeginArgs()
                        .EndArgs()
                    ))
                .Stfld(LanguageBase.Fields.grammarBytes)

                // Init runtime grammar
                .Ldarg(0)
                .Call(emit.Methods.Method(
                    _=>_
                        .StartSignature
                        .Returning(emit.Types.Import(typeof(byte[])))
                        .DecaringType(declaringTypeRef)
                        .Named(GetRtGrammarBytes)
                        .BeginArgs()
                        .EndArgs()
                    ))
                .Stfld(LanguageBase.Fields.rtGrammarBytes)

                // Init state->token table
                .Ldarg(0)
                .Call(emit.Methods.Method(
                    _=>_
                        .StartSignature
                        .Returning(emit.Types.Import(typeof(int[])))
                        .DecaringType(declaringTypeRef)
                        .Named(CreateStateToSymbolMethodName)
                        .BeginArgs()
                        .EndArgs()
                    ))
                .Stfld(LanguageBase.Fields.stateToSymbol)

                // Init parser action conflicts table
                .Ldarg(0)
                .Call(emit.Methods.Method(
                    _=>_
                        .StartSignature
                        .Returning(emit.Types.Import(typeof(int[])))
                        .DecaringType(declaringTypeRef)
                        .Named(CreateParserActionConflictsMethodName)
                        .BeginArgs()
                        .EndArgs()
                    ))
                .Stfld(LanguageBase.Fields.parserConflictActions)

                // Init token complexity table
                .Ldarg(0)
                .Call(emit.Methods.Method(
                    _=>_
                        .StartSignature
                        .Returning(emit.Types.Import(typeof(int[])))
                        .DecaringType(declaringTypeRef)
                        .Named(CreateTokenComplexityTableMethodName)
                        .BeginArgs()
                        .EndArgs()
                    ))
                .Stfld(LanguageBase.Fields.tokenComplexity)

                // Init matcher to token table
                .Ldarg(0)
                .Call(emit.Methods.Method(
                    _=>_
                        .StartSignature
                        .Returning(emit.Types.Import(typeof(int[])))
                        .DecaringType(declaringTypeRef)
                        .Named(CreateMatchActionToTokenTable)
                        .BeginArgs()
                        .EndArgs()
                    ))
                .Stfld(LanguageBase.Fields.matcherToToken)

                // Init grammarAction field
                .Ldarg(0)
                .LdMethodDelegate(
                    declaringTypeRef,
                    ProductionActionMethodName,
                    typeof(ProductionActionDelegate))
                .Stfld(LanguageBase.Fields.grammarAction)

                // Init grammarAction field
                .Ldarg(0)
                .LdMethodDelegate(
                    declaringTypeRef,
                    MergeActionMethodName,
                    typeof(MergeDelegate))
                .Stfld(LanguageBase.Fields.merge)


                // Init tokenIdentities
                .Ldarg(0)
                .Call(emit
                        .Methods.Method(
                            _=>_ 
                            .StartSignature
                            .Returning(emit.Types.Import(LanguageBase.Fields.tokenKeyToId.FieldType))
                            .DecaringType(declaringTypeRef)
                            .Named(CreateTokenKeyToIdMethodName)
                            .BeginArgs()
                            .EndArgs() ))
                .Stfld(LanguageBase.Fields.tokenKeyToId)


                // Init scan field
                .Ldarg(0)
                .LdMethodDelegate(
                    declaringTypeRef,
                    ScanMethodName,
                    typeof(Scan1Delegate))
                .Stfld(LanguageBase.Fields.scan1)

                // Init termFactory field
                .Ldarg(0)
                .LdMethodDelegate(
                    declaringTypeRef,
                    TermFactoryMethodName,
                    typeof(TermFactoryDelegate))
                .Stfld(LanguageBase.Fields.termFactory)

                // Init getParserAction field
                .Ldarg(0)
                .LdMethodDelegate(
                    declaringTypeRef,
                    GetParserActionMethodName,
                    typeof(TransitionDelegate))
                .Stfld(LanguageBase.Fields.getParserAction)

                // Init defaul context factory
                .Ldarg(0)
                .LdMethodDelegate(
                    declaringTypeRef,
                    CreateDefaultContextMethodName,
                    typeof(Func<object>))
                .Stfld(LanguageBase.Fields.createDefaultContext)

                .Ret()
            .EndBody();
        }
    }
}
