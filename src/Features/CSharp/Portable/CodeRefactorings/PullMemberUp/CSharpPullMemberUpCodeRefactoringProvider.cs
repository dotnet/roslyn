// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.PullMemberUp)), Shared]
    internal class CSharpPullMemberUpCodeRefactoringProvider : AbstractPullMemberUpRefactoringProvider
    {
        /// <summary>
        /// Test purpose only.
        /// </summary>
        public CSharpPullMemberUpCodeRefactoringProvider(IPullMemberUpOptionsService service) : base(service)
        {
        }

        [ImportingConstructor]
        public CSharpPullMemberUpCodeRefactoringProvider() : base(null)
        {
        }

        protected override async Task<SyntaxNode> GetSelectedNodeAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var refactoringHelperService = document.GetLanguageService<IRefactoringHelpersService>();

            // Consider MemberDeclaration (types, methods, events, delegates, ...) and VariableDeclarator (pure variables)
            var memberDecl = await refactoringHelperService.TryGetSelectedNodeAsync<MemberDeclarationSyntax>(document, span, cancellationToken).ConfigureAwait(false);
            if (memberDecl != default)
            {
                return memberDecl;
            }

            var varDecl = await refactoringHelperService.TryGetSelectedNodeAsync<VariableDeclaratorSyntax>(document, span, cancellationToken).ConfigureAwait(false);
            return varDecl;
        }
    }
}
