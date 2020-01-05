// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.NameTupleElement;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.NameTupleElement
{
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpNameTupleElementCodeRefactoringProvider)), Shared]
    internal class CSharpNameTupleElementCodeRefactoringProvider : AbstractNameTupleElementCodeRefactoringProvider<ArgumentSyntax, TupleExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpNameTupleElementCodeRefactoringProvider()
        {
        }

        protected override bool IsCloseParenOrComma(SyntaxToken token)
            => token.IsKind(SyntaxKind.CloseParenToken, SyntaxKind.CommaToken);

        protected override ArgumentSyntax WithName(ArgumentSyntax argument, string argumentName)
            => argument.WithNameColon(SyntaxFactory.NameColon(argumentName.ToIdentifierName()));
    }
}
