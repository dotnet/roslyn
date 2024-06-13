// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString;

internal interface IConvertStringProvider
{
    bool CanConvert(
        ParsedDocument document,
        ExpressionSyntax expression,
        SyntaxFormattingOptions formattingOptions,
        out CanConvertParams convertParams,
        CancellationToken cancellationToken);

    ExpressionSyntax Convert(
        ParsedDocument document,
        ExpressionSyntax expression,
        ConvertToRawKind kind,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken);
}

internal abstract class AbstractConvertStringProvider<TStringExpression> : IConvertStringProvider
    where TStringExpression : ExpressionSyntax
{
    protected abstract bool CheckSyntax(TStringExpression expression);

    public bool CanConvert(ParsedDocument document, ExpressionSyntax expression, SyntaxFormattingOptions formattingOptions, out CanConvertParams convertParams, CancellationToken cancellationToken)
    {
        convertParams = default;
        return expression is TStringExpression stringExpression &&
            CheckSyntax(stringExpression) &&
            CanConvert(document, stringExpression, formattingOptions, out convertParams, cancellationToken);
    }

    public ExpressionSyntax Convert(ParsedDocument document, ExpressionSyntax expression, ConvertToRawKind kind, SyntaxFormattingOptions formattingOptions, CancellationToken cancellationToken)
    {
        var stringExpression = (TStringExpression)expression;

        Contract.ThrowIfFalse(CheckSyntax(stringExpression));

        return Convert(document, stringExpression, kind, formattingOptions, cancellationToken);
    }

    protected abstract bool CanConvert(
        ParsedDocument document,
        TStringExpression expression,
        SyntaxFormattingOptions formattingOptions,
        out CanConvertParams convertParams,
        CancellationToken cancellationToken);

    protected abstract TStringExpression Convert(
        ParsedDocument document,
        TStringExpression expression,
        ConvertToRawKind kind,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken);
}
