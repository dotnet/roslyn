﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString), Shared]
    internal partial class CSharpConvertPlaceholderToInterpolatedStringRefactoringProvider :
        AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider<InvocationExpressionSyntax, ExpressionSyntax, ArgumentSyntax, LiteralExpressionSyntax, ArgumentListSyntax>
    {
        [ImportingConstructor]
        public CSharpConvertPlaceholderToInterpolatedStringRefactoringProvider()
        {
        }

        protected override SyntaxNode GetInterpolatedString(string text)
            => SyntaxFactory.ParseExpression("$" + text) as InterpolatedStringExpressionSyntax;
    }
}
