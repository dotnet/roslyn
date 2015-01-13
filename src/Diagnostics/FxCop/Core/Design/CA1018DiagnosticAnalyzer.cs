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
    public sealed class CA1018DiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        internal const string RuleId = "CA1018";
        private static LocalizableString localizableTitle = new LocalizableResourceString(nameof(FxCopRulesResources.CustomAttrShouldHaveAttributeUsage), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString localizableMessage = new LocalizableResourceString(nameof(FxCopRulesResources.MarkAttributesWithAttributeUsage), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                    localizableTitle,
                                                                    localizableMessage,
                                                                    FxCopDiagnosticCategory.Design,
                                                                    DiagnosticSeverity.Warning,
                                                                    isEnabledByDefault: true,
                                                                    helpLink: "http://msdn.microsoft.com/library/ms182158.aspx",
                                                                    customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        protected override void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
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
