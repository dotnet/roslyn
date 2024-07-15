// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpReversedForLoopSnippetProvider() : AbstractCSharpForLoopSnippetProvider
{
    public override string Identifier => "forr";

    public override string Description => CSharpFeaturesResources.reversed_for_loop;

    protected override SyntaxKind ConditionKind => SyntaxKind.GreaterThanOrEqualExpression;

    protected override SyntaxKind IncrementorKind => SyntaxKind.PostDecrementExpression;

    protected override ExpressionSyntax GenerateInitializerValue(SyntaxGenerator generator, SyntaxNode? inlineExpression)
    {
        var subtractFrom = inlineExpression?.WithoutLeadingTrivia() ?? generator.IdentifierName("length");
        return (ExpressionSyntax)generator.SubtractExpression(subtractFrom, generator.LiteralExpression(1));
    }

    protected override ExpressionSyntax GenerateRightSideOfCondition(SyntaxGenerator generator, SyntaxNode? inlineExpression)
        => (ExpressionSyntax)generator.LiteralExpression(0);

    protected override void AddSpecificPlaceholders(MultiDictionary<string, int> placeholderBuilder, ExpressionSyntax initializer, ExpressionSyntax rightOfCondition)
    {
        if (!ConstructedFromInlineExpression)
        {
            var binaryInitializer = (BinaryExpressionSyntax)initializer;
            var left = binaryInitializer.Left;
            placeholderBuilder.Add(left.ToString(), left.SpanStart);
        }
    }
}
