// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.PullMemberUp)), Shared]
    internal class CSharpPullMemberUpCodeRefactoringProvider : AbstractPullMemberUpRefactoringProvider
    {
        /// <summary>
        /// Test purpose only.
        /// </summary>
        internal CSharpPullMemberUpCodeRefactoringProvider(IPullMemberUpOptionsService service) : base(service)
        {
        }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        internal CSharpPullMemberUpCodeRefactoringProvider() : base(null)
        {
        }

        protected override bool IsSelectionValid(TextSpan span, SyntaxNode selectedNode)
        {
            var identifier = GetIdentifier(selectedNode);
            if (identifier == default)
            {
                return false;
            }
            else if (identifier.FullSpan.Contains(span) && span.Contains(identifier.Span))
            {
                // Selection lies within the identifier's span
                return true;
            }
            else if (identifier.Span.Contains(span) && span.IsEmpty)
            {
                // Cursor stands on the identifier
                return true;
            }
            else
            {
                return false;
            }
        }

        protected async override Task<SyntaxNode> GetMatchedNodeAsync(Document document, TextSpan span)
        {
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            if (span.IsEmpty)
            {
                // Still provide the refactoring for following cases when the cursor stands on the rightest of the identifier
                // void Bar();
                // int i= 0;
                // int j=> 100;
                var token = root.FindToken(span.Start);
                if (token.IsKind(SyntaxKind.OpenParenToken) ||
                    token.IsKind(SyntaxKind.EqualsGreaterThanToken) ||
                    token.IsKind(SyntaxKind.EqualsToken))
                {
                    // root.FindNode will return the syntax node contains the '(', '=>' and '-',
                    // so move the span one step back to get the declaration syntax node
                    var relocatedSpan = new TextSpan(span.Start > 0 ? span.Start - 1 : 0, length: 0);
                    return root.FindNode(relocatedSpan);
                }
                else
                {
                    return root.FindNode(span);
                }
            }
            else
            {
                return root.FindNode(span);
            }
        }

        private SyntaxToken GetIdentifier(SyntaxNode selectedNode)
        {
            switch (selectedNode)
            {
                case MemberDeclarationSyntax memberDeclarationSyntax:
                    // Nested type is checked in before this method is called.
                    return memberDeclarationSyntax.GetNameToken();
                case VariableDeclaratorSyntax variableDeclaratorSyntax:
                    // It handles multiple fields or events declared in one line
                    return variableDeclaratorSyntax.Identifier;
                default:
                    return default;
            }
        }
    }
}
