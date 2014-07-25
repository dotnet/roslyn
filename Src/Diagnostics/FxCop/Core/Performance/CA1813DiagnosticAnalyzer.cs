// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Shared.Extensions;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
{
    /// <summary>
    /// CA1813: Seal attribute types for improved performance. Sealing attribute types speeds up performance during reflection on custom attributes.
    /// </summary>
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(RuleId, LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class CA1813DiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        internal const string RuleId = "CA1813";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.AvoidUnsealedAttributes,
                                                                         FxCopRulesResources.SealAttributeTypesForImprovedPerf,
                                                                         FxCopDiagnosticCategory.Performance,
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

        public override void AnalyzeSymbol(INamedTypeSymbol namedType, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            if (namedType.IsAbstract || namedType.IsSealed || !namedType.IsAttribute())
            {
                return;
            }

            // Non-sealed non-abstract attribute type.
            addDiagnostic(namedType.CreateDiagnostic(Rule));
        }
    }
}