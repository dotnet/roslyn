// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Shared.Extensions;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Naming
{
    [DiagnosticAnalyzer]
    public sealed class CA1715DiagnosticAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1715";
        internal static readonly DiagnosticDescriptor InterfaceRule = new DiagnosticDescriptor(RuleId,
                                                                                      FxCopRulesResources.InterfaceNamesShouldStartWithI,
                                                                                      FxCopRulesResources.InterfaceNamesShouldStartWithI,
                                                                                      FxCopDiagnosticCategory.Naming,
                                                                                      DiagnosticSeverity.Warning,
                                                                                      isEnabledByDefault: true,
                                                                                      helpLink: "http://msdn.microsoft.com/library/ms182243.aspx",
                                                                                      customTags: DiagnosticCustomTags.Microsoft);

        internal static readonly DiagnosticDescriptor TypeParameterRule = new DiagnosticDescriptor(RuleId,
                                                                                      FxCopRulesResources.TypeParameterNamesShouldStartWithT,
                                                                                      FxCopRulesResources.TypeParameterNamesShouldStartWithT,
                                                                                      FxCopDiagnosticCategory.Naming,
                                                                                      DiagnosticSeverity.Warning,
                                                                                      isEnabledByDefault: true,
                                                                                      helpLink: "http://msdn.microsoft.com/library/ms182243.aspx",
                                                                                      customTags: DiagnosticCustomTags.Microsoft);

        private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(InterfaceRule, TypeParameterRule);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return SupportedRules;
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterSymbolAction(
                (context) =>
            {
                switch (context.Symbol.Kind)
                {
                    case SymbolKind.NamedType:
                        AnalyzeNamedTypeSymbol((INamedTypeSymbol)context.Symbol, context.ReportDiagnostic);
                        break;

                    case SymbolKind.Method:
                        AnalyzeMethodSymbol((IMethodSymbol)context.Symbol, context.ReportDiagnostic);
                        break;
                }
            },
                SymbolKind.Method,
                SymbolKind.NamedType);
        }

        private static void AnalyzeNamedTypeSymbol(INamedTypeSymbol symbol, Action<Diagnostic> addDiagnostic)
        {
            foreach (var parameter in symbol.TypeParameters)
            {
                if (!HasCorrectPrefix(parameter, 'T'))
                {
                    addDiagnostic(parameter.CreateDiagnostic(TypeParameterRule));
                }
            }

            if (symbol.TypeKind == TypeKind.Interface &&
                symbol.IsPublic() &&
                !HasCorrectPrefix(symbol, 'I'))
            {
                addDiagnostic(symbol.CreateDiagnostic(InterfaceRule));
            }
        }

        private static void AnalyzeMethodSymbol(IMethodSymbol symbol, Action<Diagnostic> addDiagnostic)
        {
            foreach (var parameter in symbol.TypeParameters)
            {
                if (!HasCorrectPrefix(parameter, 'T'))
                {
                    addDiagnostic(parameter.CreateDiagnostic(TypeParameterRule));
                }
            }
        }

        private static bool HasCorrectPrefix(ISymbol symbol, char prefix)
        {
            WordParser parser = new WordParser(symbol.Name, WordParserOptions.SplitCompoundWords, prefix);

            string firstWord = parser.NextWord();

            if (firstWord == null || firstWord.Length > 1)
            {
                return false;
            }

            return firstWord[0] == prefix;
        }
    }
}
