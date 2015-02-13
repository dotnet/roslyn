// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA1018: Custom attributes should have AttributeUsage attribute defined.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class MarkAttributesWithAttributeUsageAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1018";
        private static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.CustomAttrShouldHaveAttributeUsage), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));
        private static LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.MarkAttributesWithAttributeUsage), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                    s_localizableTitle,
                                                                    s_localizableMessage,
                                                                    DiagnosticCategory.Design,
                                                                    DiagnosticSeverity.Warning,
                                                                    isEnabledByDefault: true,
                                                                    helpLinkUri: "http://msdn.microsoft.com/library/ms182158.aspx",
                                                                    customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterSymbolAction(context =>
            {
                AnalyzeSymbol((INamedTypeSymbol)context.Symbol, context.Compilation, context.ReportDiagnostic, context.Options, context.CancellationToken);
            },
            SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var attributeUsageAttribute = WellKnownTypes.AttributeUsageAttribute(compilation);
            if (attributeUsageAttribute == null)
            {
                return;
            }

            if (symbol.IsAbstract || !symbol.GetBaseTypes().Contains(WellKnownTypes.Attribute(compilation)))
            {
                return;
            }

            if (attributeUsageAttribute == null)
            {
                return;
            }

            var hasAttributeUsageAttribute = symbol.GetAttributes().Any(attribute => attribute.AttributeClass == attributeUsageAttribute);
            if (!hasAttributeUsageAttribute)
            {
                addDiagnostic(symbol.CreateDiagnostic(Rule, symbol.Name));
            }
        }
    }
}
