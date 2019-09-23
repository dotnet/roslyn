// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
