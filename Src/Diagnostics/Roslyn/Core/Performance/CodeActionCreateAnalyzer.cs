// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class CodeActionCreateAnalyzer : IDiagnosticAnalyzer, ICompilationNestedAnalyzerFactory
    {
        internal const string CodeActionMetadataName = "Microsoft.CodeAnalysis.CodeActions.CodeAction";
        internal const string CreateMethodName = "Create";

        internal static readonly DiagnosticDescriptor DontUseCodeActionCreateRule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DontUseCodeActionCreateRuleId,
            RoslynDiagnosticsResources.DontUseCodeActionCreateDescription,
            RoslynDiagnosticsResources.DontUseCodeActionCreateMessage,
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DontUseCodeActionCreateRule); }
        }

        public IDiagnosticAnalyzer CreateAnalyzerWithinCompilation(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var codeActionSymbol = compilation.GetTypeByMetadataName(CodeActionMetadataName);
            if (codeActionSymbol == null)
            {
                return null;
            }

            var createSymbols = codeActionSymbol.GetMembers(CreateMethodName).Where(m => m is IMethodSymbol);
            if (createSymbols == null)
            {
                return null;
            }

            var createSymbolsSet = ImmutableHashSet.CreateRange(createSymbols);
            return GetCodeBlockStartedAnalyzer(createSymbolsSet);
        }

        protected abstract AbstractCodeBlockStartedAnalyzer GetCodeBlockStartedAnalyzer(ImmutableHashSet<ISymbol> symbols);

        protected abstract class AbstractCodeBlockStartedAnalyzer : ICodeBlockNestedAnalyzerFactory
        {
            private readonly ImmutableHashSet<ISymbol> symbols;

            public AbstractCodeBlockStartedAnalyzer(ImmutableHashSet<ISymbol> symbols)
            {
                this.symbols = symbols;
            }

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(DontUseCodeActionCreateRule); }
            }

            protected abstract AbstractSyntaxAnalyzer GetSyntaxAnalyzer(ImmutableHashSet<ISymbol> symbols);

            public IDiagnosticAnalyzer CreateAnalyzerWithinCodeBlock(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                return GetSyntaxAnalyzer(symbols);
            }
        }

        protected abstract class AbstractSyntaxAnalyzer : IDiagnosticAnalyzer
        {
            private readonly ImmutableHashSet<ISymbol> symbols;

            public AbstractSyntaxAnalyzer(ImmutableHashSet<ISymbol> symbols)
            {
                this.symbols = symbols;
            }

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(DontUseCodeActionCreateRule); }
            }

            private bool IsCodeActionCreate(SyntaxNode expression, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
                return symbolInfo.Symbol != null && symbols.Contains(symbolInfo.Symbol);
            }

            protected void AnalyzeInvocationExpression(SyntaxNode name, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                if (!IsCodeActionCreate(name, semanticModel, cancellationToken))
                {
                    return;
                }

                addDiagnostic(Diagnostic.Create(DontUseCodeActionCreateRule, name.Parent.GetLocation()));
            }
        }
    }
}
