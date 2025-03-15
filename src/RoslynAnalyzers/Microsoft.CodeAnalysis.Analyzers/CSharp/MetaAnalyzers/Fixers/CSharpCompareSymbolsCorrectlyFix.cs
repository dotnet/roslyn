// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CompareSymbolsCorrectlyFix)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public sealed class CSharpCompareSymbolsCorrectlyFix() : CompareSymbolsCorrectlyFix
    {
        protected override SyntaxNode CreateConditionalAccessExpression(SyntaxNode expression, SyntaxNode whenNotNull)
            => SyntaxFactory.ConditionalAccessExpression((ExpressionSyntax)expression, (ExpressionSyntax)whenNotNull);

        protected override SyntaxNode GetExpression(IInvocationOperation invocationOperation)
            => ((InvocationExpressionSyntax)invocationOperation.Syntax).Expression;
    }
}
