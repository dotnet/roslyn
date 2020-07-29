// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MakeAnonymousFunctionStatic
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class MakeAnonymousFunctionStaticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public MakeAnonymousFunctionStaticAnalyzer()
            : base(IDEDiagnosticIds.MakeAnonymousFunctionStaticDiagnosticId,
                   CSharpCodeStyleOptions.PreferStaticAnonymousFunction,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Make_anonymous_function_static), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Anonymous_function_can_be_made_static), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
          => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.AnonymousMethodExpression);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var anonymousFunction = (AnonymousFunctionExpressionSyntax)context.Node;
            if (anonymousFunction.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return;
            }

            var syntaxTree = context.Node.SyntaxTree;
            if (!MakeAnonymousFunctionStaticHelper.IsStaticAnonymousFunctionSupported(syntaxTree))
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var option = context.Options.GetOption(CSharpCodeStyleOptions.PreferStaticAnonymousFunction, syntaxTree, cancellationToken);
            if (!option.Value)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            if (MakeAnonymousFunctionStaticHelper.TryGetCaputuredSymbols(anonymousFunction, semanticModel, out var captures) && captures.Length == 0)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    anonymousFunction.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations: ImmutableArray.Create(anonymousFunction.GetLocation()),
                    properties: null));
            }
        }
    }
}

