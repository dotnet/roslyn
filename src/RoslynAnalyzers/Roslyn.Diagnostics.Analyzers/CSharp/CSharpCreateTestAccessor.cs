// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

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
