// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class DefaultExpressionSyntaxExtensions
    {
        private static readonly LiteralExpressionSyntax s_defaultLiteralExpression = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);

        public static bool CanReplaceWithDefaultLiteral(
            this DefaultExpressionSyntax defaultExpression,
            CSharpParseOptions parseOptions,
            OptionSet options,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (parseOptions.LanguageVersion >= LanguageVersion.CSharp7_1 &&
                options.GetOption(CSharpCodeStyleOptions.PreferDefaultLiteral).Value)
            {
                var speculationAnalyzer = new SpeculationAnalyzer(
                    defaultExpression, s_defaultLiteralExpression, semanticModel,
                    cancellationToken,
                    skipVerificationForReplacedNode: true,
                    failOnOverloadResolutionFailuresInOriginalCode: true);

                return !speculationAnalyzer.ReplacementChangesSemantics();
            }

            return false;
        }
    }
}
