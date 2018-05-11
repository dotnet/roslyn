// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.InvertIf;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InvertIf
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertIf), Shared]
    internal sealed partial class CSharpInvertIfCodeRefactoringProvider : AbstractInvertIfCodeRefactoringProvider
    {
        protected override SyntaxNode GetIfStatement(SyntaxToken token)
        {
            return token.GetAncestor<IfStatementSyntax>();
        }

        protected override IAnalyzer GetAnalyzer(SyntaxNode ifStatement)
        {
            return new Analyzer();
        }
    }
}
