// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Shared.Extensions;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1018: Custom attributes should have AttributeUsage attribute defined.
    /// </summary>
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(RuleId, LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class CA1018DiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        internal const string RuleId = "CA1018";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                    FxCopRulesResources.CustomAttrShouldHaveAttributeUsage,
                                                                    FxCopRulesResources.MarkAttributesWithAttributeUsage,
                                                                    FxCopDiagnosticCategory.Design,
                                                                    DiagnosticSeverity.Warning,
                                                                    isEnabledByDefault: true,
                                                                    customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var attributeUsageAttribute = WellKnownTypes.AttributeUsageAttribute(compilation);
            if (attributeUsageAttribute == null)
            {
                return;
            }

            if (symbol.IsAbstract || !symbol.IsAttribute())
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
