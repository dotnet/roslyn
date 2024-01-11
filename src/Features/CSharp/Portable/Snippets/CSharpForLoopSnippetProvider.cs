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

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal sealed class CSharpForLoopSnippetProvider : AbstractCSharpForLoopSnippetProvider
    {
        public override string Identifier => "for";

        public override string Description => CSharpFeaturesResources.for_loop;

        protected override SyntaxKind ConditionKind => SyntaxKind.LessThanExpression;

        protected override SyntaxKind IncrementorKind => SyntaxKind.PostIncrementExpression;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpForLoopSnippetProvider()
        {
        }

        protected override ExpressionSyntax GenerateInitializerValue(SyntaxGenerator generator, SyntaxNode? inlineExpression)
            => (ExpressionSyntax)generator.LiteralExpression(0);

        protected override ExpressionSyntax GenerateRightSideOfCondition(SyntaxGenerator generator, SyntaxNode? inlineExpression)
            => (ExpressionSyntax)(inlineExpression ?? generator.IdentifierName("length"));

        protected override void AddSpecificPlaceholders(MultiDictionary<string, int> placeholderBuilder, ExpressionSyntax initializer, ExpressionSyntax rightOfCondition)
        {
            if (!ConstructedFromInlineExpression)
                placeholderBuilder.Add(rightOfCondition.ToString(), rightOfCondition.SpanStart);
        }
    }
}
