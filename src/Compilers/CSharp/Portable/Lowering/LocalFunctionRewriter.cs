// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CodeGen;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LocalFunctionRewriter : BoundTreeRewriter
    {
        private readonly DiagnosticBag _diagnostics;
        private readonly MethodSymbol _method;
        private readonly int _methodOrdinal;
        private readonly TypeCompilationState _compilationState;
        private readonly SmallDictionary<MethodSymbol, MethodSymbol> _loweringTranslation;

        public LocalFunctionRewriter(DiagnosticBag diagnostics, MethodSymbol method, int methodOrdinal, TypeCompilationState compilationState)
        {
            _diagnostics = diagnostics;
            _method = method;
            _methodOrdinal = methodOrdinal;
            _compilationState = compilationState;
            // we know it will be used, so eagerly allocate it
            _loweringTranslation = new SmallDictionary<MethodSymbol, MethodSymbol>();
        }

        internal static BoundStatement Rewrite(
            BoundStatement statement,
            NamedTypeSymbol containingType,
            ParameterSymbol thisParameter,
            MethodSymbol method,
            int methodOrdinal,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            var rewriter = new LocalFunctionRewriter(diagnostics, method, methodOrdinal, compilationState);
            var loweredStatement = (BoundStatement)rewriter.Visit(statement);
            return loweredStatement;
        }

        private bool ImplicitReturnIsOkay(MethodSymbol method)
        {
            return method.ReturnsVoid || method.IsIterator ||
                (method.IsAsync && method.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task) == method.ReturnType);
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            // Have to do ControlFlowPass here because in MethodCompiler, we don't call this for synthed methods
            // rather we go directly to LowerBodyOrInitializer, which skips over flow analysis (which is in CompileMethod)
            // (the same thing - calling ControlFlowPass.Analyze in the lowering - is done for lambdas)
            var endIsReachable = ControlFlowPass.Analyze(node.LocalSymbol.DeclaringCompilation, node.LocalSymbol, node, _diagnostics);

            var flowAnalyzed = node.Body;
            if (endIsReachable)
            {
                if (ImplicitReturnIsOkay(node.LocalSymbol))
                {
                    flowAnalyzed = FlowAnalysisPass.AppendImplicitReturn(flowAnalyzed, node.LocalSymbol, node.Syntax);
                }
                else
                {
                    _diagnostics.Add(ErrorCode.ERR_ReturnExpected, node.LocalSymbol.Locations[0], node.LocalSymbol);
                }
            }

            var topLevelDebugId = new DebugId(_methodOrdinal, _compilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);
            var localFunctionDebugId = new DebugId(-1, _compilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);
            var synth = new SynthesizedLocalFunction(_method.ContainingType, _method, topLevelDebugId, node, localFunctionDebugId);
            _compilationState.ModuleBuilderOpt.AddSynthesizedDefinition(_method.ContainingType, synth);
            _compilationState.AddSynthesizedMethod(synth, flowAnalyzed);

            _loweringTranslation.Add(node.LocalSymbol, synth);

            return new BoundNoOpStatement(node.Syntax, NoOpStatementFlavor.Default);
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            MethodSymbol newCall;
            if (_loweringTranslation.TryGetValue(node.Method, out newCall))
            {
                node = node.Update(node.ReceiverOpt, newCall, node.Arguments);
            }
            return base.VisitCall(node);
        }

        /// <summary>
        /// A method that results from the translation of a single lambda expression.
        /// </summary>
        internal sealed class SynthesizedLocalFunction : SynthesizedMethodBaseSymbol, ISynthesizedMethodBodyImplementationSymbol
        {
            private readonly MethodSymbol _topLevelMethod;

            internal SynthesizedLocalFunction(
                NamedTypeSymbol containingType,
                MethodSymbol topLevelMethod,
                DebugId topLevelMethodId,
                BoundLocalFunctionStatement localFunction,
                DebugId localFunctionId)
                : base(containingType,
                       localFunction.LocalSymbol,
                       null,
                       localFunction.SyntaxTree.GetReference(localFunction.Body.Syntax),
                       localFunction.Syntax.GetLocation(),
                       MakeName(topLevelMethod.Name, topLevelMethodId, localFunction.LocalSymbol.Name, localFunctionId),
                       DeclarationModifiers.Private | DeclarationModifiers.Static)
            {
                _topLevelMethod = topLevelMethod;

                TypeMap typeMap;
                ImmutableArray<TypeParameterSymbol> typeParameters;

                if (!topLevelMethod.IsGenericMethod)
                {
                    typeMap = TypeMap.Empty;
                    typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
                }
                else
                {
                    typeMap = TypeMap.Empty.WithAlphaRename(topLevelMethod, this, out typeParameters);
                }

                AssignTypeMapAndTypeParameters(typeMap, typeParameters);
            }

            private static string MakeName(string topLevelMethodName, DebugId topLevelMethodId, string localFunctionName, DebugId localId)
            {
                return GeneratedNames.MakeLocalFunctionName(
                    topLevelMethodName,
                    topLevelMethodId.Ordinal,
                    topLevelMethodId.Generation,
                    localFunctionName,
                    localId.Ordinal,
                    localId.Generation);
            }

            internal override int ParameterCount => this.BaseMethod.ParameterCount;

            protected override ImmutableArray<ParameterSymbol> BaseMethodParameters => this.BaseMethod.Parameters;

            internal override bool GenerateDebugInfo => !this.IsAsync;
            internal override bool IsExpressionBodied => false;
            internal MethodSymbol TopLevelMethod => _topLevelMethod;

            internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
            {
                // Syntax offset of a syntax node contained in a local function body is calculated by the containing top-level method.
                // The offset is thus relative to the top-level method body start.
                return _topLevelMethod.CalculateLocalSyntaxOffset(localPosition, localTree);
            }

            IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method => _topLevelMethod;

            // The local function body needs to be updated when the containing top-level method body is updated.
            bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency => true;
        }
    }
}
