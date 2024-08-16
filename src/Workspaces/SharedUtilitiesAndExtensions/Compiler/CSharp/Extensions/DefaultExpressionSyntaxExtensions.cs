// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class DefaultExpressionSyntaxExtensions
{
    private static readonly LiteralExpressionSyntax s_defaultLiteralExpression =
        SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);

    public static bool CanReplaceWithDefaultLiteral(
        this DefaultExpressionSyntax defaultExpression,
        CSharpParseOptions parseOptions,
        bool preferSimpleDefaultExpression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (parseOptions.LanguageVersion < LanguageVersion.CSharp7_1 ||
            !preferSimpleDefaultExpression)
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
        if (defaultExpression?.Parent is EqualsValueClauseSyntax equalsValueClause)
        {
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
            equalsValueClause.Parent?.Parent is VariableDeclarationSyntax declaration)
        {
            return declaration.Type;
        }
        else if (equalsValueClause?.Parent is ParameterSyntax parameter)
        {
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
