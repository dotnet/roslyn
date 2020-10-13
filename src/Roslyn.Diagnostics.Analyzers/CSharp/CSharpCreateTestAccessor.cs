// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpCreateTestAccessor))]
    [Shared]
    public sealed class CSharpCreateTestAccessor : AbstractCreateTestAccessor<TypeDeclarationSyntax>
    {
        public CSharpCreateTestAccessor()
        {
        }

        private protected override IRefactoringHelpers RefactoringHelpers => CSharpRefactoringHelpers.Instance;

        protected override SyntaxNode GetTypeDeclarationForNode(SyntaxNode reportedNode)
        {
            return reportedNode.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        }
    }
}
