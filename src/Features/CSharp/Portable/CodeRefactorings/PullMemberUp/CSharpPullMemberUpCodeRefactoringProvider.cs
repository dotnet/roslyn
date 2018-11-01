// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        internal override bool IsUserSelectIdentifer(SyntaxNode userSelectedSyntax, CodeRefactoringContext context)
        {
            var identifier = GetIdentifier(userSelectedSyntax);
            return identifier.Span.Contains(context.Span);
        }

        private SyntaxToken GetIdentifier(SyntaxNode userSelectedSyntax)
        {
            switch (userSelectedSyntax)
            {
                case VariableDeclaratorSyntax variableSyntax:
                    return variableSyntax.Identifier;
                case MethodDeclarationSyntax methodSyntax:
                    return methodSyntax.Identifier;
                case PropertyDeclarationSyntax propertySyntax:
                    return propertySyntax.Identifier;
                case IndexerDeclarationSyntax indexerSyntax:
                    return indexerSyntax.ThisKeyword;
                case EventDeclarationSyntax eventDeclartionSyntax:
                    return eventDeclartionSyntax.Identifier;
                default:
                    return default;
            }
        }
    }
}
