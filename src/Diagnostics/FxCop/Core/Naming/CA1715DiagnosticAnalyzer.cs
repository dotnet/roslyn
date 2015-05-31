// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class CA1715DiagnosticAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1715";

        private static readonly LocalizableString s_localizableMessageAndTitleInterfaceRule = new LocalizableResourceString(nameof(FxCopRulesResources.InterfaceNamesShouldStartWithI), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        internal static readonly DiagnosticDescriptor InterfaceRule = new DiagnosticDescriptor(RuleId,
                                                                                      s_localizableMessageAndTitleInterfaceRule,
                                                                                      s_localizableMessageAndTitleInterfaceRule,
                                                                                      FxCopDiagnosticCategory.Naming,
                                                                                      DiagnosticSeverity.Warning,
                                                                                      isEnabledByDefault: true,
                                                                                      helpLinkUri: "http://msdn.microsoft.com/library/ms182243.aspx",
                                                                                      customTags: DiagnosticCustomTags.Microsoft);

        private static readonly LocalizableString s_localizableMessageAndTitleTypeParameterRule = new LocalizableResourceString(nameof(FxCopRulesResources.TypeParameterNamesShouldStartWithT), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        internal static readonly DiagnosticDescriptor TypeParameterRule = new DiagnosticDescriptor(RuleId,
                                                                                      s_localizableMessageAndTitleTypeParameterRule,
                                                                                      s_localizableMessageAndTitleTypeParameterRule,
                                                                                      FxCopDiagnosticCategory.Naming,
                                                                                      DiagnosticSeverity.Warning,
                                                                                      isEnabledByDefault: true,
                                                                                      helpLinkUri: "http://msdn.microsoft.com/library/ms182243.aspx",
                                                                                      customTags: DiagnosticCustomTags.Microsoft);

        private static readonly ImmutableArray<DiagnosticDescriptor> s_supportedRules = ImmutableArray.Create(InterfaceRule, TypeParameterRule);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return s_supportedRules;
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
