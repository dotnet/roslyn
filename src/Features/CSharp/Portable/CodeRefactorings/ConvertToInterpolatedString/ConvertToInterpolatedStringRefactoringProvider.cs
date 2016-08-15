// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToInterpolatedString
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString), Shared]
    internal partial class ConvertToInterpolatedStringRefactoringProvider :
        AbstractConvertToInterpolatedStringRefactoringProvider<InvocationExpressionSyntax, ExpressionSyntax, ArgumentSyntax, LiteralExpressionSyntax>
    {
        protected override SyntaxNode GetInterpolatedString(string text) =>
            ParseExpression("$" + text) as InterpolatedStringExpressionSyntax;
    }
}
