﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    public abstract class CodeActionCreateAnalyzer<TLanguageKindEnum> : DiagnosticAnalyzer where TLanguageKindEnum : struct
    {
        internal const string CodeActionMetadataName = "Microsoft.CodeAnalysis.CodeActions.CodeAction";
        internal const string CreateMethodName = "Create";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.DontUseCodeActionCreateDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.DontUseCodeActionCreateMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        internal static readonly DiagnosticDescriptor DontUseCodeActionCreateRule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DontUseCodeActionCreateRuleId,
            s_localizableTitle,
            s_localizableMessage,
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DontUseCodeActionCreateRule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(CreateAnalyzerWithinCompilation);
        }

        private void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var codeActionSymbol = context.Compilation.GetTypeByMetadataName(CodeActionMetadataName);
            if (codeActionSymbol == null)
            {
                return;
            }

            var createSymbols = codeActionSymbol.GetMembers(CreateMethodName).Where(m => m is IMethodSymbol);
            if (createSymbols == null)
            {
                return;
            }

            var createSymbolsSet = ImmutableHashSet.CreateRange(createSymbols);
            context.RegisterCodeBlockStartAction<TLanguageKindEnum>(GetCodeBlockStartedAnalyzer(createSymbolsSet).CreateAnalyzerWithinCodeBlock);
        }

        protected abstract AbstractCodeBlockStartedAnalyzer GetCodeBlockStartedAnalyzer(ImmutableHashSet<ISymbol> symbols);

        protected abstract class AbstractCodeBlockStartedAnalyzer
        {
            private readonly ImmutableHashSet<ISymbol> _symbols;

            public AbstractCodeBlockStartedAnalyzer(ImmutableHashSet<ISymbol> symbols)
            {
                _symbols = symbols;
            }

            protected abstract void GetSyntaxAnalyzer(CodeBlockStartAnalysisContext<TLanguageKindEnum> context, ImmutableHashSet<ISymbol> symbols);

            public void CreateAnalyzerWithinCodeBlock(CodeBlockStartAnalysisContext<TLanguageKindEnum> context)
            {
                GetSyntaxAnalyzer(context, _symbols);
            }
        }

        protected abstract class AbstractSyntaxAnalyzer
        {
            private readonly ImmutableHashSet<ISymbol> _symbols;

            public AbstractSyntaxAnalyzer(ImmutableHashSet<ISymbol> symbols)
            {
                _symbols = symbols;
            }

            private bool IsCodeActionCreate(SyntaxNode expression, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
                return symbolInfo.Symbol != null && _symbols.Contains(symbolInfo.Symbol);
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
