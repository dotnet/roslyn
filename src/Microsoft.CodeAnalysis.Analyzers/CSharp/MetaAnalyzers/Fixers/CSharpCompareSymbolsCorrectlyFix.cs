// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CompareSymbolsCorrectlyFix)), Shared]
    public sealed class CSharpCompareSymbolsCorrectlyFix : CompareSymbolsCorrectlyFix
    {
        protected override SyntaxNode CreateConditionalAccessExpression(SyntaxNode expression, SyntaxNode whenNotNull)
            => SyntaxFactory.ConditionalAccessExpression((ExpressionSyntax)expression, (ExpressionSyntax)whenNotNull);

        protected override SyntaxNode GetExpression(IInvocationOperation invocationOperation)
            => ((InvocationExpressionSyntax)invocationOperation.Syntax).Expression;
    }
}
