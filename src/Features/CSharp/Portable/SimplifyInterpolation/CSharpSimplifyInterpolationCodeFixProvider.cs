// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SimplifyInterpolation;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyInterpolation
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpSimplifyInterpolationCodeFixProvider : AbstractSimplifyInterpolationCodeFixProvider<
        InterpolationSyntax, ExpressionSyntax>
    {
        protected override InterpolationSyntax Update(
            InterpolationSyntax interpolation, ExpressionSyntax unwrapped, ExpressionSyntax alignment, string formatString)
        {
            var result = interpolation.WithExpression(unwrapped);
            if (alignment != null)
            {
                result = result.WithAlignmentClause(
                    SyntaxFactory.InterpolationAlignmentClause(SyntaxFactory.Token(SyntaxKind.CommaToken), alignment));
            }

            if (formatString != null)
            {
                if (formatString == "")
                {
                    result = result.WithFormatClause(null);
                }
                else
                {
                    result = result.WithFormatClause(SyntaxFactory.InterpolationFormatClause(
                        SyntaxFactory.Token(SyntaxKind.ColonToken),
                        SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, formatString, formatString, default)));
                }
            }

            return result;
        }
    }
}
