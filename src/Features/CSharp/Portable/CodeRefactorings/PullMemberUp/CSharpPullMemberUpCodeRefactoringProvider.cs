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

        protected override async Task<SyntaxNode> GetSelectedNodeAsync(CodeRefactoringContext context)
        {
            // Consider:
            // MemberDeclaration: member that can be declared in type (those are the ones we can pull up) 
            // VariableDeclaratorSyntax: for fields the MemberDeclaration can actually represent multiple declarations, e.g. `int a = 0, b = 1;`.
            // ..Since the user might want to select & pull up only one of them (e.g. `int a = 0, [|b = 1|];` we also look for closest VariableDeclaratorSyntax.
            var memberDecl = await context.TryGetSelectedNodeAsync<MemberDeclarationSyntax>().ConfigureAwait(false);
            if (memberDecl != default)
            {
                return memberDecl;
            }

            var varDecl = await context.TryGetSelectedNodeAsync<VariableDeclaratorSyntax>().ConfigureAwait(false);
            return varDecl;
        }
    }
}
