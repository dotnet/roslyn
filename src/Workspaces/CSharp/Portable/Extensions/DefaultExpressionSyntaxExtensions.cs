// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class DefaultExpressionSyntaxExtensions
    {
        private static readonly LiteralExpressionSyntax s_defaultLiteralExpression = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);

        public static bool CanReplaceWithDefaultLiteral(
            this DefaultExpressionSyntax defaultExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var speculationAnalyzer = new SpeculationAnalyzer(
                defaultExpression, s_defaultLiteralExpression, semanticModel,
                cancellationToken,
                skipVerificationForReplacedNode: true,
                failOnOverloadResolutionFailuresInOriginalCode: true);

            return !speculationAnalyzer.ReplacementChangesSemantics();
        }
    }
}
