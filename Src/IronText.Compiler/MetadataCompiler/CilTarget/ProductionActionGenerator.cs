﻿using System.Diagnostics;
using System.Linq;
using IronText.Lib.IL;
using IronText.Lib.Shared;
using IronText.Reflection;
using IronText.Reflection.Managed;
using IronText.Runtime;
using IronText.MetadataCompiler.CilTarget;
using IronText.Framework;
using IronText.Compilation;
using System;

namespace IronText.MetadataCompiler
{
    internal class ProductionActionGenerator
    {
        public ProductionActionGenerator()
        {
        }

        public ClassSyntax BuildMethod(ClassSyntax context, string methodName, LanguageData data)
        {
            var args = context.Method()
                            .Static
                            .Returning(context.Types.Object)
                            .Named(methodName)
                            .BeginArgs();

            Def<Args> ruleId        = args.Args.Generate("ruleId");
            Def<Args> ruleArgs      = args.Args.Generate("ruleArgs");
            Def<Args> argsStart     = args.Args.Generate("argsStart");
            Def<Args> ctx           = args.Args.Generate("rootContext");
            Def<Args> stackLookback = args.Args.Generate("startLookback");

            var emit = args
                    .Argument(context.Types.Int32, ruleId)
                    .Argument(context.Types.Import(typeof(ActionNode[])), ruleArgs)
                    .Argument(context.Types.Int32, argsStart)
                    .Argument(context.Types.Object, ctx)
                    .Argument(context.Types.Import(typeof(IStackLookback<ActionNode>)), stackLookback)
                    .EndArgs()

                .BeginBody();

            BuildBody(
                emit, 
                data, 
                ruleId.GetRef(),
                ruleArgs.GetRef(),
                argsStart.GetRef(),
                ctx.GetRef(),
                stackLookback.GetRef());

            return emit.EndBody();
        }

        public void BuildBody(EmitSyntax emit, LanguageData data, Ref<Args>[] args)
        {
            BuildBody(
                emit,
                data,
                ruleId:     args[0],
                ruleArgs:   args[1],
                argsStart:  args[2],
                ctx:        args[3],
                lookbackStart: args[4]);
        }

        private void BuildBody(
            EmitSyntax emit, 
            LanguageData data,
            Ref<Args> ruleId,
            Ref<Args> ruleArgs,
            Ref<Args> argsStart,
            Ref<Args> ctx,
            Ref<Args> lookbackStart)
        {
            var varStack = new VarsStack(Fluent.Create(emit));

            Def<Labels> returnLabel = emit.Labels.Generate();

            var globalSemanticCode = new GlobalSemanticLoader(emit, il => il.Ldarg(ctx), data.Grammar.Globals);

            var defaultLabel = emit.Labels.Generate();
            var endWithSingleResultLabel = emit.Labels.Generate();
            var jumpTable = new Ref<Labels>[data.Grammar.Productions.IndexCount];
            for (int i = 0; i != jumpTable.Length; ++i)
            {
                var prod = data.Grammar.Productions[i];
                // Optimize indexing
                if (prod == null)
                {
                    jumpTable[i] = defaultLabel.GetRef();
                }
                else
                {
                    jumpTable[i] = emit.Labels.Generate().GetRef();
                }
            }

            emit
                .Do(il => il.Ldarg(ruleId))
                .Switch(jumpTable)
                .Br(defaultLabel.GetRef());

            foreach (var prod in data.Grammar.Productions)
            {
                if (prod.IsHidden)
                {
                    continue;
                }

                emit.Label(jumpTable[prod.Index].Def);

                CompileProduction(emit, data, ruleArgs, argsStart, lookbackStart, returnLabel, globalSemanticCode, prod, varStack);
                varStack.LdLastSlot();
                varStack.Pop(1);

                emit.Br(endWithSingleResultLabel.GetRef());
            }

            emit
                .Label(defaultLabel)
                .Ldnull()
                .Label(endWithSingleResultLabel)
                .Label(returnLabel)
                .Ret();
        }

        private static void CompileProduction(
            EmitSyntax      emit,
            LanguageData    data,
            Ref<Args>       ruleArgs,
            Ref<Args>       argsStart,
            Ref<Args>       lookbackStart,
            Def<Labels>     returnLabel,
            ISemanticLoader globals,
            Production      prod,
            VarsStack       varsStack)
        {
            var locals = new StackSemanticLoader(
                globals,
                emit,
                il => il.Ldarg(lookbackStart),
                data.SemanticBindings);

            int varsStackStart = varsStack.Count;
            int index = 0;
            foreach (var arg in prod.Pattern)
            {
                emit = emit
                    .Ldarg(ruleArgs)
                    .Ldarg(argsStart)
                    ;

                // Optimization for "+ 0".
                if (index != 0)
                {
                    emit
                        .Ldc_I4(index)
                        .Add();
                }

                if (typeof(ActionNode).IsValueType)
                {
                    emit = emit
                        .Ldelema(emit.Types.Import(typeof(ActionNode)));
                }
                else
                {
                    emit = emit
                        .Ldelem_Ref();
                }

                emit = emit
                    .Ldfld((ActionNode msg) => msg.Value)
                    ;

                varsStack.Push();

                ++index;
            }

            var emitCoder = Fluent.Create(emit);

            // Build inlined productions 
            var compiler = new ProductionCompiler(emitCoder, varsStack, globals);
            compiler.Execute(prod);


            var coder = Fluent.Create<IActionCode>(new ProductionCode(
                    emitCoder,
                    locals,
                    varsStack,
                    varsStackStart));

            CompileProduction(coder, varsStack, varsStackStart, prod);
        }

        public static void CompileProduction(
            Fluent<IActionCode> coder,
            VarsStack           varStack,
            int                 varsStackStart, 
            Production          prod)
        {
            var bindings = prod.Joint.All<CilProduction>();
            if (!bindings.Any())
            {
                if (prod.IsAugmented)
                {
                    coder.Do(c => c.Emit(il => il.Ldnull()));
                }
                else if (prod.HasIdentityAction)
                {
                    coder.Do(c => c.LdActionArgument(0));
                }
                else
                {
                    var msg = string.Format("Production '{0}' has no associated managed action.", prod);
                    throw new InvalidOperationException(msg);
                }
            }
            else
            {
                bool first = true;
                foreach (var binding in bindings)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        // Result of this rule supersedes result of the prvious one
                        coder.Do(c => c
                            .Emit(il => il.Pop()));
                    }

                    coder.Do(c => c
                        .LdSemantic(binding.Context.UniqueName)
                        .Do(binding.ActionBuilder))
                        ;
                }
            }

            varStack.RemoveRange(varsStackStart, prod.Components.Length);
            varStack.InsertAt(varsStackStart);
        }
    }
}
