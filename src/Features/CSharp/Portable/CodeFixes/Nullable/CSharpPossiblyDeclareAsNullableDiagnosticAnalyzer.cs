// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.PossiblyDeclareAsNullable
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpPossiblyDeclareAsNullableDiagnosticAnalyzer :
        DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_possiblyDeclareAsNullableRule
            = new DiagnosticDescriptor(
            IDEDiagnosticIds.PossiblyDeclareAsNullableDiagnosticId,
            title: new LocalizableResourceString(nameof(FeaturesResources.Variable_could_be_annotated_as_nullable), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            messageFormat: new LocalizableResourceString(nameof(FeaturesResources.Variable_0_could_be_annotated_as_nullable), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
            DiagnosticCategory.CodeQuality,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(FeaturesResources.NullTestedVariablePossiblyDeclaredNullableDescription), FeaturesResources.ResourceManager, typeof(FeaturesResources)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(s_possiblyDeclareAsNullableRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression,
                SyntaxKind.IsPatternExpression, SyntaxKind.ConditionalAccessExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var fixableSymbol = IsFixable(context.Node, context.SemanticModel);
            if (fixableSymbol != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(s_possiblyDeclareAsNullableRule, context.Node.GetLocation(), fixableSymbol.Name));
            }
        }

        internal static ISymbol IsFixable(SyntaxNode node, SemanticModel model)
        {
            var symbolToFix = TryGetSymbolToFix(node, model);
            if (symbolToFix == null ||
                symbolToFix.Locations.Length != 1 ||
                !symbolToFix.IsNonImplicitAndFromSource() ||
                symbolToFix.Locations[0].SourceTree != node.SyntaxTree)
            {
                return null;
            }

            if (!IsFixableType(symbolToFix))
            {
                return null;
            }

            return symbolToFix;
        }

        private static bool IsFixableType(ISymbol symbolToFix)
        {
            var type = symbolToFix switch
            {
                IParameterSymbol parameter => parameter.Type,
                ILocalSymbol local => local.Type,
                IPropertySymbol property => property.Type,
                IMethodSymbol method when method.IsDefinition => method.ReturnType,
                IFieldSymbol field => field.Type,
                _ => null
            };

            return type?.IsReferenceType == true;
        }

        private static ISymbol TryGetSymbolToFix(SyntaxNode node, SemanticModel model)
        {
            ExpressionSyntax value = null;
            if (node.IsKind(SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression))
            {
                var equals = (BinaryExpressionSyntax)node;
                if (equals.Right.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    value = equals.Left;
                }
                else if (equals.Left.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    value = equals.Right;
                }
            }

            if (node.IsKind(SyntaxKind.IsPatternExpression))
            {
                value = ((IsPatternExpressionSyntax)node).Expression;
            }

            if (node.IsKind(SyntaxKind.ConditionalAccessExpression))
            {
                value = ((ConditionalAccessExpressionSyntax)node).Expression;
            }

            return value == null ? null : model.GetSymbolInfo(value).Symbol;
        }
    }
}
