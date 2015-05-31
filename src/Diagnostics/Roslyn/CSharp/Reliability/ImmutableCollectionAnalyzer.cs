// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.CSharp.Reliability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ImmutableCollectionAnalyzer : DiagnosticAnalyzer
    {
        private const string ImmutableArrayMetadataName = "System.Collections.Immutable.ImmutableArray`1";

        private static readonly LocalizableString s_localizableMessageAndTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.DoNotCallToImmutableArrayMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        public static readonly DiagnosticDescriptor DoNotCallToImmutableArrayDescriptor = new DiagnosticDescriptor(
            RoslynDiagnosticIds.DoNotCallToImmutableArrayRuleId,
            s_localizableMessageAndTitle,
            s_localizableMessageAndTitle,
            "Reliability",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(DoNotCallToImmutableArrayDescriptor);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var immutableArrayType = context.Compilation.GetTypeByMetadataName(ImmutableArrayMetadataName);
            if (immutableArrayType != null)
            {
                context.RegisterSyntaxNodeAction(syntaxContext => AnalyzeCall(syntaxContext, immutableArrayType), SyntaxKind.InvocationExpression);
            }
        }

        private void AnalyzeCall(SyntaxNodeAnalysisContext context, ISymbol immutableArrayType)
        {
            var invokeSyntax = context.Node as InvocationExpressionSyntax;
            if (invokeSyntax == null)
            {
                return;
            }

            var memberSyntax = invokeSyntax.Expression as MemberAccessExpressionSyntax;
            if (memberSyntax == null ||
                memberSyntax.Name == null ||
                memberSyntax.Name.Identifier.ValueText != "ToImmutableArray")
            {
                return;
            }

            var targetType = context.SemanticModel.GetTypeInfo(memberSyntax.Expression, context.CancellationToken);
            if (targetType.Type != null && targetType.Type.OriginalDefinition.Equals(immutableArrayType))
            {
                context.ReportDiagnostic(Diagnostic.Create(DoNotCallToImmutableArrayDescriptor, context.Node.GetLocation()));
            }
        }
    }
}
