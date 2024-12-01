// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static partial class CastExpressionSyntaxExtensions
{
    public static ExpressionSyntax Uncast(this CastExpressionSyntax node)
    {
        var leadingTrivia = node.OpenParenToken.LeadingTrivia
            .Concat(node.OpenParenToken.TrailingTrivia)
            .Concat(node.Type.GetLeadingTrivia())
            .Concat(node.Type.GetTrailingTrivia())
            .Concat(node.CloseParenToken.LeadingTrivia)
            .Concat(node.CloseParenToken.TrailingTrivia)
            .Concat(node.Expression.GetLeadingTrivia())
            .Where(t => !t.IsElastic());

        var trailingTrivia = node.GetTrailingTrivia().Where(t => !t.IsElastic());

        var resultNode = node.Expression
            .WithLeadingTrivia(leadingTrivia)
            .WithTrailingTrivia(trailingTrivia)
            .WithAdditionalAnnotations(Simplifier.Annotation);

        resultNode = SimplificationHelpers.CopyAnnotations(from: node, to: resultNode);

        return resultNode;
    }
}
