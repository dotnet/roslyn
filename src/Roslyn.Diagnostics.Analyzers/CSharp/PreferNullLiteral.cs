// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PreferNullLiteral : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.PreferNullLiteralTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.PreferNullLiteralMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.PreferNullLiteralDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.PreferNullLiteralRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsMaintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(HandleDefaultValueOperation, OperationKind.DefaultValue);
        }

        private void HandleDefaultValueOperation(OperationAnalysisContext context)
        {
            if (context.Operation.IsImplicit)
            {
                // Ignore implicit operations since they don't appear in source code.
                return;
            }

            var type = context.Operation.Type;
            if (type is null)
            {
                return;
            }

            if (type.TypeKind == TypeKind.Pointer)
            {
                // Pointers can use 'null'
            }
            else if (type.TypeKind == TypeKind.Error)
            {
                return;
            }
            else if (type.IsValueType)
            {
                if (type is not INamedTypeSymbol namedType
                    || namedType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                {
                    return;
                }
            }
            else if (!type.IsReferenceType)
            {
                return;
            }

            context.ReportDiagnostic(context.Operation.CreateDiagnostic(Rule));
        }
    }
}
