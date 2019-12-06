// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SimplifyInterpolation;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyInterpolation
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpSimplifyInterpolationCodeFixProvider : AbstractSimplifyInterpolationCodeFixProvider<
        InterpolationSyntax, ExpressionSyntax, InterpolationAlignmentClauseSyntax, InterpolationFormatClauseSyntax>
    {
        protected override InterpolationSyntax WithExpression(InterpolationSyntax interpolation, ExpressionSyntax expression)
            => interpolation.WithExpression(expression);

        protected override InterpolationSyntax WithAlignmentClause(InterpolationSyntax interpolation, InterpolationAlignmentClauseSyntax alignmentClause)
            => interpolation.WithAlignmentClause(alignmentClause);

        protected override InterpolationSyntax WithFormatClause(InterpolationSyntax interpolation, InterpolationFormatClauseSyntax formatClause)
            => interpolation.WithFormatClause(formatClause);
    }
}
