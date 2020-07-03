// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.Lightup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    /// <summary>
    /// RS0046: Avoid 'Opt' suffix for nullable enable code
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpAvoidOptSuffixForNullableEnableCode : DiagnosticAnalyzer
    {
        internal const string OptSuffix = "Opt";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.AvoidOptSuffixForNullableEnableCodeTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.AvoidOptSuffixForNullableEnableCodeMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.AvoidOptSuffixForNullableEnableCodeDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.AvoidOptSuffixForNullableEnableCodeRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(context =>
            {
                var parameter = (ParameterSyntax)context.Node;
                ReportOnInvalidIdentifier(parameter.Identifier, context.SemanticModel, context.ReportDiagnostic, context.CancellationToken);
            }, SyntaxKind.Parameter);

            context.RegisterSyntaxNodeAction(context =>
            {
                var variableDeclarator = (VariableDeclaratorSyntax)context.Node;
                ReportOnInvalidIdentifier(variableDeclarator.Identifier, context.SemanticModel, context.ReportDiagnostic, context.CancellationToken);
            }, SyntaxKind.VariableDeclarator);
        }

        private static void ReportOnInvalidIdentifier(SyntaxToken identifier, SemanticModel semanticModel, Action<Diagnostic> reportAction, CancellationToken cancellationToken)
        {
            if (!identifier.Text.EndsWith(OptSuffix, StringComparison.Ordinal) ||
                !semanticModel.GetNullableContext(identifier.SpanStart).AnnotationsEnabled())
            {
                return;
            }

            var symbol = semanticModel.GetDeclaredSymbol(identifier.Parent, cancellationToken);
            if (symbol?.GetMemberOrLocalOrParameterType()?.NullableAnnotation() != Analyzer.Utilities.Lightup.NullableAnnotation.Annotated)
            {
                return;
            }

            if (symbol.Kind != SymbolKind.Parameter ||
                symbol.ContainingSymbol == null ||
                (!symbol.ContainingSymbol.IsOverride && !symbol.ContainingSymbol.IsImplementationOfAnyInterfaceMember()))
            {
                reportAction(identifier.CreateDiagnostic(Rule));
            }
        }
    }
}
