// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class DefaultExpressionSyntaxExtensions
    {
        private static readonly LiteralExpressionSyntax s_defaultLiteralExpression =
            SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);

        public static bool CanReplaceWithDefaultLiteral(
            this DefaultExpressionSyntax defaultExpression,
            CSharpParseOptions parseOptions,
            OptionSet options,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (parseOptions.LanguageVersion < LanguageVersion.CSharp7_1 ||
                !options.GetOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression).Value)
            {
                return false;
            }

            // Using the speculation analyzer can be slow.  Check for common cases first before
            // trying the expensive path.
            return CanReplaceWithDefaultLiteralFast(defaultExpression, semanticModel, cancellationToken) ??
                   CanReplaceWithDefaultLiteralSlow(defaultExpression, semanticModel, cancellationToken);
        }

        private static bool? CanReplaceWithDefaultLiteralFast(
            DefaultExpressionSyntax defaultExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (defaultExpression.IsParentKind(SyntaxKind.EqualsValueClause))
            {
                var equalsValueClause = (EqualsValueClauseSyntax)defaultExpression.Parent;
                var typeSyntax = GetTypeSyntax(equalsValueClause);

                if (typeSyntax != null)
                {
                    if (typeSyntax.IsVar)
                    {
                        // If we have:   var v = default(CancellationToken);    then we can't simplify this.
                        return false;
                    }

                    var entityType = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type;
                    var defaultType = semanticModel.GetTypeInfo(defaultExpression.Type, cancellationToken).Type;

                    if (entityType != null && entityType.Equals(defaultType))
                    {
                        // We have a simple case of "CancellationToken c = default(CancellationToken)".
                        // We can just simplify without having to do any additional analysis.
                        return true;
                    }
                }
            }

            return null;
        }

        private static TypeSyntax GetTypeSyntax(EqualsValueClauseSyntax equalsValueClause)
        {
            if (equalsValueClause.IsParentKind(SyntaxKind.VariableDeclarator) &&
                equalsValueClause.Parent.IsParentKind(SyntaxKind.VariableDeclaration))
            {
                var declaration = (VariableDeclarationSyntax)equalsValueClause.Parent.Parent;
                return declaration.Type;
            }
            else if (equalsValueClause.IsParentKind(SyntaxKind.Parameter))
            {
                var parameter = (ParameterSyntax)equalsValueClause.Parent;
                return parameter.Type;
            }

            return null;
        }

        private static bool CanReplaceWithDefaultLiteralSlow(
            DefaultExpressionSyntax defaultExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var speculationAnalyzer = new SpeculationAnalyzer(
                defaultExpression, s_defaultLiteralExpression, semanticModel,
                cancellationToken,
                skipVerificationForReplacedNode: false,
                failOnOverloadResolutionFailuresInOriginalCode: true);

            return !speculationAnalyzer.ReplacementChangesSemantics();
        }
    }
}
