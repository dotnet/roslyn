﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseNullPropagation;

namespace Microsoft.CodeAnalysis.CSharp.UseNullPropagation
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseNullPropagationCodeFixProvider : AbstractUseNullPropagationCodeFixProvider<
            SyntaxKind,
            ExpressionSyntax,
            ConditionalExpressionSyntax,
            BinaryExpressionSyntax,
            InvocationExpressionSyntax,
            MemberAccessExpressionSyntax,
            ConditionalAccessExpressionSyntax,
            ElementAccessExpressionSyntax,
            ElementBindingExpressionSyntax,
            BracketedArgumentListSyntax>
    {
        [ImportingConstructor]
        public CSharpUseNullPropagationCodeFixProvider()
        {
        }

        protected override ElementBindingExpressionSyntax ElementBindingExpression(BracketedArgumentListSyntax argumentList)
            => SyntaxFactory.ElementBindingExpression(argumentList);
    }
}
