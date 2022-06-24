// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.PreferTrailingComma
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class PreferTrailingCommaDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public PreferTrailingCommaDiagnosticAnalyzer() : base(
            diagnosticId: IDEDiagnosticIds.PreferTrailingCommaDiagnosticId,
            enforceOnBuild: EnforceOnBuildValues.PreferTrailingComma,
            option: CSharpCodeStyleOptions.PreferTrailingComma,
            language: LanguageNames.CSharp,
            title: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Missing_trailing_comma), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode,
                SyntaxKind.EnumDeclaration, SyntaxKind.PropertyPatternClause, SyntaxKind.SwitchExpression,
                SyntaxKind.ObjectInitializerExpression, SyntaxKind.CollectionInitializerExpression, SyntaxKind.WithInitializerExpression,
                SyntaxKind.ArrayInitializerExpression, SyntaxKind.ComplexElementInitializerExpression, SyntaxKind.AnonymousObjectCreationExpression, SyntaxKind.ListPattern);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var option = context.GetCSharpAnalyzerOptions().PreferTrailingComma;
            if (!option.Value)
                return;

            var nodesWithSeparators = GetNodesWithSeparators(context.Node);
            if (nodesWithSeparators.Count < 1)
            {
                return;
            }

            var lastNodeOrSeparator = nodesWithSeparators[^1];
            if (lastNodeOrSeparator.IsToken)
            {
                return;
            }

            if (lastNodeOrSeparator.IsNode)
            {
                var lastNode = lastNodeOrSeparator.AsNode()!;
                if (CommaWillBeLastTokenOnLine(lastNode, context.CancellationToken))
                    context.ReportDiagnostic(DiagnosticHelper.Create(Descriptor, lastNode.GetLocation(), option.Notification.Severity, additionalLocations: null, properties: null));
            }
        }

        private static bool CommaWillBeLastTokenOnLine(SyntaxNode node, CancellationToken cancellationToken)
        {
            var lines = node.SyntaxTree.GetText(cancellationToken).Lines;
            var lastCurrentToken = node.DescendantTokens().Last();
            var nextToken = lastCurrentToken.GetNextToken();
            if (nextToken == default)
            {
                return true;
            }

            var line1 = lines.GetLineFromPosition(lastCurrentToken.Span.End).LineNumber;
            var line2 = lines.GetLineFromPosition(lastCurrentToken.GetNextToken().SpanStart).LineNumber;
            return line1 != line2;
        }

        internal static SyntaxNodeOrTokenList GetNodesWithSeparators(SyntaxNode node)
        {
            return node switch
            {
                EnumDeclarationSyntax enumDeclaration => enumDeclaration.Members.GetWithSeparators(),
                PropertyPatternClauseSyntax propertyPattern => propertyPattern.Subpatterns.GetWithSeparators(),
                SwitchExpressionSyntax switchExpression => switchExpression.Arms.GetWithSeparators(),
                InitializerExpressionSyntax initializerExpression => initializerExpression.Expressions.GetWithSeparators(),
                AnonymousObjectCreationExpressionSyntax anonymousObjectCreation => anonymousObjectCreation.Initializers.GetWithSeparators(),
                ListPatternSyntax listPattern => listPattern.Patterns.GetWithSeparators(),
                _ => throw ExceptionUtilities.Unreachable,
            };
        }
    }
}
