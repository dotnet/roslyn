// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpCreateTestAccessor))]
    [Shared]
    public sealed class CSharpCreateTestAccessor : AbstractCreateTestAccessor<TypeDeclarationSyntax>
    {
        [ImportingConstructor]
        [Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
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
