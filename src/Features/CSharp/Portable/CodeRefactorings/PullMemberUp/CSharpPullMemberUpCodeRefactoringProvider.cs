// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.PullMember)), Shared]
    internal class CSharpPullMemberUpCodeRefactoringProvider : AbstractPullMemberUpRefactoringProvider
    {
        /// <summary>
        /// For test purpose only.
        /// </summary>
        public CSharpPullMemberUpCodeRefactoringProvider(IPullMemberUpOptionsService pullMemberUpService) : base(pullMemberUpService)
        {
        }

        internal CSharpPullMemberUpCodeRefactoringProvider() : this(null)
        {
        }

        protected override bool IsSelectionValid(TextSpan span, SyntaxNode userSelectedSyntax)
        {
            var identifier = GetIdentifier(userSelectedSyntax);
            if (identifier == default)
            {
                return false;
            }
            else
            {
                return (identifier.FullSpan.Contains(span) && span.Contains(identifier.Span)) ||  
                    (identifier.Span.Contains(span) && span.Length == 0);
            }
        }

        private SyntaxToken GetIdentifier(SyntaxNode userSelectedSyntax)
        {
            if (userSelectedSyntax is MemberDeclarationSyntax memberDeclarationSyntax)
            {
                return memberDeclarationSyntax.GetNameToken();
            }
            else if (userSelectedSyntax is VariableDeclaratorSyntax variableDeclaratorSyntax)
            {
            // It handles multiple fields or events declared in one line
                return variableDeclaratorSyntax.Identifier;
            }
            else
            {
                return default;
            }
        }
    }
}
