// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.SimplifyInterpolation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SimplifyInterpolation;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyInterpolation;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SimplifyInterpolation), Shared]
internal class CSharpSimplifyInterpolationCodeFixProvider : AbstractSimplifyInterpolationCodeFixProvider<
    InterpolationSyntax, ExpressionSyntax, InterpolationAlignmentClauseSyntax,
    InterpolationFormatClauseSyntax, InterpolatedStringExpressionSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpSimplifyInterpolationCodeFixProvider()
    {
    }

    protected override AbstractSimplifyInterpolationHelpers GetHelpers() => CSharpSimplifyInterpolationHelpers.Instance;

    protected override InterpolationSyntax WithExpression(InterpolationSyntax interpolation, ExpressionSyntax expression)
        => interpolation.WithExpression(expression);

    protected override InterpolationSyntax WithAlignmentClause(InterpolationSyntax interpolation, InterpolationAlignmentClauseSyntax alignmentClause)
        => interpolation.WithAlignmentClause(alignmentClause);

    protected override InterpolationSyntax WithFormatClause(InterpolationSyntax interpolation, InterpolationFormatClauseSyntax formatClause)
        => interpolation.WithFormatClause(formatClause);

    protected override string Escape(InterpolatedStringExpressionSyntax interpolatedString, string formatString)
    {
        var result = new StringBuilder();
        if (interpolatedString.StringStartToken.Kind() == SyntaxKind.InterpolatedVerbatimStringStartToken)
        {
            foreach (var c in formatString)
            {
                // in a verbatim string, the only char we have to escape is the double-quote char
                if (c == '"')
                {
                    result.Append(c);
                }

                result.Append(c);
            }
        }
        else
        {
            // In a normal string we have to escape quotes and we have to escape an 
            // escape character itself.
            foreach (var c in formatString)
            {
                if (c is '"' or '\\')
                {
                    result.Append('\\');
                }

                result.Append(c);
            }
        }

        return result.ToString();
    }
}
